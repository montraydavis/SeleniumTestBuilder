using SeleniumTestBuilder.TestConsole.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeleniumTestBuilder.Models
{
    public class CSExport
    {
        public IReadOnlyCollection<CSPropertyDefinition> Properties { get; set; }
        public string Code { get; set; }

        public CSExport(IReadOnlyCollection<CSPropertyDefinition> properties, string code)
        {
            Properties = properties;
            Code = code;
        }
    }
}
