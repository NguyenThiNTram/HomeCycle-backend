using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Dashboard;
using HomeCycle.Application.DTOs.Responses.Auths;
using HomeCycle.Application.DTOs.Responses.Dashboard;
using HomeCycle.Application.Interfaces.Repositories.Dashboard;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace HomeCycle.Infrastructure.Repositories.Dashboard;

public sealed class UserDashboardRepository(HomeCycleDbContext db) : IUserDashboardRepository
{
    private IQueryable<User> Scope(UserRole? role)
    {
        var query = db.Users.AsNoTracking();
        return role.HasValue ? query.Where(x => x.Role == (int)role.Value) : query;
    }

    public async Task<IReadOnlyList<UserAccountGroup>> GetAccountGroupsAsync(UserRole? role, CancellationToken ct)
        => await Scope(role)
            .GroupBy(x => new { x.Role, x.Status, x.IsEmailVerified })
            .Select(g => new UserAccountGroup((UserRole)g.Key.Role, (UserStatus)g.Key.Status, g.Key.IsEmailVerified, g.Count()))
            .ToListAsync(ct);

    public async Task<PagedResult<UserAdminResponse>> GetUsersAsync(DashboardUserListRequest request, CancellationToken ct)
    {
        var query = Scope(request.Role);
        if (request.Status.HasValue)
            query = query.Where(x => x.Status == (int)request.Status.Value);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim().ToLower();
            query = query.Where(x => x.Username.ToLower().Contains(keyword)
                || x.Email.ToLower().Contains(keyword)
                || (x.PhoneNumber != null && x.PhoneNumber.Contains(keyword)));
        }
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.UserId)
            .Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .Select(x => new UserAdminResponse
            {
                UserId = x.UserId, Username = x.Username, Email = x.Email,
                PhoneNumber = x.PhoneNumber, AvatarUrl = x.AvatarUrl,
                Role = (UserRole)x.Role, Status = (UserStatus)x.Status,
                IsEmailVerified = x.IsEmailVerified, CreatedAt = x.CreatedAt
            }).ToListAsync(ct);
        return new PagedResult<UserAdminResponse>
        {
            Items = items, TotalCount = count, PageNumber = request.PageNumber, PageSize = request.PageSize
        };
    }

    public async Task<IReadOnlyList<RegistrationDay>> GetRegistrationsAsync(UserRole? role, DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        // Group in PostgreSQL, not by loading individual user records. Vietnam is UTC+7.
        var rows = await Scope(role).Where(x => x.CreatedAt >= fromUtc && x.CreatedAt < toUtc)
            .GroupBy(x => x.CreatedAt.AddHours(7).Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .OrderBy(x => x.Date).ToListAsync(ct);
        return rows.Select(x => new RegistrationDay(DateOnly.FromDateTime(x.Date), x.Count)).ToArray();
    }
}
