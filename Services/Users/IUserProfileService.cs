using BusinessObject.DTOs.User;

namespace Services.Users;

public interface IUserProfileService
{
    Task<ProfileDto?> GetMyProfileAsync(int userId);
    Task<ProfileDto> UpdateMyProfileAsync(int userId, UpdateProfileRequest request);
}


