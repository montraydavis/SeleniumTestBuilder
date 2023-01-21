using OpenQA.Selenium.Chrome;
using SeleniumTestBuilder;

namespace SelniumTestBuilder.TestConsole
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var bootstrap = new Bootstrap();
            var selenium = bootstrap.Resolve<ChromeDriver>();

            using (var handler = bootstrap.Resolve<IProxyRequestHandler>())
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

                //var builder = handler.Capture(new Uri("https://courses.ultimateqa.com/users/sign_in;"), "Test", JsonConvert.SerializeObject(payload));

                while (Console.ReadKey().Key != ConsoleKey.Escape)
                {
                    // Hit Escape Key To Quit
                }

                selenium.Quit();
            }
        }
    }
}