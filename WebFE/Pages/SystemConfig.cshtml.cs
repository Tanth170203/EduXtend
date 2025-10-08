using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessObject.DTOs.Semester;
using WebFE.Services;

namespace WebFE.Pages
{
    public class SystemConfigModel : PageModel
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<SystemConfigModel> _logger;

        public SystemConfigModel(IApiClient apiClient, ILogger<SystemConfigModel> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public List<SemesterDto> Semesters { get; set; } = new();
        
        [TempData]
        public string? SuccessMessage { get; set; }
        
        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var semesters = await _apiClient.GetAsync<List<SemesterDto>>("/api/semesters");

                if (semesters != null)
                {
                    Semesters = semesters;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data");
                ErrorMessage = "Không thể tải dữ liệu. Vui lòng thử lại sau.";
            }

            return Page();
        }

        // Semester handlers
        public async Task<IActionResult> OnPostCreateSemesterAsync([FromBody] CreateSemesterDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return new JsonResult(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                var result = await _apiClient.PostAsync<SemesterDto>("/api/semesters", model);
                
                if (result != null)
                {
                    return new JsonResult(new { success = true, message = "Thêm học kỳ thành công", data = result });
                }

                return new JsonResult(new { success = false, message = "Không thể thêm học kỳ" });
            }
            catch (HttpRequestException ex)
            {
                // Check if it's a warning message
                _logger.LogWarning(ex, "Warning or error creating semester");
                return new JsonResult(new { success = false, isWarning = ex.Message.Contains("⚠️"), message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating semester");
                return new JsonResult(new { success = false, message = "Đã xảy ra lỗi. Vui lòng thử lại." });
            }
        }

        public async Task<IActionResult> OnPostUpdateSemesterAsync([FromBody] UpdateSemesterDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return new JsonResult(new { success = false, message = "Dữ liệu không hợp lệ" });
                }

                var result = await _apiClient.PutAsync<SemesterDto>($"/api/semesters/{model.Id}", model);
                
                if (result != null)
                {
                    return new JsonResult(new { success = true, message = "Cập nhật học kỳ thành công", data = result });
                }

                return new JsonResult(new { success = false, message = "Không thể cập nhật học kỳ" });
            }
            catch (HttpRequestException ex)
            {
                // Check if it's a warning message
                _logger.LogWarning(ex, "Warning or error updating semester");
                return new JsonResult(new { success = false, isWarning = ex.Message.Contains("⚠️"), message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating semester");
                return new JsonResult(new { success = false, message = "Đã xảy ra lỗi. Vui lòng thử lại." });
            }
        }

        public async Task<IActionResult> OnPostDeleteSemesterAsync(int id)
        {
            try
            {
                var result = await _apiClient.DeleteAsync($"/api/semesters/{id}");
                
                if (result)
                {
                    return new JsonResult(new { success = true, message = "Xóa học kỳ thành công" });
                }

                return new JsonResult(new { success = false, message = "Không thể xóa học kỳ" });
            }
            catch (HttpRequestException ex)
            {
                // This exception contains the specific error message from the API
                _logger.LogError(ex, "Error deleting semester");
                return new JsonResult(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting semester");
                return new JsonResult(new { success = false, message = "Đã xảy ra lỗi. Vui lòng thử lại." });
            }
        }
    }
}

