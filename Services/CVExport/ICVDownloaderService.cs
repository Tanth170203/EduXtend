namespace Services.CVExport
{
    public interface ICVDownloaderService
    {
        Task<(byte[] fileData, string extension)?> DownloadCVAsync(string cvUrl);
        bool IsSupportedFormat(string url);
    }
}
