using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Application.DTOs.Responses.Orders;
using HomeCycle.Application.DTOs.Responses.Wallets;

namespace HomeCycle.Application.DTOs.Responses.Dashboard;

public sealed class BusinessSelfDashboardResponse
{
    public PagedResult<OrderListItemDto> BuyerOrders { get; init; } = new();
    public PagedResult<OrderListItemDto> SellerOrders { get; init; } = new();
    public WalletInfoDto Wallet { get; init; } = new();
    public PagedResult<WalletLedgerResponseDto> Ledger { get; init; } = new();
    public PagedResult<WithdrawalListItemDto> Withdrawals { get; init; } = new();
}
