using Microsoft.Extensions.Logging;

namespace Services.CVExport
{
    public class CVDownloaderService : ICVDownloaderService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CVDownloaderService> _logger;
        private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
        private const int TimeoutSeconds = 30;

        public CVDownloaderService(
            IHttpClientFactory httpClientFactory,
            ILogger<CVDownloaderService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<(byte[] fileData, string extension)?> DownloadCVAsync(string cvUrl)
        {
            try
            {
                // Validate URL
                if (string.IsNullOrWhiteSpace(cvUrl))
                {
                    _logger.LogWarning("CV URL is null or empty");
                    return null;
                }

                if (!Uri.TryCreate(cvUrl, UriKind.Absolute, out var uri))
                {
                    _logger.LogWarning("Invalid CV URL format: {Url}", cvUrl);
                    return null;
                }

                // Security: Only allow HTTPS URLs
                if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
                {
                    _logger.LogWarning("CV URL must use HTTP or HTTPS protocol: {Url}", cvUrl);
                    return null;
                }

                // Check if format is supported
                if (!IsSupportedFormat(cvUrl))
                {
                    _logger.LogWarning("Unsupported file format: {Url}", cvUrl);
                    return null;
                }

                // Get file extension
                var extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
                
                // For Cloudinary URLs, try to detect extension from URL path
                if (string.IsNullOrEmpty(extension) && cvUrl.Contains("cloudinary"))
                {
                    // Try to find .pdf or .docx in the URL
                    if (cvUrl.Contains(".pdf", StringComparison.OrdinalIgnoreCase))
                        extension = ".pdf";
                    else if (cvUrl.Contains(".docx", StringComparison.OrdinalIgnoreCase))
                        extension = ".docx";
                    else if (cvUrl.Contains(".doc", StringComparison.OrdinalIgnoreCase))
                        extension = ".doc";
                }
                
                if (string.IsNullOrEmpty(extension))
                {
                    _logger.LogWarning("Could not determine file extension from URL: {Url}", cvUrl);
                    return null;
                }

                // Download file
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

                _logger.LogInformation("Downloading CV from: {Url}", cvUrl);
                
                using var response = await httpClient.GetAsync(uri);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to download CV. Status: {Status}, URL: {Url}", 
                        response.StatusCode, cvUrl);
                    return null;
                }

                // Check content length
                if (response.Content.Headers.ContentLength.HasValue &&
                    response.Content.Headers.ContentLength.Value > MaxFileSizeBytes)
                {
                    _logger.LogWarning("CV file too large: {Size} bytes, URL: {Url}", 
                        response.Content.Headers.ContentLength.Value, cvUrl);
                    return null;
                }

                var fileData = await response.Content.ReadAsByteArrayAsync();

                // Double-check size after download
                if (fileData.Length > MaxFileSizeBytes)
                {
                    _logger.LogWarning("Downloaded CV file too large: {Size} bytes, URL: {Url}", 
                        fileData.Length, cvUrl);
                    return null;
                }
                
                // Check if we actually got a file (not HTML error page)
                if (fileData.Length < 100)
                {
                    _logger.LogWarning("Downloaded file too small (possibly error page): {Size} bytes, URL: {Url}", 
                        fileData.Length, cvUrl);
                    return null;
                }
                
                // Check content type
                var contentType = response.Content.Headers.ContentType?.MediaType;
                _logger.LogInformation("Downloaded CV: {Size} bytes, ContentType: {ContentType}, Extension: {Ext}, URL: {Url}", 
                    fileData.Length, contentType, extension, cvUrl);
                
                // If content type is HTML, it's probably an error page
                if (contentType?.Contains("html", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _logger.LogWarning("Downloaded content is HTML (error page?), URL: {Url}", cvUrl);
                    return null;
                }

                return (fileData, extension);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error downloading CV from: {Url}", cvUrl);
                return null;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Timeout downloading CV from: {Url}", cvUrl);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error downloading CV from: {Url}", cvUrl);
                return null;
            }
        }

        public bool IsSupportedFormat(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            var extension = Path.GetExtension(url).ToLowerInvariant();
            return extension == ".pdf" || extension == ".docx";
        }
    }
}
