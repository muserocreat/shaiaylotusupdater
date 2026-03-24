using System.IO;
using System.Net.Http;

namespace Updater.Extensions
{
    public static class HttpClientExtensions
    {
        private const int MaxRetries = 3;
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(300); // 5 minutes for large updates

        /// <summary>
        /// Downloads the resource with the specified URI to a local file.
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="requestUri">The URI specified as a <see cref="String"/>, from which to download data.</param>
        /// <param name="fileName">The name of the local file that is to receive the data.</param>
        /// <param name="progressAction">Action to report progress (bytesReceived, totalBytes).</param>
        public static void DownloadFile(this HttpClient httpClient, string? requestUri, string fileName, Action<long, long>? progressAction = null) =>
            DownloadFile(httpClient, CreateUri(requestUri), fileName, progressAction);

        /// <summary>
        /// Downloads the resource with the specified URI to a local file.
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="requestUri">The URI from which to download data.</param>
        /// <param name="fileName">The name of the local file that is to receive the data.</param>
        /// <param name="progressAction">Action to report progress (bytesReceived, totalBytes).</param>
        public static void DownloadFile(this HttpClient httpClient, Uri? requestUri, string fileName, Action<long, long>? progressAction = null)
        {
            Exception? lastException = null;

            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    using var cts = new CancellationTokenSource(DefaultTimeout);
                    var response = httpClient.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cts.Token).Result;
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    using var contentStream = response.Content.ReadAsStreamAsync(cts.Token).Result;
                    using var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                    var buffer = new byte[8192];
                    long bytesReadTotal = 0;
                    int read;

                    while ((read = contentStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, read);
                        bytesReadTotal += read;

                        if (progressAction != null && totalBytes > 0)
                        {
                            progressAction(bytesReadTotal, totalBytes);
                        }
                    }

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
