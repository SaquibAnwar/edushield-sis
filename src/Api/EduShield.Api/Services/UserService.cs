using AutoMapper;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using EduShield.Core.Interfaces;

namespace EduShield.Api.Services;

public class UserService : IUserService
{
    private readonly IUserRepo _userRepo;
    private readonly ISessionService _sessionService;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepo userRepo, ISessionService sessionService, IMapper mapper, ILogger<UserService> logger)
    {
        _userRepo = userRepo;
        _sessionService = sessionService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _userRepo.GetByIdAsync(userId, cancellationToken);
    }

    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _userRepo.GetByEmailAsync(email, cancellationToken);
    }

    public async Task<User?> GetUserByExternalIdAsync(string externalId, AuthProvider provider, CancellationToken cancellationToken = default)
    {
        return await _userRepo.GetByExternalIdAsync(externalId, provider, cancellationToken);
    }

    public async Task<User> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        // Check if user already exists
        var existingUser = await _userRepo.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            throw new InvalidOperationException($"User with email {request.Email} already exists");
        }

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            ExternalId = request.ExternalId,
            Provider = request.Provider,
            Role = request.Role,
            IsActive = true,
            ProfilePictureUrl = request.ProfilePictureUrl
        };

        return await _userRepo.CreateAsync(user, cancellationToken);
    }

    public async Task<UserProfileDto?> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        // Update only provided fields
        if (!string.IsNullOrEmpty(request.FirstName))
            user.FirstName = request.FirstName;
        
        if (!string.IsNullOrEmpty(request.LastName))
            user.LastName = request.LastName;
        
        if (!string.IsNullOrEmpty(request.PhoneNumber))
            user.PhoneNumber = request.PhoneNumber;
        
        if (request.Role.HasValue)
            user.Role = request.Role.Value;
        
        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;
        
        if (request.ProfilePictureUrl != null)
            user.ProfilePictureUrl = request.ProfilePictureUrl;

        var success = await _userRepo.UpdateAsync(user.UserId, user, cancellationToken);
        if (!success)
            throw new InvalidOperationException("Failed to update user");
            
        return await GetUserProfileAsync(user.UserId, cancellationToken);
    }

    public async Task<bool> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return false;
        }

        user.IsActive = false;
        return await _userRepo.UpdateAsync(userId, user, cancellationToken);
    }

    public async Task<bool> ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return false;
        }

        user.IsActive = true;
        return await _userRepo.UpdateAsync(userId, user, cancellationToken);
    }

    public async Task<bool> AssignRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return false;
        }

        user.Role = role;
        return await _userRepo.UpdateAsync(userId, user, cancellationToken);
    }

    public async Task<bool> UpdateUserRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default)
    {
        return await AssignRoleAsync(userId, role, cancellationToken);
    }

    public async Task<IEnumerable<UserProfileDto>> GetUsersAsync(int page = 1, int pageSize = 20, UserRole? role = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var users = await _userRepo.GetAllAsync(page, pageSize, role, isActive, cancellationToken);
        var profiles = new List<UserProfileDto>();

        foreach (var user in users)
        {
            var profile = _mapper.Map<UserProfileDto>(user);
            
            // Get role-specific information
            if (user.Role == UserRole.Student)
            {
                var student = await _userRepo.GetStudentByUserIdAsync(user.UserId, cancellationToken);
                profile.StudentId = student?.Id;
            }
            else if (user.Role == UserRole.Teacher)
            {
                var faculty = await _userRepo.GetFacultyByUserIdAsync(user.UserId, cancellationToken);
                profile.FacultyId = faculty?.FacultyId;
            }

            profiles.Add(profile);
        }

        return profiles;
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        var profile = _mapper.Map<UserProfileDto>(user);
        
        // Get role-specific information
        if (user.Role == UserRole.Student)
        {
            var student = await _userRepo.GetStudentByUserIdAsync(userId, cancellationToken);
            profile.StudentId = student?.Id;
        }
        else if (user.Role == UserRole.Teacher)
        {
            var faculty = await _userRepo.GetFacultyByUserIdAsync(userId, cancellationToken);
            profile.FacultyId = faculty?.FacultyId;
        }

        return profile;
    }

    public async Task UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userRepo.UpdateAsync(userId, user, cancellationToken);
        }
    }

    public async Task<IEnumerable<UserSessionDto>> GetUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _sessionService.GetUserSessionsAsync(userId);
    }

    public async Task<bool> InvalidateSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await _sessionService.InvalidateSessionAsync(sessionId);
    }

    public async Task InvalidateOtherUserSessionsAsync(Guid userId, string currentSessionId, CancellationToken cancellationToken = default)
    {
        var sessions = await _sessionService.GetUserSessionsAsync(userId);
        var otherSessions = sessions.Where(s => s.SessionId != currentSessionId);

        foreach (var session in otherSessions)
        {
            await _sessionService.InvalidateSessionAsync(session.SessionId);
        }
    }

    public async Task<User> CreateFromExternalAsync(ExternalUserInfo userInfo, CancellationToken cancellationToken = default)
    {
        var createRequest = new CreateUserRequest
        {
            Email = userInfo.Email,
            FirstName = userInfo.FirstName,
            LastName = userInfo.LastName,
            ExternalId = userInfo.ExternalId,
            Provider = userInfo.Provider,
            Role = UserRole.Student,
            ProfilePictureUrl = userInfo.ProfilePictureUrl
        };

        return await CreateUserAsync(createRequest, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await GetUserByEmailAsync(email, cancellationToken);
    }

    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await GetUserByIdAsync(userId, cancellationToken);
    }

    public async Task<User> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        return await CreateUserAsync(request, cancellationToken);
    }

    public async Task<UserProfileDto?> UpdateAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        return await UpdateUserAsync(userId, request, cancellationToken);
    }

    public async Task<bool> DeactivateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DeactivateUserAsync(userId, cancellationToken);
    }

    public async Task<IEnumerable<UserProfileDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await GetUsersAsync(1, 1000, null, null, cancellationToken);
    }

    public async Task<IEnumerable<UserProfileDto>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        return await GetUsersAsync(1, 1000, role, null, cancellationToken);
    }
}