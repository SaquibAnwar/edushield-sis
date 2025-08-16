using AutoMapper;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using EduShield.Core.Interfaces;

namespace EduShield.Api.Services;

public class UserService : IUserService
{
    private readonly IUserRepo _userRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepo userRepo, IMapper mapper, ILogger<UserService> logger)
    {
        _userRepo = userRepo;
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

    public async Task<User> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new ArgumentException($"User with ID {userId} not found");
        }

        // Update only provided fields
        if (!string.IsNullOrEmpty(request.FirstName))
            user.FirstName = request.FirstName;
        
        if (!string.IsNullOrEmpty(request.LastName))
            user.LastName = request.LastName;
        
        if (request.Role.HasValue)
            user.Role = request.Role.Value;
        
        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;
        
        if (request.ProfilePictureUrl != null)
            user.ProfilePictureUrl = request.ProfilePictureUrl;

        return await _userRepo.UpdateAsync(user, cancellationToken);
    }

    public async Task<bool> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return false;
        }

        user.IsActive = false;
        await _userRepo.UpdateAsync(user, cancellationToken);
        return true;
    }

    public async Task<bool> AssignRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return false;
        }

        user.Role = role;
        await _userRepo.UpdateAsync(user, cancellationToken);
        return true;
    }

    public async Task<IEnumerable<User>> GetUsersAsync(UserRole? role = null, CancellationToken cancellationToken = default)
    {
        return await _userRepo.GetAllAsync(role, cancellationToken);
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
            await _userRepo.UpdateAsync(user, cancellationToken);
        }
    }
}