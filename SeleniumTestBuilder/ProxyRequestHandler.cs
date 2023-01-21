using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SeleniumTestBuilder.Models;
using SeleniumTestBuilder.TestConsole.Models;
using System.Net;
using System.Reflection.Metadata;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using ICSharpCode.Decompiler;
using System.Reflection;

using System.Text.RegularExpressions;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web;

namespace SeleniumTestBuilder
{
    public interface IProxyRequestHandler : IDisposable
    {
        CSExport ParsePost(Uri url, string baseClassName, string stringPayload);
    }

    public sealed class ProxyRequestHandler : IProxyRequestHandler
    {
        private readonly Dictionary<string, string> _requestBodies;
        private ProxyServer _server;

        private string CleanupText(string text, bool removeSpecial = true)
        {
            var decodedText = HttpUtility.UrlDecode(text);
            var strippedText = decodedText
                .Replace("-", "_")
                .Replace("[", "_")
                .Replace("]", "_");
            var cleanText = removeSpecial ? strippedText : decodedText;
            return cleanText;
        }

        private IList<CSPropertyDefinition> ParseArray(in IList<CSPropertyDefinition> classesDict, in string currentClass, JProperty? child)
        {
            var value = child?.Value;
            var newClass = child != null ? $"{currentClass}_{child.Name}" : currentClass;

            IEnumerable<JToken> items = value?.Children() ?? Enumerable.Empty<JToken>();

            var itemsArrayType = items.All(x => x.Type == JTokenType.String) ? JTokenType.String :
                items.All(x => x.Type == JTokenType.Integer) ? JTokenType.Integer :
                items.All(x => x.Type == JTokenType.Object) ? JTokenType.Object : JTokenType.Undefined;
            var itemType = string.Empty;

            switch (itemsArrayType)
            {
                case JTokenType.String:
                    itemType = "string[]";
                    break;
                case JTokenType.Integer:
                    itemType = "double[]";
                    break;
                case JTokenType.Object:
                    itemType = newClass;
                    break;
                case JTokenType.Undefined:
                    itemType = "undefined[]";
                    break;
            }



            if (itemType != newClass)
            {
                if (child == null)
                    throw new Exception("Null child value");

                switch (itemType)
                {
                    case "string[]":
                        classesDict.Add(new CSPropertyDefinition(new CSClassDefinition(currentClass), child.Name, itemType, string.Join(",", items.Select(x => $"\"{x}\"")), true));
                        break;

                    case "double[]":
                        classesDict.Add(new CSPropertyDefinition(new CSClassDefinition(currentClass), child.Name, itemType, string.Join(",", items.Select(x => $"{x}")), true));
                        break;

                    case "undefined[]":
                        classesDict.Add(new CSPropertyDefinition(new CSClassDefinition(currentClass), child.Name, "object[]", string.Join(",", items.Select(x =>
                        {
                            switch (x.Type)
                            {
                                default:
                                case JTokenType.String:
                                    return $"\"{x}\"";
                                case JTokenType.Integer:
                                    return $"{x}";
                            }
                        })), true));
                        break;
                }
            }
            else
            {
                ParseObject(in classesDict, in newClass, items);
                classesDict.Add(new CSPropertyDefinition(new CSClassDefinition(currentClass), child.Name, itemType, null, true));
            }

            return classesDict;
        }

