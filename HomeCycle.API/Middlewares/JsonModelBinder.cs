using HomeCycle.Application.DTOs.Requests.Products;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeCycle.API.Middlewares
{
    public class JsonModelBinder : IModelBinder
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);

            var valueProviderResult = bindingContext.ValueProvider.GetValue(
                bindingContext.ModelName);

            if (valueProviderResult == ValueProviderResult.None)
            {
                bindingContext.Result = ModelBindingResult.Success(
                    new List<ProductAttributeValueRequest>());

                return Task.CompletedTask;
            }

            var result = new List<ProductAttributeValueRequest>();

            try
            {
                foreach (var rawValue in valueProviderResult.Values)
                {
                    if (string.IsNullOrWhiteSpace(rawValue))
                        continue;

                    var json = rawValue.Trim();

                    // Trường hợp một field chứa JSON array:
                    // [{...}, {...}]
                    if (json.StartsWith("["))
                    {
                        var items = JsonSerializer.Deserialize<
                            List<ProductAttributeValueRequest>>(
                                json,
                                JsonOptions);

                        if (items is not null)
                        {
                            result.AddRange(items);
                        }

                        continue;
                    }

                    // Trường hợp Swagger gửi nhiều field cùng tên,
                    // mỗi field chứa một JSON object: {...}
                    var item = JsonSerializer.Deserialize<
                        ProductAttributeValueRequest>(
                            json,
                            JsonOptions);

                    if (item is not null)
                    {
                        result.Add(item);
                    }
                }

                bindingContext.Result = ModelBindingResult.Success(result);
            }
            catch (JsonException ex)
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    $"Danh sách thuộc tính sản phẩm không đúng định dạng JSON: {ex.Message}");

                bindingContext.Result = ModelBindingResult.Failed();
            }

            return Task.CompletedTask;
        }
    }
}
