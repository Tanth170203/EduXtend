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
    /// Upload activity image to Cloudinary.
    /// Images are stored in activities/ folder (root level, not under evidences/).
    /// Only accepts image files (.jpg, .png, .gif, .webp).
    /// Automatically applies optimization (q_auto, f_auto).
    /// </summary>
    /// <param name="file">The image file to upload</param>
    /// <returns>Cloudinary secure URL with auto-optimization</returns>
    Task<string> UploadActivityImageAsync(IFormFile file);

    /// <summary>
    /// Upload news image to Cloudinary.
    /// Images are stored in news/ folder (root level, not under evidences/).
    /// Only accepts image files (.jpg, .png, .gif, .webp).
    /// Automatically applies optimization (q_auto, f_auto).
    /// </summary>
    /// <param name="file">The image file to upload</param>
    /// <returns>Cloudinary secure URL with auto-optimization</returns>
    Task<string> UploadNewsImageAsync(IFormFile file);

    /// <summary>
    /// Upload user avatar to Cloudinary.
    /// Images are stored in avatars/ folder (root level, not under evidences/).
    /// Only accepts image files (.jpg, .png, .gif, .webp).
    /// Automatically applies optimization (q_auto, f_auto).
    /// </summary>
    /// <param name="file">The image file to upload</param>
    /// <returns>Cloudinary secure URL with auto-optimization</returns>
    Task<string> UploadAvatarAsync(IFormFile file);

    /// <summary>
    /// Upload club logo or banner to Cloudinary.
    /// Images are stored in clubs/ folder (root level, not under evidences/).
    /// Only accepts image files (.jpg, .png, .gif, .webp).
    /// Automatically applies optimization (q_auto, f_auto).
    /// </summary>
    /// <param name="file">The image file to upload</param>
    /// <returns>Cloudinary secure URL with auto-optimization</returns>
    Task<string> UploadClubImageAsync(IFormFile file);

    /// <summary>
    /// Upload receipt/invoice image to Cloudinary.
    /// Images are stored in receipts/ folder (root level).
    /// Only accepts image files (.jpg, .png, .gif, .webp, .pdf).
    /// Automatically applies optimization (q_auto, f_auto).
    /// </summary>
    /// <param name="file">The receipt image file to upload</param>
    /// <returns>Cloudinary secure URL with auto-optimization</returns>
    Task<string> UploadReceiptImageAsync(IFormFile file);

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
