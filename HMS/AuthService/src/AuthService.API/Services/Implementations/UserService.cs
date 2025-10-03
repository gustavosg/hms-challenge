using System.Linq.Expressions;

using AuthService.API.Data;
using AuthService.API.Models;
using AuthService.API.Services.Interfaces;

using Microsoft.EntityFrameworkCore;

using Shared.DTOs;
using Shared.Infra.UnitOfWork;
using Shared.Mapper;
using Shared.Services.Cache;
using Shared.Utils;

namespace AuthService.API.Services.Implementations;

public class UserService(
    IAddMapper<Shared.DTOs.Users.Add.Request, Models.User> addMapper,
    IEditMapper<Shared.DTOs.Users.Edit.Request, Models.User> editMapper,
    IGetMapper<Models.User, Shared.DTOs.Users.Get.Response> getMapper,
    IUnitOfWork<ApplicationDbContext> unitOfWork,
    ICacheService cache)
    : IUserService
{
    private const string USER_CACHE_KEY = "user:{0}";
    private const string USERS_LIST_CACHE_KEY = "users:all";
    private const string USERS_PAGE_CACHE_KEY = "users:page:{0}:{1}";
    private const string USERS_FILTERED_CACHE_KEY = "users:filtered:{0}:page:{1}:{2}";

    public async Task<Guid> AddAsync(Shared.DTOs.Users.Add.Request request)
    {
        User user = addMapper.ToEntity(request);

        user.PasswordHash = PasswordHelper.Hash(request.Password);

        await unitOfWork.Context.Users.AddAsync(user);
        await unitOfWork.CommitAsync();

        // Invalidar cache de listas
        InvalidateListCaches();

        return user.Id;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        int rows = await unitOfWork.Context.Users.Where(_ => _.Id == id)
            .ExecuteUpdateAsync(_ =>
                _.SetProperty(user => user.IsDeleted, true));

        if (rows > 0)
        {
            // Invalidar cache do usuário específico e listas
            InvalidateUserCache(id);
            InvalidateListCaches();
        }

        return rows > 0;
    }

    public async Task<Shared.DTOs.Users.Get.Response> EditAsync(Shared.DTOs.Users.Edit.Request request)
    {
        User user = await unitOfWork.Context.Users.AsNoTracking().SingleAsync(x => x.Id == request.Id)
            ?? throw new InvalidOperationException($"User with ID {request.Id} not found");

        user = editMapper.ToEntity(request, user);

        unitOfWork.Context.Users.Update(user);
        await unitOfWork.CommitAsync();
        
        var response = getMapper.ToDTO(user);

        // Atualizar cache do usuário
        var userCacheKey = string.Format(USER_CACHE_KEY, user.Id);
        cache.Set(userCacheKey, response, TimeSpan.FromMinutes(15));

        // Invalidar cache de listas
        InvalidateListCaches();

        return response;
    }

    public async Task<Shared.DTOs.Users.Get.Response> GetAsync(Guid id)
    {
        var cacheKey = string.Format(USER_CACHE_KEY, id);

        return await cache.GetOrSetAsync(cacheKey, async () =>
        {
            var user = await unitOfWork.Context.Users
                .AsNoTracking()
                .SingleAsync(x => x.Id == id && !x.IsDeleted);

            return getMapper.ToDTO(user);
        }, TimeSpan.FromMinutes(15));
    }

    public async Task<PaginationResponse<Shared.DTOs.Users.Get.Response>> GetAsync(
        Shared.DTOs.Users.Get.Request request, 
        int page, 
        int pageSize)
    {
        string json = request is null ? "{}" : System.Text.Json.JsonSerializer.Serialize(request);

        var cacheKey = string.Format(USERS_FILTERED_CACHE_KEY, json, page, pageSize);

        return await cache.GetOrSetAsync(cacheKey, async () =>
        {
            IQueryable<Models.User> query = unitOfWork.Context.Users
                .AsNoTracking()
                .Where(_ => !_.IsDeleted);

            // Aplicar filtros baseados no request
            query = request switch
            {
                null => query,
                _ => query
                    .Where(u => string.IsNullOrWhiteSpace(request.Username) || u.Username.Contains(request.Username))
                    .Where(u => string.IsNullOrWhiteSpace(request.Email) || u.Email.Contains(request.Email))
                    .Where(u => string.IsNullOrWhiteSpace(request.PhoneNumber) || u.PhoneNumber.Contains(request.PhoneNumber))
            };

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            IEnumerable<Shared.DTOs.Users.Get.Response> items = await query
                .OrderBy(u => u.Username)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(user => getMapper.ToDTO(user))
                .ToListAsync();

            PaginationResponse pagination = new(page, pageSize, totalItems, totalPages);

            return new PaginationResponse<Shared.DTOs.Users.Get.Response>(items, pagination);
        }, TimeSpan.FromMinutes(5));
    }

    public async Task<bool> ExistsAsync(Expression<Func<Models.User, bool>> expression)
        => await unitOfWork.Context.Users.AnyAsync(expression);

    private void InvalidateUserCache(Guid userId)
    {
        var userCacheKey = string.Format(USER_CACHE_KEY, userId);
        cache.Remove(userCacheKey);
    }

    private void InvalidateListCaches()
    {
        cache.Remove(USERS_LIST_CACHE_KEY);

        for (int page = 1; page <= 10; page++)
        {
            for (int pageSize = 10; pageSize <= 50; pageSize += 10)
            {
                var pageKey = string.Format(USERS_PAGE_CACHE_KEY, page, pageSize);
                cache.Remove(pageKey);
            }
        }
    }
}
