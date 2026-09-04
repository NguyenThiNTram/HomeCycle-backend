using HomeCycle.Application.Commons.Paginations;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Interfaces.Repositories.Offers
{
    public interface IConversationRepository
    {
        Task<conversation?> GetByIdAsync(Guid conversationId, CancellationToken cancellationToken = default);

        Task<conversation?> GetByUsersAsync(Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken = default);

        // tránh tạo trùng
        Task<conversation> GetOrCreateAsync(Guid firstUserId, Guid secondUserId, DateTime activityAt, CancellationToken cancellationToken = default);

        Task<PagedResult<conversation>> GetMineAsync(Guid userId, PaginationRequest request, CancellationToken cancellationToken = default);

        Task<bool> IsParticipantAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken = default);

        // Chỉ cập nhật khi activityAt mới hơn giá trị hiện tại
        Task UpdateLastActivityAsync(Guid conversationId, DateTime activityAt, CancellationToken cancellationToken = default);
    }
}
