namespace WishAndGet
{
    public class PageSchemaOrgGrabber
    {
        private readonly BrowserAccessor browserAccessor;
        private readonly Lazy<string> grabScriptFile = new(ReadGrabScriptFile, true);

        public PageSchemaOrgGrabber(BrowserAccessor browserAccessor)
        {
            this.browserAccessor = browserAccessor;
        }

        public async Task<List<string>> GrabAsync(string url, CancellationToken cancellationToken = default)
        {
            var browser = await browserAccessor.GetBrowserAsync().AnyContext();
            await using var page = await browser.NewPageAsync().AnyContext();
            var response = await page.GoToAsync(url).AnyContext();
            if (!response.Ok)
                return new List<string>();

            await page.EvaluateExpressionAsync<object>(grabScriptFile.Value).AnyContext();
            var schemaData = await page.EvaluateFunctionAsync<List<string>>("grabData").AnyContext();

            return schemaData;
        }

        static string ReadGrabScriptFile()
        {
            var microdataScriptFile = ReadFile("microdata.js");
            var grabScriptFile = ReadFile("grab.js");

            return string.Concat(microdataScriptFile, grabScriptFile);
        }

        static string ReadFile(string fileName)
        {
            var currentDir = Environment.CurrentDirectory;
            return File.ReadAllText(Path.Combine(currentDir, "content", fileName));
        }
    }
}
