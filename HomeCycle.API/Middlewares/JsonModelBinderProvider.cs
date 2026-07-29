using HomeCycle.Application.DTOs.Requests.Products;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace HomeCycle.API.Middlewares
{
    public class JsonModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            //if (context == null)
            //    throw new ArgumentNullException(nameof(context));

            //// Tự động kích hoạt JsonModelBinder khi gặp thuộc tính AttributeValues 
            //// hoặc kiểu dữ liệu List<ProductAttributeValueRequest>
            //if (context.Metadata.ModelType == typeof(List<ProductAttributeValueRequest>) ||
            //    context.Metadata.PropertyName == "AttributeValues")
            //{
            //    return new BinderTypeModelBinder(typeof(JsonModelBinder));
            //}

            //return null;
            ArgumentNullException.ThrowIfNull(context);

            var modelType = context.Metadata.ModelType;

            var isSupportedType =
                modelType == typeof(IList<ProductAttributeValueRequest>) ||
                modelType == typeof(List<ProductAttributeValueRequest>) ||
                modelType == typeof(ICollection<ProductAttributeValueRequest>);

            if (!isSupportedType)
                return null;

            return new BinderTypeModelBinder(typeof(JsonModelBinder));
        }
    }
}
