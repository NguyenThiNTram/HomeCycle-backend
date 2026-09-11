using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Requests.Wallets;
using HomeCycle.Application.DTOs.Responses.Wallets;
using HomeCycle.Application.Interfaces.Repositories.Wallets;
using HomeCycle.Domain.Entities;
using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.DbContexts;
using HomeCycle.Infrastructure.Persistences.Mappers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Repositories.Wallets
{
    public class WithdrawalRepository : IWithdrawalRepository
    {
        private readonly HomeCycleDbContext _db;
        public WithdrawalRepository(HomeCycleDbContext db) => _db = db;

        public async Task AddAsync(withdrawal withdrawal, CancellationToken ct = default)
            => await _db.Withdrawals.AddAsync(withdrawal.ToInfrastructure(), ct);

        public async Task<withdrawal?> GetByIdAsync(Guid withdrawalId, CancellationToken ct = default)
        {
            var entity = await _db.Withdrawals.FirstOrDefaultAsync(x => x.WithdrawalId == withdrawalId, ct);
            return entity?.ToDomain();
        }

        public Task UpdateAsync(withdrawal withdrawal, CancellationToken ct = default)
        {
            var entity = withdrawal.ToInfrastructure();
            var local = _db.Withdrawals.Local.FirstOrDefault(x => x.WithdrawalId == entity.WithdrawalId);
            if (local != null) _db.Entry(local).State = EntityState.Detached;
            _db.Withdrawals.Update(entity);
            return Task.CompletedTask;
        }

        public async Task<withdrawal?> GetByIdForUpdateAsync(
            Guid withdrawalId,
            CancellationToken ct = default)
        {
            if (_db.Database.CurrentTransaction == null)
            {
                throw new InvalidOperationException(
                    "FOR UPDATE requires an active database transaction.");
            }

            var entity = await _db.Withdrawals
                .FromSqlInterpolated($@"
            SELECT *
            FROM ""Withdrawal""
            WHERE ""WithdrawalId"" = {withdrawalId}
            FOR UPDATE")
                .AsNoTracking()
                .SingleOrDefaultAsync(ct);

            return entity?.ToDomain();
        }

        public async Task<PagedResult<WithdrawalReadModel>> GetPagedAsync(
            WithdrawalQuery request,
            CancellationToken ct = default)
        {
            var query = _db.Withdrawals.AsNoTracking().AsQueryable();

            if (request.UserId.HasValue)
                query = query.Where(x => x.Wallet.UserId == request.UserId.Value);

            if (request.Status.HasValue)
                query = query.Where(x => x.WithdrawalStatus == (int)request.Status.Value);

            if (request.FromDate.HasValue)
                query = query.Where(x => x.RequestedAt >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(x => x.RequestedAt <= request.ToDate.Value);

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                var keyword = request.Keyword.Trim();

                query = query.Where(x =>
                    EF.Functions.ILike(x.Wallet.User.Username, $"%{keyword}%") ||
                    EF.Functions.ILike(x.Wallet.User.Email, $"%{keyword}%") ||
                    (x.UserBank.AccountName != null &&
                     EF.Functions.ILike(x.UserBank.AccountName, $"%{keyword}%")) ||
                    (x.UserBank.AccountNumber != null &&
                     EF.Functions.ILike(x.UserBank.AccountNumber, $"%{keyword}%")));
            }

            var totalCount = await query.CountAsync(ct);

            query = request.PrioritizeActionable
                ? query
                    .OrderBy(x =>
                        x.WithdrawalStatus == (int)WithdrawalStatus.Pending ? 0 :
                        x.WithdrawalStatus == (int)WithdrawalStatus.Processing ? 1 : 2)
                    .ThenByDescending(x => x.RequestedAt)
                : query.OrderByDescending(x => x.RequestedAt);

            var items = await ProjectWithdrawalReadModels(query)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(ct);

            return new PagedResult<WithdrawalReadModel>
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }


        public Task<WithdrawalReadModel?> GetReadModelByIdAsync(
            Guid withdrawalId,
            CancellationToken ct = default)
        {
            var query = _db.Withdrawals
                .AsNoTracking()
                .Where(x => x.WithdrawalId == withdrawalId);

            return ProjectWithdrawalReadModels(query).FirstOrDefaultAsync(ct);
        }

        private static IQueryable<WithdrawalReadModel> ProjectWithdrawalReadModels(IQueryable<Withdrawal> query)
        {
            return query.Select(x => new WithdrawalReadModel
            {
                WithdrawalId = x.WithdrawalId,
                WalletId = x.WalletId,

                UserId = x.Wallet.UserId ?? Guid.Empty,
                Username = x.Wallet.User.Username,
                Email = x.Wallet.User.Email,
                AvatarUrl = x.Wallet.User.AvatarUrl,

                UserBankId = x.UserBankId,
                BankCode = x.UserBank.BankCode,
                BankName = x.UserBank.BankName,
                AccountNumber = x.UserBank.AccountNumber,
                AccountName = x.UserBank.AccountName,

                BankVerifyStatus = x.UserBank.VerifyStatus.HasValue
                    ? (VerifyStatus?)x.UserBank.VerifyStatus.Value
                    : null,

                Amount = x.Amount ?? 0m,

                Status = x.WithdrawalStatus.HasValue
                    ? (WithdrawalStatus?)x.WithdrawalStatus.Value
                    : null,

                RequestedAt = x.RequestedAt,
                ProcessedAt = x.ProcessedAt,

                ProcessedByUserId = x.ProcessedBy,
                ProcessedByUsername = x.ProcessedByUser != null
                    ? x.ProcessedByUser.Username
                    : null,

                RejectReason = x.RejectReason
            });
        }
    }
}
