using BusinessObject.DTOs.GGLogin;
using BusinessObject.Enum;
using BusinessObject.Models;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.GGLogin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Users
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly ILogger<GoogleAuthService> _logger;

        public GoogleAuthService(IUserRepository userRepo, ILogger<GoogleAuthService> logger)
        {
            _userRepo = userRepo;
            _logger = logger;
        }

        public async Task<User> LoginWithGoogleAsync(string idToken, string deviceInfo)
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings());

            _logger.LogInformation("[Google Auth] Payload - Email: {Email}, Name: {Name}, Picture: {Picture}", 
                payload.Email, payload.Name, payload.Picture);

            // Tìm user theo GoogleSubject trước, nếu không có thì tìm theo Email
            var existingUser = await _userRepo.FindByGoogleSubAsync(payload.Subject)
                ?? await _userRepo.FindByEmailAsync(payload.Email);

            if (existingUser == null)
            {
                throw new UnauthorizedAccessException("Your email is not registered in the system. Please contact the administrator for support.");
            }
            else
            {
                // User đã tồn tại (có thể từ import hoặc Google login trước đó)
                
                _logger.LogInformation("[Google Auth] Existing user found - Current AvatarUrl: {AvatarUrl}", existingUser.AvatarUrl);
                
                // ✅ CẬP NHẬT GOOGLESUBJECT nếu thiếu hoặc sai
                if (string.IsNullOrWhiteSpace(existingUser.GoogleSubject) || 
                    existingUser.GoogleSubject != payload.Subject)
                {
                    existingUser.GoogleSubject = payload.Subject;
                }
                
                // ✅ CẬP NHẬT THÔNG TIN KHÁC nếu thiếu hoặc cần cập nhật
                if (string.IsNullOrWhiteSpace(existingUser.FullName) && !string.IsNullOrEmpty(payload.Name))
                {
                    existingUser.FullName = payload.Name;
                }
                
                // ✅ CẬP NHẬT AVATAR từ Google CHỈ KHI:
                // - User chưa có avatar (null/empty)
                // - Hoặc avatar hiện tại cũng là từ Google (để cập nhật avatar Google mới nhất)
                // => Không ghi đè avatar tùy chỉnh của user
                if (!string.IsNullOrEmpty(payload.Picture))
                {
                    bool isCurrentAvatarFromGoogle = !string.IsNullOrEmpty(existingUser.AvatarUrl) && 
                                                     existingUser.AvatarUrl.Contains("googleusercontent.com");
                    bool hasNoAvatar = string.IsNullOrEmpty(existingUser.AvatarUrl);
                    
                    if (hasNoAvatar || isCurrentAvatarFromGoogle)
                    {
                        existingUser.AvatarUrl = payload.Picture;
                        _logger.LogInformation("[Google Auth] Updated AvatarUrl to: {AvatarUrl}", payload.Picture);
                    }
                    else
                    {
                        _logger.LogInformation("[Google Auth] Keeping custom avatar: {AvatarUrl}", existingUser.AvatarUrl);
                    }
                }
                else
                {
                    _logger.LogWarning("[Google Auth] No picture in Google payload!");
                }

                // ✅ ĐẢM BẢO USER CÓ ROLE (nếu user từ import có thể thiếu role)
                if (existingUser.RoleId == 0)
                {
                    // Reload user với đầy đủ relationships
                    existingUser = await _userRepo.GetByIdAsync(existingUser.Id);
                    
                    // Nếu vẫn không có role, gán role Student mặc định (RoleId = 2)
                    if (existingUser.RoleId == 0)
                    {
                        existingUser.RoleId = 2; // Student
                    }
                }

                // ✅ ĐẢM BẢO ROLE NAVIGATION PROPERTY ĐƯỢC LOAD
                if (existingUser.Role == null)
                {
                    // Reload user với đầy đủ relationships nếu Role navigation property bị null
                    existingUser = await _userRepo.GetByIdAsync(existingUser.Id);
                }
            }

            existingUser.LastLoginAt = DateTime.UtcNow;
            await _userRepo.SaveChangesAsync();

            return existingUser;
        }
    }
}
