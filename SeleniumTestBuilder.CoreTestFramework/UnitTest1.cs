using Newtonsoft.Json;

namespace SeleniumTestBuilder.CoreTestFramework
{
    public class Tests
    {
        // Classes

        private HttpClientWrapper.HttpClientWrapper _wrapper;

        [SetUp]
        public void Setup()
        {
            this._wrapper = new HttpClientWrapper.HttpClientWrapper();
        }

        [Test]
        public void ClassMethod()
        {
            // Instantiate
            // Make Request
        }
    }
}