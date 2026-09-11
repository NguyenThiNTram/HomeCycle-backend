using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.Commons.Results;
using HomeCycle.Application.DTOs.Requests.Wallets;
using HomeCycle.Application.DTOs.Responses.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Wallets
{
    public interface IWithdrawalService
    {
        Task<Result<Guid>> CreateWithdrawalRequestAsync(
            Guid userId, CreateWithdrawalRequest request, CancellationToken ct = default);

        Task<Result<bool>> ApproveWithdrawalAsync(
            Guid moderatorId, Guid withdrawalId, CancellationToken ct = default);

        Task<Result<bool>> RejectWithdrawalAsync(
            Guid moderatorId, Guid withdrawalId, RejectWithdrawalRequest request, CancellationToken ct = default);
        Task<Result<bool>> SyncWithdrawalStatusAsync(Guid withdrawalId, CancellationToken ct = default);
        Task<Result<PagedResult<WithdrawalListItemDto>>> GetMyWithdrawalsAsync(
            Guid userId,
            WithdrawalSearchRequest request,
            CancellationToken ct = default);

        Task<Result<WithdrawalDetailDto>> GetMyWithdrawalDetailAsync(
            Guid userId,
            Guid withdrawalId,
            CancellationToken ct = default);

        Task<Result<PagedResult<ModeratorWithdrawalListItemDto>>> GetAllForModeratorAsync(
            ModeratorWithdrawalSearchRequest request,
            CancellationToken ct = default);

        Task<Result<ModeratorWithdrawalDetailDto>> GetDetailForModeratorAsync(
            Guid withdrawalId,
            CancellationToken ct = default);
    }

}
