using BasketAPI.DTOs;

namespace BasketAPI.Validations
{
    public static class BasketValidation
    {
        public static string? Validate(AddProductToBasketRequest request)
        {
            if (request == null)
            {
                return "Request is null";
            }

            var errorMessage = new List<string>();

            if (request.Quantity <= 0)
            {
                errorMessage.Add("Quantity should be bigger than 0");
            }

            if (request.ProductId <= 0)
            {
                errorMessage.Add("ProductId should be bigger than 0");
            }

            return errorMessage.Count == 0
                    ? null
                    : string.Join(Environment.NewLine, errorMessage);
        }

        public static string? Validate(UpdateProductToBasketRequest request)
        {
            if (request == null)
            {
                return "Request is null";
            }

            var errorMessage = new List<string>();

            if (request.ProductId <= 0)
            {
                errorMessage.Add("ProductId should be bigger than 0");
            }

            return errorMessage.Count == 0
                    ? null
                    : string.Join(Environment.NewLine, errorMessage);
        }
    }
}
