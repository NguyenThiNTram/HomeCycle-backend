using System.Security.Claims;
using HomeCycle.Application.DTOs.Requests.Orders;
using HomeCycle.Application.DTOs.Requests.Wallets;
using HomeCycle.Application.DTOs.Responses.Dashboard;
using HomeCycle.Application.Interfaces.Services.Orders;
using HomeCycle.Application.Interfaces.Services.Wallets;
using HomeCycle.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HomeCycle.API.Controllers;

[ApiController]
[Route("api/business/dashboard")]
[Authorize(Roles = nameof(UserRole.Business))]
public sealed class BusinessDashboardController(
    IOrderService orderService,
    IWalletService walletService,
    IWithdrawalService withdrawalService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation(
        Summary = "Dashboard đơn hàng và ví của doanh nghiệp đang đăng nhập",
        Description = "Trả đơn mua, đơn bán, số dư Available/Hold, sao kê và lịch sử rút tiền. " +
            "Không cần query để lấy trang đầu. Các bộ lọc dùng tiền tố buyerOrders, sellerOrders, ledger, withdrawals; " +
            "ví dụ buyerOrders.PageNumber=1&sellerOrders.PageSize=5&ledger.FromDate=2026-09-01T00:00:00Z. " +
            "Mỗi danh sách phân trang độc lập, mặc định 10 và tối đa 100 bản ghi/trang. " +
            "FromDate/ToDate áp dụng riêng cho ledger và withdrawals; nên truyền thời gian kèm múi giờ. " +
            "Số dư ví là số dư hiện tại, không bị giới hạn bởi bộ lọc thời gian. " +
            "Sao kê không phải báo cáo doanh thu hoặc thu nhập thuần.")]
    [ProducesResponseType(typeof(BusinessSelfDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BusinessSelfDashboardResponse>> GetDashboard(
        [FromQuery(Name = "buyerOrders")] OrderSearchRequest buyerOrders,
        [FromQuery(Name = "sellerOrders")] OrderSearchRequest sellerOrders,
        [FromQuery(Name = "ledger")] WalletLedgerSearchRequest ledger,
        [FromQuery(Name = "withdrawals")] WithdrawalSearchRequest withdrawals,
        CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            return Unauthorized();

        if (ledger.FromDate.HasValue && ledger.ToDate.HasValue && ledger.FromDate > ledger.ToDate)
            ModelState.AddModelError("ledger.ToDate", "ToDate phải lớn hơn hoặc bằng FromDate.");

        if (withdrawals.FromDate.HasValue && withdrawals.ToDate.HasValue && withdrawals.FromDate > withdrawals.ToDate)
            ModelState.AddModelError("withdrawals.ToDate", "ToDate phải lớn hơn hoặc bằng FromDate.");

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var wallet = await walletService.GetMyWalletAsync(userId, WalletTypeEnum.Business, ct);
        if (!wallet.IsSuccess)
            return NotFound(wallet.Error);

        var purchases = await orderService.GetMyOrdersAsync(userId, false, buyerOrders, ct);
        if (!purchases.IsSuccess)
            return BadRequest(purchases.Error);

        var sales = await orderService.GetMyOrdersAsync(userId, true, sellerOrders, ct);
        if (!sales.IsSuccess)
            return BadRequest(sales.Error);

        var statement = await walletService.GetWalletStatementAsync(userId, WalletTypeEnum.Business, ledger, ct);
        if (!statement.IsSuccess)
            return NotFound(statement.Error);

        var withdrawalHistory = await withdrawalService.GetMyWithdrawalsAsync(userId, withdrawals, ct);
        if (!withdrawalHistory.IsSuccess)
            return BadRequest(withdrawalHistory.Error);

        return Ok(new BusinessSelfDashboardResponse
        {
            BuyerOrders = purchases.Data!,
            SellerOrders = sales.Data!,
            Wallet = wallet.Data!,
            Ledger = statement.Data!,
            Withdrawals = withdrawalHistory.Data!
        });
    }
}
