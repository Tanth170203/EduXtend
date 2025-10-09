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

            if (!payload.Email.EndsWith("@fpt.edu.vn", StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Chỉ chấp nhận tài khoản @fpt.edu.vn");

            var existingUser = await _userRepo.FindByGoogleSubAsync(payload.Subject)
                ?? await _userRepo.FindByEmailAsync(payload.Email);

            if (existingUser == null)
            {
                existingUser = new User
                {
                    FullName = payload.Name ?? "No Name",
                    Email = payload.Email,
                    GoogleSubject = payload.Subject,
                    AvatarUrl = payload.Picture,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                };

                await _userRepo.AddAsync(existingUser);

                // Gán role mặc định: Student
                existingUser.UserRoles.Add(new UserRole
                {
                    UserId = existingUser.Id,
                    RoleId = 2, // Student
                    AssignedAt = DateTime.UtcNow
                });

                await _userRepo.SaveChangesAsync();
            }

            existingUser.LastLoginAt = DateTime.UtcNow;
            await _userRepo.SaveChangesAsync();

            return existingUser;
        }
    }
}
