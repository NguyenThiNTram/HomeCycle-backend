using HomeCycle.Domain.Entities;

namespace HomeCycle.Infrastructure.Persistences.Mappers
{
    public static class CartItemMapper
    {
        public static cart_item ToDomain(this Cart_Item entity)
        {
            if (entity == null) return null;

            return new cart_item
            {
                CartItemId = entity.CartItemId,
                UserId = entity.UserId,
                PostId = entity.PostId,
                Quantity = entity.Quantity,
                CreatedAt = entity.CreatedAt,

                Post = entity.Post?.ToDomain()
            };
        }

        public static Cart_Item ToInfrastructure(this cart_item entity)
        {
            if (entity == null) return null;

            return new Cart_Item
            {
                CartItemId = entity.CartItemId,
                UserId = entity.UserId,
                PostId = entity.PostId,
                Quantity = entity.Quantity,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