        private IList<CSPropertyDefinition> ParseObject(in IList<CSPropertyDefinition> classesDict, in string currentClass, IEnumerable<JToken> children)
        {
            foreach (var child_ in children)
            {
                if (child_ is JProperty cProperty)
                {
                    var value = cProperty.Value;

                    switch (value.Type)
                    {
                        case JTokenType.Object:

                            var newString = $"{currentClass}_{cProperty.Name}";
                            classesDict.Add(new CSPropertyDefinition(new CSClassDefinition(currentClass), cProperty.Name, newString, null, false));
                            ParseObject(in classesDict, in newString, value.Children());

                            break;
                        case JTokenType.Array:

                            ParseArray(in classesDict, in currentClass, cProperty);


                            break;
                        case JTokenType.Property:

                            break;
                        case JTokenType.Null:

                            classesDict.Add(new CSPropertyDefinition(new CSClassDefinition(currentClass), cProperty.Name, "object", null, false));
                            break;
                        case JTokenType.String:
                            var stringValue = value.Value<string>()?.ToString() ?? string.Empty;

                            classesDict.Add(new CSPropertyDefinition(new CSClassDefinition(currentClass), cProperty.Name, "string", stringValue, false));
                            break;
                        case JTokenType.Integer:
                            var integerValue = value.Value<double>().ToString();

                            classesDict.Add(new CSPropertyDefinition(new CSClassDefinition(currentClass), cProperty.Name, "double", integerValue, false));
                            break;
                        case JTokenType.Boolean:
                            var booleanValue = value.Value<bool>().ToString();

                            classesDict.Add(new CSPropertyDefinition(new CSClassDefinition(currentClass), cProperty.Name, "boolean", booleanValue, false));
                            break;
                    }

                }
                else if (child_ is JObject cObject)
                {
                    ParseObject(in classesDict, in currentClass, cObject.Children());
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            return classesDict;
        }

        private string ParseDefaultValue(CSPropertyDefinition a)
        {
            switch (a.PropertyType)
            {
                case "string":
                    return $"\"{a.DefaultValue}\"";
                case "double":
                    return a.DefaultValue;
                case "double[]":
                case "string[]":
                    if (a.DefaultValue != null)
                        return $"new []{{ {string.Join(",", a.DefaultValue.Split(","))} }}";
                    else
                        return $"new []{{  }}";
                case "object[]":
                    if (a.DefaultValue != null)
                        return $"new object[]{{ {string.Join(",", a.DefaultValue.Split(","))} }}";
                    else
                        return $"new object[]{{  }}";
                case "undefined[]":
                    if (a.DefaultValue != null)
                        return $"new []{{ {string.Join(",", a.DefaultValue.Split(","))} }}";
                    else
                        return $"new []{{  }}";
                default:
                    return $"new {a.PropertyType}()";
            }

        }

        private Task ProxyServerBeforeResponse(object sender, SessionEventArgs e)
        {
            var clientConnectionId = e.ClientConnectionId.ToString();
            var whiteList = File.ReadAllLines("whitelist.txt");

            if (whiteList.Contains(e.HttpClient.Request.Url) == false
                || e.HttpClient.Request.Url.StartsWith("https://accounts.google.com/ListAccounts"))
                return Task.CompletedTask;

            if (whiteList.Contains(e.HttpClient.Request.Url)
                && e.HttpClient.Response.StatusCode != 200)
                return Task.CompletedTask;

            if (this._requestBodies.ContainsKey(clientConnectionId) == false)
                this._requestBodies.Add(clientConnectionId, string.Empty);

            switch (e.HttpClient.Request.Method)
            {
                default:
                case "GET":

                    break;

                case "POST":

                    var baseClassName = string.Join("_", e.HttpClient.Request.Url.Split("/").Skip(3));

                    var stringPayload = string.Empty;
                    var data = e.HttpClient.UserData as dynamic;

                    try { stringPayload = data.RequestBody ?? string.Empty; }
                    catch (Exception ex) { }


                    var export = ParsePost(new Uri(e.HttpClient.Request.Url), baseClassName, stringPayload);
                    break;
            }
            return Task.CompletedTask;
        }

        private Task ProxyServerBeforeRequest(object sender, SessionEventArgs e)
        {
            try
            {
                e.HttpClient.UserData = new
                {
                    SessionId = Guid.NewGuid(),
                    RequestBody = e.GetRequestBodyAsString().GetAwaiter().GetResult()
                };
            }
            catch (Exception)
            {
                e.HttpClient.UserData = new
                {
                    SessionId = Guid.NewGuid(),
                    RequestBody = string.Empty
                };
            }

            //try
            //{
            //    this._requestBodies.Add(e.ClientConnectionId.ToString(), e.GetRequestBodyAsString().GetAwaiter().GetResult());
            //}
            //catch(Exception ex)
            //{
            //    this._requestBodies.Add(e.HttpClient.UserData.ToString(), string.Empty);
            //}
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            try
            {
                this._server.Stop();
            }
            catch (Exception)
            {

            }
        }

        public CSExport ParsePost(Uri url, string baseClassName, string stringPayload)
        {
            var isQueryParameters = false;
            var originalClassName = baseClassName;
            IList<CSPropertyDefinition> classesDict = new List<CSPropertyDefinition>();

            if (string.IsNullOrWhiteSpace(stringPayload))
            {

            }
            else if (int.TryParse(stringPayload, out _) || bool.TryParse(stringPayload, out _))
            {

            }
            else if (stringPayload.StartsWith("{") || stringPayload.StartsWith("["))
            {
                var currentClass = baseClassName;


                var payload = JsonConvert.DeserializeObject<dynamic>(stringPayload);

                if (payload is JObject jObject)
                {
                    var children = jObject.Children();

                    classesDict = ParseObject(in classesDict, in currentClass, children);
                }
                else if (payload is JArray jArray)
                {
                    ParseArray(in classesDict, in currentClass, null);
                }
            }
            else if (Regex.Match(stringPayload, "(?:&?[^=&]*=[^=&]*)*") is Match match && match.Success)
            {
                isQueryParameters = true;
                ParseQueryParameters(in classesDict, originalClassName, stringPayload);
            }

            var classNames = classesDict.Select(c => CleanupText(c.PropertyClass.Name)).Distinct().ToArray();
            var classPropertiesGrouped = classesDict.GroupBy(c => CleanupText(c.PropertyClass.Name));

            var generatedPropertiesCode = string.Join("\n", classNames.Select(x =>
            {
                var associatedProperties = classPropertiesGrouped
                .SelectMany(c => c.Where(y => y.PropertyClass.Name == x));

                var associatedPropertiesCode = associatedProperties
                .Select(p => $"[JsonProperty(\"{CleanupText(p.Name, false)}\")]\npublic {p.PropertyType} {CleanupText(p.Name)} {{ get; set; }}");

                var associatedPropertiesInitializers = associatedProperties
                .Select(a => $"\nthis.{CleanupText(a.Name)} = {ParseDefaultValue(a)};"); ;

                var constructor = $"public {CleanupText(x)}(){{\n{string.Join("\n", associatedPropertiesInitializers)}\n}}";

                return $"public class {CleanupText(x)} {{\n{string.Join("\n", associatedPropertiesCode)}\n\n{constructor}\n}}\n";
            }));

            var propertiesExport = classesDict
                .ToList()
                .AsReadOnly();

            var initiatorCode = $"var @{originalClassName}=new {originalClassName}();";
            var createRequestCode = $"var resp = this._wrapper.Post(\"{url.AbsoluteUri}\", @{originalClassName}, {isQueryParameters.ToString().ToLower()}).GetAwaiter().GetResult();\nAssert.That(resp.StatusCode == System.Net.HttpStatusCode.OK);";
            var unitTestCode = File.ReadAllText("UnitTest1.cs")
                .Replace("// Classes", generatedPropertiesCode)
                .Replace("// Instantiate", initiatorCode)
                .Replace("// Make Request", createRequestCode);

            if (Directory.Exists("export") == false)
                Directory.CreateDirectory("export");

            var randomFileName = Guid.NewGuid().ToString().Substring(0, 5);
            File.WriteAllText(randomFileName, unitTestCode);

            return new CSExport(propertiesExport, unitTestCode);
        }

        private void ParseQueryParameters(in IList<CSPropertyDefinition> classesDict, string currentClass, string stringPayload)
        {
            var queryParameters = stringPayload
                .Split("&");

            var queryParameterProperties = queryParameters.Select(q =>
            {
                var qp = q
                    .Split("=")
                    .ToArray();
                var classDefinition = new CSClassDefinition(currentClass);
                return new CSPropertyDefinition(classDefinition, qp[0], "string", qp[1], false);

            });

            foreach (var qpp in queryParameterProperties)
                classesDict.Add(qpp);
        }

        public ProxyRequestHandler()
        {
            this._requestBodies = new Dictionary<string, string>();
            this._server = new ProxyServer();

            var eep = new ExplicitProxyEndPoint(IPAddress.Any, 18884);
            this._server.AddEndPoint(eep);

            this._server.BeforeRequest += ProxyServerBeforeRequest;
            this._server.BeforeResponse += ProxyServerBeforeResponse;

            this._server.Start();
        }

    }
}