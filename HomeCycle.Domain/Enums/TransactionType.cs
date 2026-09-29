using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Domain.Enums
{
    public enum TransactionType
    {
        // 1. Nhóm thanh toán đơn hàng (Order)
        Escrow_Deposit = 1,     // PayOS / external -> Order_Escrow Available
        Wallet_Payment = 2,     // Buyer Available -> Order_Escrow Available

        // 2. Nhóm biến động sau giao dịch (Post-Order)
        Payout_Release = 3,      // Order_Escrow Available -> Seller Available
        Order_Refund = 4,       // Order_Escrow Available -> Buyer Available

        // 3. Nhóm Rút tiền (Withdrawal)
        Withdrawal_Lock = 5,    // Khóa tiền rút: Từ Available sang Hold của chính user
        Withdrawal_Success = 6, // Rút thành công: Trừ hẳn tiền khỏi Hold của user
        Withdrawal_Revert = 7,  // Rút thất bại: Trả lại từ Hold về Available

        // 4. Nhóm Doanh thu hệ thống (System Revenue)
        Commission_Fee = 8,     // Sàn thu phí hoa hồng đơn hàng (Chảy vào System Wallet)
        Subscription_Fee = 9,    // Sàn thu phí gói dịch vụ (Chảy vào System Wallet)

        Shipping_Fee_Collected = 10
    }
}
