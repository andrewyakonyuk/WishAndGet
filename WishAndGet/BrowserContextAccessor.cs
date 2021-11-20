using PuppeteerSharp;
using WishAndGet.Infrastructure;

namespace WishAndGet
{
    public class BrowserContextAccessor : IAsyncDisposable
    {
        private readonly AsyncLazy<BrowserContext> browserContext = new(InitializeBrowserContext);

        private static async Task<BrowserContext> InitializeBrowserContext()
        {
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync().AnyContext();
            var browser = await Puppeteer.LaunchAsync(
                new LaunchOptions { Headless = true });

            return await browser.CreateIncognitoBrowserContextAsync();
        }

        public Task<BrowserContext> GetBrowserContextAsync() => browserContext.Value;

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            GC.SuppressFinalize(this);
            if (browserContext.IsValueCreated)
            {
                var value = await browserContext.Value.AnyContext();
                await value.CloseAsync();
                await value.Browser.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
