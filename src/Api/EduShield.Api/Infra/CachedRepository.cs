using EduShield.Core.Interfaces;
using EduShield.Core.Entities;
using EduShield.Core.Dtos;

namespace EduShield.Api.Infra;

/// <summary>
/// Generic cached repository wrapper that automatically caches database operations
/// </summary>
public class CachedRepository<TEntity, TDto> : ICachedRepository<TEntity, TDto>
    where TEntity : class
    where TDto : class
{
    private readonly IBaseRepository<TEntity> _repository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedRepository<TEntity, TDto>> _logger;
    private readonly string _entityName;

    public CachedRepository(
        IBaseRepository<TEntity> repository,
        ICacheService cacheService,
        ILogger<CachedRepository<TEntity, TDto>> logger)
    {
        _repository = repository;
        _cacheService = cacheService;
        _logger = logger;
        _entityName = typeof(TEntity).Name;
    }

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(id);
        
        var cached = await _cacheService.GetAsync<TEntity>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for {EntityName} with ID: {Id}", _entityName, id);
            return cached;
        }

        var result = await _repository.GetByIdAsync(id, cancellationToken);
        if (result != null)
        {
            await _cacheService.SetAsync(cacheKey, result, CacheKeys.TTL.Medium, cancellationToken);
            _logger.LogDebug("Cached {EntityName} with ID: {Id}", _entityName, id);
        }
        
        return result;
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = GetListCacheKey();
        
        var cached = await _cacheService.GetAsync<IEnumerable<TEntity>>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for {EntityName} list", _entityName);
            return cached;
        }

        var result = await _repository.GetAllAsync(cancellationToken);
        await _cacheService.SetAsync(cacheKey, result, CacheKeys.TTL.Short, cancellationToken);
        _logger.LogDebug("Cached {EntityName} list", _entityName);
        
        return result;
    }

    public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var result = await _repository.CreateAsync(entity, cancellationToken);
        
        // Invalidate list cache when new entity is created
        await InvalidateListCache();
        
        return result;
    }

    public async Task<bool> UpdateAsync(Guid id, TEntity entity, CancellationToken cancellationToken = default)
    {
        var result = await _repository.UpdateAsync(id, entity, cancellationToken);
        
        if (result)
        {
            // Invalidate specific entity cache and list cache
            await InvalidateEntityCache(id);
            await InvalidateListCache();
        }
        
        return result;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _repository.DeleteAsync(id, cancellationToken);
        
        if (result)
        {
            // Invalidate specific entity cache and list cache
            await InvalidateEntityCache(id);
            await InvalidateListCache();
        }
        
        return result;
    }

    // Additional cached methods for common queries - these will need to be implemented
    // based on the specific repository capabilities
    public async Task<TDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // This method needs to be implemented by specific cached repositories
        // as not all entities have email fields
        throw new NotImplementedException($"GetByEmailAsync is not implemented for {_entityName}");
    }

    public async Task<IEnumerable<TDto>> GetByRoleAsync(int role, CancellationToken cancellationToken = default)
    {
        // This method needs to be implemented by specific cached repositories
        // as not all entities have role fields
        throw new NotImplementedException($"GetByRoleAsync is not implemented for {_entityName}");
    }

    public async Task<IEnumerable<TDto>> GetByDepartmentAsync(string department, CancellationToken cancellationToken = default)
    {
        // This method needs to be implemented by specific cached repositories
        // as not all entities have department fields
        throw new NotImplementedException($"GetByDepartmentAsync is not implemented for {_entityName}");
    }

    public async Task<IEnumerable<TDto>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        // This method needs to be implemented by specific cached repositories
        // as not all entities have student relationships
        throw new NotImplementedException($"GetByStudentAsync is not implemented for {_entityName}");
    }

    public async Task<IEnumerable<TDto>> GetByFacultyAsync(Guid facultyId, CancellationToken cancellationToken = default)
    {
        // This method needs to be implemented by specific cached repositories
        // as not all entities have faculty relationships
        throw new NotImplementedException($"GetByFacultyAsync is not implemented for {_entityName}");
    }

    public async Task<IEnumerable<TDto>> GetByTypeAsync(int type, CancellationToken cancellationToken = default)
    {
        // This method needs to be implemented by specific cached repositories
        // as not all entities have type fields
        throw new NotImplementedException($"GetByTypeAsync is not implemented for {_entityName}");
    }

    public async Task<IEnumerable<TDto>> GetByStatusAsync(int status, CancellationToken cancellationToken = default)
    {
        // This method needs to be implemented by specific cached repositories
        // as not all entities have status fields
        throw new NotImplementedException($"GetByStatusAsync is not implemented for {_entityName}");
    }

    // Cache key generation methods
    private string GetCacheKey(Guid id) => $"{_entityName.ToLower()}:{id}";
    private string GetListCacheKey() => $"{_entityName.ToLower()}s:list";
    private string GetEmailCacheKey(string email) => $"{_entityName.ToLower()}:email:{email}";
    private string GetRoleCacheKey(int role) => $"{_entityName.ToLower()}s:role:{role}";
    private string GetDepartmentCacheKey(string department) => $"{_entityName.ToLower()}s:dept:{department}";
    private string GetStudentCacheKey(Guid studentId) => $"{_entityName.ToLower()}s:student:{studentId}";
    private string GetFacultyCacheKey(Guid facultyId) => $"{_entityName.ToLower()}s:faculty:{facultyId}";
    private string GetTypeCacheKey(int type) => $"{_entityName.ToLower()}s:type:{type}";
    private string GetStatusCacheKey(int status) => $"{_entityName.ToLower()}s:status:{status}";

    // Cache invalidation methods
    private async Task InvalidateEntityCache(Guid id)
    {
        await _cacheService.RemoveAsync(GetCacheKey(id));
        _logger.LogDebug("Invalidated entity cache for {EntityName} with ID: {Id}", _entityName, id);
    }

    private async Task InvalidateListCache()
    {
        await _cacheService.RemoveAsync(GetListCacheKey());
        _logger.LogDebug("Invalidated list cache for {EntityName}", _entityName);
    }

    private async Task InvalidateEmailCache(string email)
    {
        await _cacheService.RemoveAsync(GetEmailCacheKey(email));
        _logger.LogDebug("Invalidated email cache for {EntityName} with email: {Email}", _entityName, email);
    }

    private async Task InvalidateRoleCache(int role)
    {
        await _cacheService.RemoveAsync(GetRoleCacheKey(role));
        _logger.LogDebug("Invalidated role cache for {EntityName} with role: {Role}", _entityName, role);
    }

    private async Task InvalidateDepartmentCache(string department)
    {
        await _cacheService.RemoveAsync(GetDepartmentCacheKey(department));
        _logger.LogDebug("Invalidated department cache for {EntityName} with department: {Department}", _entityName, department);
    }

    private async Task InvalidateStudentCache(Guid studentId)
    {
        await _cacheService.RemoveAsync(GetStudentCacheKey(studentId));
        _logger.LogDebug("Invalidated student cache for {EntityName} with student ID: {StudentId}", _entityName, studentId);
    }

    private async Task InvalidateFacultyCache(Guid facultyId)
    {
        await _cacheService.RemoveAsync(GetFacultyCacheKey(facultyId));
        _logger.LogDebug("Invalidated faculty cache for {EntityName} with faculty ID: {FacultyId}", _entityName, facultyId);
    }

    private async Task InvalidateTypeCache(int type)
    {
        await _cacheService.RemoveAsync(GetTypeCacheKey(type));
        _logger.LogDebug("Invalidated type cache for {EntityName} with type: {Type}", _entityName, type);
    }

    private async Task InvalidateStatusCache(int status)
    {
        await _cacheService.RemoveAsync(GetStatusCacheKey(status));
        _logger.LogDebug("Invalidated status cache for {EntityName} with status: {Status}", _entityName, status);
    }
}

/// <summary>
/// Interface for cached repository operations
/// </summary>
public interface ICachedRepository<TEntity, TDto> : IBaseRepository<TEntity>
    where TEntity : class
    where TDto : class
{
    // Additional cached methods for common queries
    Task<TDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<TDto>> GetByRoleAsync(int role, CancellationToken cancellationToken = default);
    Task<IEnumerable<TDto>> GetByDepartmentAsync(string department, CancellationToken cancellationToken = default);
    Task<IEnumerable<TDto>> GetByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TDto>> GetByFacultyAsync(Guid facultyId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TDto>> GetByTypeAsync(int type, CancellationToken cancellationToken = default);
    Task<IEnumerable<TDto>> GetByStatusAsync(int status, CancellationToken cancellationToken = default);
}
