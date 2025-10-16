using BusinessObject.DTOs.GGLogin;
using BusinessObject.Enum;
using BusinessObject.Models;
using Google.Apis.Auth;
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

        public GoogleAuthService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<User> LoginWithGoogleAsync(string idToken, string deviceInfo)
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings());

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
                
                if (string.IsNullOrWhiteSpace(existingUser.AvatarUrl) && !string.IsNullOrEmpty(payload.Picture))
                {
                    existingUser.AvatarUrl = payload.Picture;
                }

                // ✅ ĐẢM BẢO USER CÓ ROLE (nếu user từ import có thể thiếu role)
                if (existingUser.UserRoles == null || !existingUser.UserRoles.Any())
                {
                    // Reload user với đầy đủ relationships
                    existingUser = await _userRepo.GetByIdAsync(existingUser.Id);
                    
                    // Nếu vẫn không có role, gán role Student mặc định
                    if (existingUser.UserRoles == null || !existingUser.UserRoles.Any())
                    {
                        existingUser.UserRoles.Add(new UserRole
                        {
                            UserId = existingUser.Id,
                            RoleId = 2, // Student
                            AssignedAt = DateTime.UtcNow
                        });
                    }
                }

                // ✅ ĐẢM BẢO TẤT CẢ USERROLES CÓ ROLE NAVIGATION PROPERTY
                if (existingUser.UserRoles != null)
                {
                    foreach (var userRole in existingUser.UserRoles)
                    {
                        if (userRole.Role == null)
                        {
                            // Reload user với đầy đủ relationships nếu Role navigation property bị null
                            existingUser = await _userRepo.GetByIdAsync(existingUser.Id);
                            break;
                        }
                    }
                }
            }

            existingUser.LastLoginAt = DateTime.UtcNow;
            await _userRepo.SaveChangesAsync();

            return existingUser;
        }
    }
}
