using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using SeleniumTestBuilder;
using SeleniumTestBuilder.Models;

namespace SelniumTestBuilder.TestConsole
{
    public static class Program
    {
        public static void Main(string[] args)
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

            using (var handler = new ProxyRequestHandler())
            {
                //var payload = new
                //{
                //    Username = "MontrayDavis",
                //    Password = "Resitrader",
                //    Info = new
                //    {
                //        Email = "montraydavis@gmail.com",
                //        Address = new
                //        {
                //            AddressLine1 = "2347 Bluffstone DR, Round Rock, Texas -- 78665"
                //        },
                //        Nicknames = new object[] { 1, 2, "3" },
                //        Friends = new[]
                //        {
                //            new
                //            {
                //                Name = "John",
                //                Age = 16
                //            }
                //        }
                //    }
                //};

                //var builder = handler.ParsePost(new Uri("https://courses.ultimateqa.com/users/sign_in;"), "Test", JsonConvert.SerializeObject(payload));

                while (Console.ReadKey().Key != ConsoleKey.Escape)
                {
                    // Hit Escape Key To Quit
                }

                selenium.Quit();
            }
        }
    }
}