using PuppeteerSharp;
using WishAndGet.Infrastructure;

namespace WishAndGet
{
    public class BrowserAccessor : IAsyncDisposable
    {
        private readonly AsyncLazy<Browser> browser = new(InitializeBrowser);

        private static async Task<Browser> InitializeBrowser()
        {
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync().AnyContext();
            return await Puppeteer.LaunchAsync(
                new LaunchOptions { Headless = true }).AnyContext();
        }

        public Task<Browser> GetBrowserAsync() => browser.Value;

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            GC.SuppressFinalize(this);
            if (browser.IsValueCreated)
            {
                var value = await browser.Value.AnyContext();
                await value.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
