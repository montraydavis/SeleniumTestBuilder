using Autofac;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SeleniumTestBuilder
{
    public class Bootstrap
    {
        private readonly Autofac.IContainer Container;
        public T Resolve<T>() where T : class
        {
            if (this.Container != null
                && this.Container?.Resolve<T>() is T resolution)
            {
                return resolution;
            }

            throw new Exception("Cannot resolve type.");
        }
        public Bootstrap()
        {
            var builder = new ContainerBuilder();
            builder.Register<ChromeDriver>((driver) =>
            {
                var proxy = new Proxy()
                {
                    HttpProxy = "http://localhost:18884",
                    SslProxy = "http://localhost:18884",
                    FtpProxy = "http://localhost:18884"
                };
                var chromeOptions = new ChromeOptions()
                {
                    Proxy = proxy
                };
                var selenium = new ChromeDriver(chromeOptions);

                return selenium;
            }).As<ChromeDriver>();
            builder.RegisterType<ProxyRequestHandler>().As<IProxyRequestHandler>();
            Container = builder.Build();
        }

    }
}
