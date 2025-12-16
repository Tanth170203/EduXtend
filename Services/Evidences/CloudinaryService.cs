using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Services.Evidences;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;

    private readonly long _maxFileSize = 10 * 1024 * 1024; // 10MB
    
    // Image extensions - will use ImageUploadParams
    private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    
    // Document extensions - will use RawUploadParams
    private readonly string[] _allowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".txt", ".xls", ".xlsx" };

    // MIME type validation mapping
    private readonly Dictionary<string, string[]> _mimeTypeMap = new()
    {
        { ".jpg", new[] { "image/jpeg" } },
        { ".jpeg", new[] { "image/jpeg" } },
        { ".png", new[] { "image/png" } },
        { ".gif", new[] { "image/gif" } },
        { ".webp", new[] { "image/webp" } },
        { ".pdf", new[] { "application/pdf" } },
        { ".doc", new[] { "application/msword" } },
        { ".docx", new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" } },
        { ".txt", new[] { "text/plain" } },
        { ".xls", new[] { "application/vnd.ms-excel" } },
        { ".xlsx", new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } }
    };

    public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
    {
        _logger = logger;

        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];
        if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
            throw new InvalidOperationException("Cloudinary configuration is missing. Please check appsettings.json");

        _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
    }

    private static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "file";
        var cleaned = Regex.Replace(input, @"\s+", "_");
        cleaned = string.Join("_", cleaned.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        return cleaned.Trim('_');
    }

    /// <summary>
    /// Upload file lên Cloudinary với xử lý tự động dựa trên loại file:
    /// - IMAGE (.jpg, .png, ...): ImageUploadParams → /upload/ với auto-optimize
    /// - DOCUMENT (.pdf, .doc, ...): RawUploadParams → /raw/upload/
    /// Trả về URL gốc (preview mode, không có fl_attachment)
    /// </summary>
    public async Task<string> UploadEvidenceFileAsync(IFormFile file, string studentCode)
    {
        try
        {
            // ===== VALIDATIONS =====
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null");
            if (file.Length > _maxFileSize)
                throw new ArgumentException($"File size exceeds maximum allowed size of {_maxFileSize / (1024 * 1024)}MB");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var isImage = _allowedImageExtensions.Contains(ext);
            var isDocument = _allowedDocumentExtensions.Contains(ext);

            if (!isImage && !isDocument)
                throw new ArgumentException($"File type {ext} is not allowed. Allowed: {string.Join(", ", _allowedImageExtensions.Concat(_allowedDocumentExtensions))}");

            // ===== MIME TYPE VALIDATION =====
            if (!ValidateMimeType(file, ext))
                throw new ArgumentException($"File MIME type does not match extension {ext}. Possible file tampering detected.");

            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            var safeName = Slugify(baseName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

            var folder = $"evidences/{studentCode}";
            var publicIdWithoutFolder = $"{timestamp}_{safeName}";

            using var stream = file.OpenReadStream();

            // ===== UPLOAD: IMAGE vs DOCUMENT =====
            UploadResult uploadResult;

            if (isImage)
            {
                // IMAGE: Use ImageUploadParams for proper resource type handling
                var imageParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder,
                    PublicId = publicIdWithoutFolder,
                    UseFilename = false,
                    Overwrite = false
                    // Note: Quality and format optimization can be added as transformation URL parameters
                };
                uploadResult = await _cloudinary.UploadAsync(imageParams);
                _logger.LogInformation("Uploaded IMAGE file to Cloudinary. PublicId={PublicId}, Size={Size}bytes", 
                    folder + "/" + publicIdWithoutFolder, file.Length);
            }
            else
            {
                // DOCUMENT: Use RawUploadParams to preserve original file
                var rawParams = new RawUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder,
                    PublicId = publicIdWithoutFolder,
                    UseFilename = false,
                    Overwrite = false
                };
                uploadResult = await _cloudinary.UploadAsync(rawParams);
                _logger.LogInformation("Uploaded DOCUMENT file to Cloudinary. PublicId={PublicId}, Type={FileType}, Size={Size}bytes", 
                    folder + "/" + publicIdWithoutFolder, ext, file.Length);
            }

            // ===== CHECK UPLOAD ERROR =====
            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {Error}", uploadResult.Error.Message);
                throw new Exception($"Upload failed: {uploadResult.Error.Message}");
            }

            // ===== RETURN URL & APPLY TRANSFORMATIONS =====
            var secureUrl = uploadResult.SecureUrl?.ToString();
            if (string.IsNullOrWhiteSpace(secureUrl))
            {
                var fallbackUrl = uploadResult.Url?.ToString();
                if (string.IsNullOrWhiteSpace(fallbackUrl))
                {
                    _logger.LogError("Cloudinary upload returned no URL. Response: {@UploadResult}", uploadResult);
                    throw new Exception("Cloudinary upload succeeded but returned no URL");
                }
                _logger.LogWarning("Using fallback HTTP URL instead of HTTPS");
                return fallbackUrl;
            }

            // For images, apply transformation for quality optimization
            if (isImage)
            {
                secureUrl = AddImageTransformations(secureUrl);
            }

            _logger.LogInformation("Successfully uploaded file. URL={Url}, ResourceType={ResourceType}", 
                secureUrl, isImage ? "image" : "raw");
            return secureUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to Cloudinary");
            throw;
        }
    }

    /// <summary>
    /// Upload activity image to Cloudinary in activities/ folder (root level).
    /// Only accepts image files. Automatically applies quality and format optimization.
    /// </summary>
    public async Task<string> UploadActivityImageAsync(IFormFile file)
    {
        try
        {
            // ===== VALIDATIONS =====
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null");
            if (file.Length > _maxFileSize)
                throw new ArgumentException($"File size exceeds maximum allowed size of {_maxFileSize / (1024 * 1024)}MB");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var isImage = _allowedImageExtensions.Contains(ext);

            if (!isImage)
                throw new ArgumentException($"Only image files are allowed for activities. Allowed: {string.Join(", ", _allowedImageExtensions)}");

            // ===== MIME TYPE VALIDATION =====
            if (!ValidateMimeType(file, ext))
                throw new ArgumentException($"File MIME type does not match extension {ext}. Possible file tampering detected.");

            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            var safeName = Slugify(baseName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

            // Upload to root-level "activities" folder
            var folder = "activities";
            var publicIdWithoutFolder = $"{timestamp}_{safeName}";

            using var stream = file.OpenReadStream();

            // IMAGE: Use ImageUploadParams for proper resource type handling
            var imageParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                PublicId = publicIdWithoutFolder,
                UseFilename = false,
                Overwrite = false
            };
            var uploadResult = await _cloudinary.UploadAsync(imageParams);
            
            _logger.LogInformation("Uploaded activity image to Cloudinary. PublicId={PublicId}, Size={Size}bytes", 
                folder + "/" + publicIdWithoutFolder, file.Length);

            // ===== CHECK UPLOAD ERROR =====
            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {Error}", uploadResult.Error.Message);
                throw new Exception($"Upload failed: {uploadResult.Error.Message}");
            }

            // ===== RETURN URL & APPLY TRANSFORMATIONS =====
            var secureUrl = uploadResult.SecureUrl?.ToString();
            if (string.IsNullOrWhiteSpace(secureUrl))
            {
                var fallbackUrl = uploadResult.Url?.ToString();
                if (string.IsNullOrWhiteSpace(fallbackUrl))
                {
                    _logger.LogError("Cloudinary upload returned no URL. Response: {@UploadResult}", uploadResult);
                    throw new Exception("Cloudinary upload succeeded but returned no URL");
                }
                _logger.LogWarning("Using fallback HTTP URL instead of HTTPS");
                return fallbackUrl;
            }

            // Apply transformation for quality optimization
            secureUrl = AddImageTransformations(secureUrl);

            _logger.LogInformation("Successfully uploaded activity image. URL={Url}", secureUrl);
            return secureUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading activity image to Cloudinary");
            throw;
        }
    }

    /// <summary>
    /// Upload news image to Cloudinary in news/ folder (root level).
    /// Only accepts image files. Automatically applies quality and format optimization.
    /// </summary>
    public async Task<string> UploadNewsImageAsync(IFormFile file)
    {
        try
        {
            // ===== VALIDATIONS =====
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null");
            if (file.Length > _maxFileSize)
                throw new ArgumentException($"File size exceeds maximum allowed size of {_maxFileSize / (1024 * 1024)}MB");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var isImage = _allowedImageExtensions.Contains(ext);

            if (!isImage)
                throw new ArgumentException($"Only image files are allowed for news. Allowed: {string.Join(", ", _allowedImageExtensions)}");

            // ===== MIME TYPE VALIDATION =====
            if (!ValidateMimeType(file, ext))
                throw new ArgumentException($"File MIME type does not match extension {ext}. Possible file tampering detected.");

            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            var safeName = Slugify(baseName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

            // Upload to root-level "news" folder
            var folder = "news";
            var publicIdWithoutFolder = $"{timestamp}_{safeName}";

            using var stream = file.OpenReadStream();

            // IMAGE: Use ImageUploadParams for proper resource type handling
            var imageParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                PublicId = publicIdWithoutFolder,
                UseFilename = false,
                Overwrite = false
            };
            var uploadResult = await _cloudinary.UploadAsync(imageParams);
            
            _logger.LogInformation("Uploaded news image to Cloudinary. PublicId={PublicId}, Size={Size}bytes", 
                folder + "/" + publicIdWithoutFolder, file.Length);

            // ===== CHECK UPLOAD ERROR =====
            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {Error}", uploadResult.Error.Message);
                throw new Exception($"Upload failed: {uploadResult.Error.Message}");
            }

            // ===== RETURN URL & APPLY TRANSFORMATIONS =====
            var secureUrl = uploadResult.SecureUrl?.ToString();
            if (string.IsNullOrWhiteSpace(secureUrl))
            {
                var fallbackUrl = uploadResult.Url?.ToString();
                if (string.IsNullOrWhiteSpace(fallbackUrl))
                {
                    _logger.LogError("Cloudinary upload returned no URL. Response: {@UploadResult}", uploadResult);
                    throw new Exception("Cloudinary upload succeeded but returned no URL");
                }
                _logger.LogWarning("Using fallback HTTP URL instead of HTTPS");
                return fallbackUrl;
            }

            // Apply transformation for quality optimization
            secureUrl = AddImageTransformations(secureUrl);

            _logger.LogInformation("Successfully uploaded news image. URL={Url}", secureUrl);
            return secureUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading news image to Cloudinary");
            throw;
        }
    }

    /// <summary>
    /// Upload user avatar to Cloudinary in avatars/ folder (root level).
    /// Only accepts image files. Automatically applies quality and format optimization.
    /// </summary>
    public async Task<string> UploadAvatarAsync(IFormFile file)
    {
        try
        {
            // ===== VALIDATIONS =====
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null");
            if (file.Length > _maxFileSize)
                throw new ArgumentException($"File size exceeds maximum allowed size of {_maxFileSize / (1024 * 1024)}MB");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var isImage = _allowedImageExtensions.Contains(ext);

            if (!isImage)
                throw new ArgumentException($"Only image files are allowed for avatars. Allowed: {string.Join(", ", _allowedImageExtensions)}");

            // ===== MIME TYPE VALIDATION =====
            if (!ValidateMimeType(file, ext))
                throw new ArgumentException($"File MIME type does not match extension {ext}. Possible file tampering detected.");

            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            var safeName = Slugify(baseName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

            // Upload to root-level "avatars" folder
            var folder = "avatars";
            var publicIdWithoutFolder = $"{timestamp}_{safeName}";

            using var stream = file.OpenReadStream();

            // IMAGE: Use ImageUploadParams for proper resource type handling
            var imageParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                PublicId = publicIdWithoutFolder,
                UseFilename = false,
                Overwrite = false
            };
            var uploadResult = await _cloudinary.UploadAsync(imageParams);
            
            _logger.LogInformation("Uploaded avatar image to Cloudinary. PublicId={PublicId}, Size={Size}bytes", 
                folder + "/" + publicIdWithoutFolder, file.Length);

            // ===== CHECK UPLOAD ERROR =====
            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {Error}", uploadResult.Error.Message);
                throw new Exception($"Upload failed: {uploadResult.Error.Message}");
            }

            // ===== RETURN URL & APPLY TRANSFORMATIONS =====
            var secureUrl = uploadResult.SecureUrl?.ToString();
            if (string.IsNullOrWhiteSpace(secureUrl))
            {
                var fallbackUrl = uploadResult.Url?.ToString();
                if (string.IsNullOrWhiteSpace(fallbackUrl))
                {
                    _logger.LogError("Cloudinary upload returned no URL. Response: {@UploadResult}", uploadResult);
                    throw new Exception("Cloudinary upload succeeded but returned no URL");
                }
                _logger.LogWarning("Using fallback HTTP URL instead of HTTPS");
                return fallbackUrl;
            }

            // Apply transformation for quality optimization
            secureUrl = AddImageTransformations(secureUrl);

            _logger.LogInformation("Successfully uploaded avatar image. URL={Url}", secureUrl);
            return secureUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading avatar image to Cloudinary");
            throw;
        }
    }

    /// <summary>
    /// Upload club logo or banner to Cloudinary in clubs/ folder (root level).
    /// Only accepts image files. Automatically applies quality and format optimization.
    /// </summary>
    public async Task<string> UploadClubImageAsync(IFormFile file)
    {
        try
        {
            // ===== VALIDATIONS =====
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null");
            if (file.Length > _maxFileSize)
                throw new ArgumentException($"File size exceeds maximum allowed size of {_maxFileSize / (1024 * 1024)}MB");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var isImage = _allowedImageExtensions.Contains(ext);

            if (!isImage)
                throw new ArgumentException($"Only image files are allowed for clubs. Allowed: {string.Join(", ", _allowedImageExtensions)}");

            // ===== MIME TYPE VALIDATION =====
            if (!ValidateMimeType(file, ext))
                throw new ArgumentException($"File MIME type does not match extension {ext}. Possible file tampering detected.");

            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            var safeName = Slugify(baseName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

            // Upload to root-level "clubs" folder
            var folder = "clubs";
            var publicIdWithoutFolder = $"{timestamp}_{safeName}";

            using var stream = file.OpenReadStream();

            // IMAGE: Use ImageUploadParams for proper resource type handling
            var imageParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                PublicId = publicIdWithoutFolder,
                UseFilename = false,
                Overwrite = false
            };
            var uploadResult = await _cloudinary.UploadAsync(imageParams);
            
            _logger.LogInformation("Uploaded club image to Cloudinary. PublicId={PublicId}, Size={Size}bytes", 
                folder + "/" + publicIdWithoutFolder, file.Length);

            // ===== CHECK UPLOAD ERROR =====
            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {Error}", uploadResult.Error.Message);
                throw new Exception($"Upload failed: {uploadResult.Error.Message}");
            }

            // ===== RETURN URL & APPLY TRANSFORMATIONS =====
            var secureUrl = uploadResult.SecureUrl?.ToString();
            if (string.IsNullOrWhiteSpace(secureUrl))
            {
                var fallbackUrl = uploadResult.Url?.ToString();
                if (string.IsNullOrWhiteSpace(fallbackUrl))
                {
                    _logger.LogError("Cloudinary upload returned no URL. Response: {@UploadResult}", uploadResult);
                    throw new Exception("Cloudinary upload succeeded but returned no URL");
                }
                _logger.LogWarning("Using fallback HTTP URL instead of HTTPS");
                return fallbackUrl;
            }

            // Apply transformation for quality optimization
            secureUrl = AddImageTransformations(secureUrl);

            _logger.LogInformation("Successfully uploaded club image. URL={Url}", secureUrl);
            return secureUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading club image to Cloudinary");
            throw;
        }
    }

    /// <summary>
    /// Upload receipt/invoice image to Cloudinary in receipts/ folder (root level).
    /// Only accepts image files. Automatically applies quality and format optimization.
    /// </summary>
    public async Task<string> UploadReceiptImageAsync(IFormFile file)
    {
        try
        {
            // ===== VALIDATIONS =====
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null");
            if (file.Length > _maxFileSize)
                throw new ArgumentException($"File size exceeds maximum allowed size of {_maxFileSize / (1024 * 1024)}MB");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var isImage = _allowedImageExtensions.Contains(ext);

            if (!isImage)
                throw new ArgumentException($"Only image files are allowed for receipts. Allowed: {string.Join(", ", _allowedImageExtensions)}");

            // ===== MIME TYPE VALIDATION =====
            if (!ValidateMimeType(file, ext))
                throw new ArgumentException($"File MIME type does not match extension {ext}. Possible file tampering detected.");

            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            var safeName = Slugify(baseName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

            // Upload to root-level "receipts" folder
            var folder = "receipts";
            var publicIdWithoutFolder = $"{timestamp}_{safeName}";

            using var stream = file.OpenReadStream();

            // IMAGE: Use ImageUploadParams for proper resource type handling
            var imageParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                PublicId = publicIdWithoutFolder,
                UseFilename = false,
                Overwrite = false
            };
            var uploadResult = await _cloudinary.UploadAsync(imageParams);
            
            _logger.LogInformation("Uploaded receipt image to Cloudinary. PublicId={PublicId}, Size={Size}bytes", 
                folder + "/" + publicIdWithoutFolder, file.Length);

            // ===== CHECK UPLOAD ERROR =====
            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload error: {Error}", uploadResult.Error.Message);
                throw new Exception($"Upload failed: {uploadResult.Error.Message}");
            }

            // ===== RETURN URL & APPLY TRANSFORMATIONS =====
            var secureUrl = uploadResult.SecureUrl?.ToString();
            if (string.IsNullOrWhiteSpace(secureUrl))
            {
                var fallbackUrl = uploadResult.Url?.ToString();
                if (string.IsNullOrWhiteSpace(fallbackUrl))
                {
                    _logger.LogError("Cloudinary upload returned no URL. Response: {@UploadResult}", uploadResult);
                    throw new Exception("Cloudinary upload succeeded but returned no URL");
                }
                _logger.LogWarning("Using fallback HTTP URL instead of HTTPS");
                return fallbackUrl;
            }

            // Apply transformation for quality optimization
            secureUrl = AddImageTransformations(secureUrl);

            _logger.LogInformation("Successfully uploaded receipt image. URL={Url}", secureUrl);
            return secureUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading receipt image to Cloudinary");
            throw;
        }
    }

    /// <summary>
    /// Add quality and format transformations to image URL for optimization.
    /// Cloudinary will serve optimized version based on browser capabilities.
    /// </summary>
    private static string AddImageTransformations(string url)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            
            // Insert transformation parameters after /upload/
            const string uploadMarker = "/upload/";
            var idx = url.IndexOf(uploadMarker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return url;
            
            // Transformation: q_auto (auto quality), f_auto (auto format)
            var transformations = "q_auto,f_auto/";
            var insertPos = idx + uploadMarker.Length;
            
            return url.Insert(insertPos, transformations);
        }
        catch (Exception)
        {
            // If transformation fails, return original URL
            return url;
        }
    }

    /// <summary>
    /// Validate MIME type matches the file extension
    /// </summary>
    private bool ValidateMimeType(IFormFile file, string ext)
    {
        try
        {
            if (!_mimeTypeMap.TryGetValue(ext, out var allowedMimeTypes))
                return true; // Unknown extension - let other validations catch it

            var fileMimeType = file.ContentType?.ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(fileMimeType))
            {
                _logger.LogWarning("File {FileName} has no ContentType header. Extension={Ext}", file.FileName, ext);
                return false;
            }

            var isValid = allowedMimeTypes.Any(mt => fileMimeType.Equals(mt, StringComparison.OrdinalIgnoreCase));
            if (!isValid)
            {
                _logger.LogWarning("MIME type mismatch for {FileName}. Expected={Expected}, Got={Got}", 
                    file.FileName, string.Join(", ", allowedMimeTypes), fileMimeType);
            }
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating MIME type for {FileName}", file.FileName);
            return false;
        }
    }

    public async Task<bool> DeleteFileAsync(string publicIdOrUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(publicIdOrUrl))
            {
                _logger.LogWarning("Public ID/URL is null or empty");
                return false;
            }

            string publicId = publicIdOrUrl;

            if (publicIdOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var extracted = ExtractPublicIdFromUrl(publicIdOrUrl);
                if (string.IsNullOrEmpty(extracted))
                {
                    _logger.LogWarning("Cannot extract publicId from URL: {Url}", publicIdOrUrl);
                    return false;
                }
                publicId = extracted;
            }

            // ===== DETERMINE RESOURCE TYPE =====
            // Try raw first (documents), then fall back to auto-detection
            var del = new DeletionParams(publicId) { ResourceType = ResourceType.Raw };
            var res = await _cloudinary.DestroyAsync(del);
            
            if (res.Result == "ok")
            {
                _logger.LogInformation("Successfully deleted RAW file. PublicId={PublicId}", publicId);
                return true;
            }

            // If raw delete failed, try image (in case URL was image)
            if (res.Error?.Message?.Contains("not found") == true || res.Result == "not_found")
            {
                del = new DeletionParams(publicId) { ResourceType = ResourceType.Image };
                res = await _cloudinary.DestroyAsync(del);
                if (res.Result == "ok")
                {
                    _logger.LogInformation("Successfully deleted IMAGE file. PublicId={PublicId}", publicId);
                    return true;
                }
            }

            _logger.LogWarning("Failed to delete file. publicId={PublicId}, result={Result}, error={Error}", 
                publicId, res.Result, res.Error?.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from Cloudinary");
            return false;
        }
    }

    public static string? ExtractPublicIdFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        try
        {
            var u = new Uri(url);
            var parts = u.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            // Find "upload" marker (/upload/ for images or /raw/upload/ for documents)
            var uploadIdx = Array.FindIndex(parts, s => s.Equals("upload", StringComparison.OrdinalIgnoreCase));
            if (uploadIdx < 0) return null;

            var afterUpload = parts.Skip(uploadIdx + 1).ToArray(); // [v123, folder, filename, ...]
            if (afterUpload.Length < 2) return null;

            // Skip version (v123) and build public ID from remaining parts
            var withoutVersion = afterUpload.Skip(1).ToArray();
            if (withoutVersion.Length == 0) return null;

            var joined = string.Join('/', withoutVersion);
            
            // Remove file extension and transformation parameters
            var lastDot = joined.LastIndexOf('.');
            if (lastDot > 0)
            {
                joined = joined[..lastDot];
            }
            
            return joined;
        }
        catch (Exception)
        {
            // Parse error - return null for invalid URL
            return null;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            _logger.LogInformation("Testing Cloudinary connection by uploading tiny test file...");
            var bytes = new byte[] { 0x41 };
            using var ms = new MemoryStream(bytes);

            var p = new RawUploadParams
            {
                File = new FileDescription("test.bin", ms),
                Folder = "test",
                PublicId = $"connection_test_{DateTime.UtcNow:yyyyMMddHHmmss}",
                Overwrite = true
            };

            var r = await _cloudinary.UploadAsync(p);
            if (r.Error != null)
            {
                _logger.LogError("Cloudinary connection test failed: {Error}", r.Error.Message);
                return false;
            }

            await DeleteFileAsync($"test/{p.PublicId}");
            _logger.LogInformation("Cloudinary connection OK ✓");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cloudinary connection test failed");
            return false;
        }
    }
}
