using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Evidences;

/// <summary>
/// Service for uploading and managing files on Cloudinary cloud storage.
/// Automatically handles both image and document files with appropriate optimization.
/// </summary>
public interface ICloudinaryService
{
    /// <summary>
    /// Upload evidence file to Cloudinary.
    /// 
    /// Automatically selects upload method based on file type:
    /// - IMAGE (.jpg, .png, .gif, .webp): Uses ImageUploadParams with auto-optimization
    /// - DOCUMENT (.pdf, .doc, .docx, .txt, .xls, .xlsx): Uses RawUploadParams to preserve original
    /// 
    /// Validates:
    /// - File size (max 10MB)
    /// - File extension
    /// - MIME type (prevents file tampering)
    /// 
    /// Returns secure HTTPS URL in preview mode (without fl_attachment).
    /// URL can be transformed by caller to add download behavior.
    /// </summary>
    /// <param name="file">The file to upload from student's device</param>
    /// <param name="studentCode">Student code for organizing files: evidences/{studentCode}/{timestamp}_{filename}</param>
    /// <returns>Cloudinary secure URL in preview mode</returns>
    Task<string> UploadEvidenceFileAsync(IFormFile file, string studentCode);

    /// <summary>
    /// Delete file from Cloudinary by public ID or URL.
    /// 
    /// Automatically handles:
    /// - Direct public IDs (e.g., "evidences/SE123456/20250101_120000_document")
    /// - Cloudinary URLs (extracts public ID from URL path)
    /// - Both image and raw resource types
    /// </summary>
    /// <param name="publicIdOrUrl">Public ID or full Cloudinary URL of file to delete</param>
    /// <returns>True if deletion succeeded, false otherwise</returns>
    Task<bool> DeleteFileAsync(string publicIdOrUrl);

    /// <summary>
    /// Test connection to Cloudinary and verify API credentials are valid.
    /// Uploads a tiny test file and immediately deletes it.
    /// </summary>
    /// <returns>True if connection successful, false otherwise</returns>
    Task<bool> TestConnectionAsync();
}
