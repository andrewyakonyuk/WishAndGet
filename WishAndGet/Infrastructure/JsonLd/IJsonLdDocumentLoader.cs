namespace WishAndGet.Infrastructure.JsonLd
{
    public interface IJsonLdDocumentLoader
    {
        Task<RemoteDocument> LoadDocumentAsync(string url, CancellationToken token = default);
    }
}
