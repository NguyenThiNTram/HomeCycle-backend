using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Services.Auths
{
    public interface IEmailService
    {
        Task SendModeratorConfirmationEmailAsync(string toEmail, string username, string activationUrl, DateTime expiresAt, CancellationToken cancellationToken = default);
        Task SendOtpEmailAsync(string toEmail, string otpCode, bool passwordReset = false);

        Task SendBusinessApprovalEmailAsync(string toEmail, string businessName);

        Task SendBusinessRejectionEmailAsync(string toEmail, string businessName, IEnumerable<string> rejectionReasons);

        Task SendNewDisputeEmailAsync(string toEmail, string recipientName, string disputeId, string disputeType, string categoryName, string createdAt, string reporterName, string otherPartyName, string orderCode, string productSummary, string appointmentSummary, string description, int evidenceCount, CancellationToken cancellationToken = default);

        Task SendPaymentReceiptEmailAsync(string toEmail, string payerName, string paymentId, string providerTransactionId, string orderCode, string productSummary, string itemAmount, string shippingFee, string paidAmount, string paymentMethod, string appointmentSummary, CancellationToken cancellationToken = default);

        Task SendPaymentFailureEmailAsync(string toEmail, string payerName, string paymentId, string providerTransactionId, string agreementId, string attemptAt, string amount, string productSummary, string shippingFee, string paymentStatus, CancellationToken cancellationToken = default);
    }
}
