using System.IO;
using System.Net.Http;

namespace Updater.Extensions
{
    public static class HttpClientExtensions
    {
        private const int MaxRetries = 3;
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Downloads the resource with the specified URI to a local file.
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="requestUri">The URI specified as a <see cref="String"/>, from which to download data.</param>
        /// <param name="fileName">The name of the local file that is to receive the data.</param>
        public static void DownloadFile(this HttpClient httpClient, string? requestUri, string fileName) =>
            DownloadFile(httpClient, CreateUri(requestUri), fileName);

        /// <summary>
        /// Downloads the resource with the specified URI to a local file.
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="requestUri">The URI from which to download data.</param>
        /// <param name="fileName">The name of the local file that is to receive the data.</param>
        public static void DownloadFile(this HttpClient httpClient, Uri? requestUri, string fileName)
        {
            Exception? lastException = null;

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    using var cts = new CancellationTokenSource(DefaultTimeout);
                    var bytes = httpClient.GetByteArrayAsync(requestUri, cts.Token).Result;
                    File.WriteAllBytes(fileName, bytes);
                    return;
                }
                catch (Exception ex) when (attempt < MaxRetries)
                {
                    lastException = ex;
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    Thread.Sleep(delay);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }

            if (lastException != null)
                throw new HttpRequestException($"Download failed after {MaxRetries} attempts: {requestUri}", lastException);
        }

        private static Uri? CreateUri(string? uri) =>
            string.IsNullOrEmpty(uri) ? null : new Uri(uri, UriKind.RelativeOrAbsolute);
    }
}
