namespace SeleniumTestBuilder.TestConsole.Models
{
    public sealed class CSPropertyDefinition
    {
        public CSClassDefinition PropertyClass { get; set; }
        public string Name { get; set; }
        public string PropertyType { get; set; }
        public string? DefaultValue { get; set; }
        public bool IsArray { get; set; }

        public CSPropertyDefinition(CSClassDefinition propertyClass, string name, string propertyType, string? defaultValue, bool isArray)
        {
            this.PropertyClass = propertyClass;
            this.Name = name;
            this.PropertyType = propertyType;
            this.DefaultValue = defaultValue;
            this.IsArray = isArray;
        }
    }
}
