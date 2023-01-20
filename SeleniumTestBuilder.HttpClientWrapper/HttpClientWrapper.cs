using Newtonsoft.Json;
using System.Text;

namespace SeleniumTestBuilder.HttpClientWrapper
{
    public class HttpClientWrapper
    {
        public async Task<T> Get<T>(string endpoint)
        {
            var resp = await this.RawRequest<T>(HttpMethod.Get, endpoint, null, false);

            return resp;
        }

        public async Task<T> Post<T>(string endpoint, object content, bool isQueryParameters)
        {
            var resp = await this.RawRequest<T>(HttpMethod.Post, endpoint, content, isQueryParameters);

            return resp;
        }

        public async Task<T> Patch<T>(string endpoint, object content, bool isQueryParameters)
        {
            var resp = await this.RawRequest<T>(HttpMethod.Post, endpoint, content, isQueryParameters);

            return resp;
        }

        public async Task<T> Delete<T>(string endpoint, object content, bool isQueryParameters)
        {
            var resp = await this.RawRequest<T>(HttpMethod.Post, endpoint, content, isQueryParameters);

            return resp;
        }

        public async Task<T> RawRequest<T>(HttpMethod method, string endpoint, object content, bool isQueryParameters)
        {
            var httpClient = new HttpClient();
            var contentDict = isQueryParameters ? JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(content)).ToList() : null;
            var requestContent = isQueryParameters && contentDict != null ? new FormUrlEncodedContent(contentDict) as HttpContent : new StringContent(JsonConvert.SerializeObject(content)) as HttpContent;

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = method == HttpMethod.Get ? null : requestContent,
                Method = method,
                RequestUri = new Uri(endpoint)
            };

            var response = await httpClient.SendAsync(request);
            var value = JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());

            if (value == null)
                throw new Exception("Cannot deserialize response.");

            return value;
        }
    }
}