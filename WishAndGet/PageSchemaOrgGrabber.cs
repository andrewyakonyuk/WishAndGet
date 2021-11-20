namespace WishAndGet
{
    public class PageSchemaOrgGrabber
    {
        private readonly BrowserContextAccessor browserAccessor;
        private readonly Lazy<string> grabScriptFile = new(ReadGrabScriptFile, true);
        const string UserAgent = "Mozilla/5.0 (Linux; Android 6.0.1; Nexus 5X Build/MMB29P) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/W.X.Y.Z Mobile Safari/537.36 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";

        public PageSchemaOrgGrabber(BrowserContextAccessor browserAccessor)
        {
            this.browserAccessor = browserAccessor;
        }

        public async Task<List<string>> GrabAsync(string url, CancellationToken cancellationToken = default)
        {
            var browserContext = await browserAccessor.GetBrowserContextAsync();

            await using var page = await browserContext.NewPageAsync();
            await page.SetUserAgentAsync(UserAgent);
            var response = await page.GoToAsync(url).AnyContext();
            if (!response.Ok)
            {
                await page.CloseAsync();
                return new List<string>();
            }

            await page.EvaluateExpressionAsync<object>(grabScriptFile.Value);
            var schemaData = await page.EvaluateFunctionAsync<List<string>>("grabData");

            await page.CloseAsync();

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
            return File.ReadAllText(Path.Combine(currentDir, "assets", fileName));
        }
    }
}
