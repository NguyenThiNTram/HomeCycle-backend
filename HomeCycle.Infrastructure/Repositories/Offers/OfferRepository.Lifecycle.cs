using HomeCycle.Domain.Enums;
using HomeCycle.Infrastructure.Persistences.Mappers;
using HomeCycle.Application.DTOs.Responses.Offers;
using Microsoft.EntityFrameworkCore;
namespace HomeCycle.Infrastructure.Repositories.Offers;
public partial class OfferRepository
{
    public async Task ClosePendingByPostAsync(Guid postId, OfferStatus status, CancellationToken cancellationToken = default)
    {
        // Caller locks the Post before touching offers, so acceptance cannot race closure.
        var pending = await _db.Offers.Where(o => (o.PostId == postId || o.BuyPostId == postId) && o.OfferStatus == (int)OfferStatus.Pending)
            .Include(o => o.Sender).Include(o => o.Receiver).Include(o => o.Post).ToListAsync(cancellationToken);
        foreach (var row in pending)
        {
            row.OfferStatus = (int)status; row.Version = (row.Version ?? 1) + 1;
            var response = _mapper.Map<OfferResponse>(row.ToDomain());
            _unit.RegisterAfterCommit(async () => {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _publisher.PublishOfferUpdatedAsync(new[] { row.SenderId, row.ReceiverId }, response, timeout.Token);
            });
        }
    }
}
