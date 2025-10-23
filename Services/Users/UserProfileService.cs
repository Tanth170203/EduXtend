using BusinessObject.DTOs.User;
using Repositories.Users;

namespace Services.Users;

public class UserProfileService : IUserProfileService
{
    private readonly IUserRepository _userRepo;

    public UserProfileService(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<ProfileDto?> GetMyProfileAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return null;
        return new ProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            PhoneNumber = user.PhoneNumber
        };
    }

    public async Task<ProfileDto> UpdateMyProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId) ?? throw new KeyNotFoundException("User not found");

        user.FullName = request.FullName;
        user.AvatarUrl = request.AvatarUrl;
        user.PhoneNumber = request.PhoneNumber;

        await _userRepo.UpdateAsync(user);

        return new ProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            PhoneNumber = user.PhoneNumber
        };
    }
}


