using BasketAPI.Client;
using BasketAPI.Domain;
using BasketAPI.DTOs;
using BasketAPI.Interfaces;
using BasketAPI.Services;
using BasketAPI.Validations;
using Microsoft.AspNetCore.Mvc;

namespace BasketAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BasketController : ControllerBase
    {
        private readonly ILogger<BasketController> _logger;
        private readonly IBasketService _basketService;
        private readonly IProductService _productService;
        public BasketController(ILogger<BasketController> logger, IBasketService basketService, IProductService productService)
        {
            _logger = logger;
            _basketService = basketService;
            _productService = productService;
        }

        [HttpGet("Get")]
        public async Task<ActionResult<OrderResponse>> Get(Guid guid)
        {
            var basket = await _basketService.GetBasket(guid);

            if (basket == null)
            {
                return NotFound("Basket not found");
            }

            return Ok(new OrderResponse(basket));
        }

        [HttpPost("Create")]
        public async Task<ActionResult<Guid>> Create()
        {
            var basket = await _basketService.CreateBasket(ImpactApiClient._emailDefault);
            return Ok(basket.Id);
        }

        [HttpPut("Add")]
        public async Task<ActionResult<OrderResponse>> Add(AddProductToBasketRequest addProductToBasketRequest)
        {
            string errorMessage = BasketValidation.Validate(addProductToBasketRequest);

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                return BadRequest(new OrderResponse(errorMessage));
            }

            Basket basket = await _basketService.GetBasket(addProductToBasketRequest.BasketId);

            if (basket == null)
            {
                return NotFound(new OrderResponse("Basket not found"));
            }

            if (basket.AlreadyOrder)
            {
                return BadRequest(new OrderResponse("Basket already Ordered"));
            }

            ProductResponse findProduct = await _basketService.GetProductById(addProductToBasketRequest.ProductId);

            if (findProduct == null)
            {
                return NotFound(new OrderResponse("Product not found"));
            }

            List<ProductResponse> topProducts = await _productService.GetTopProducts();

            if (!topProducts.Any(p => p.Id == findProduct.Id))
            {
                return BadRequest(new OrderResponse("Product is not in top 100."));
            }

            BasketItem findProductOnBasket = basket.Items.FirstOrDefault(x => x.ProductId == addProductToBasketRequest.ProductId);

            if (findProductOnBasket == null)
            {
                decimal productPrice = findProduct.Price * addProductToBasketRequest.Quantity;

                basket.Items.Add(new BasketItem
                {
                    Price = productPrice,
                    IndividualPrice = findProduct.Price,
                    ProductId = findProduct.Id,
                    Quantity = addProductToBasketRequest.Quantity
                });

                basket.TotalPrice = basket.Items.Sum(x => x.Price);
                basket.UpdateDate = DateTime.UtcNow;
            }
            else
            {
                findProductOnBasket.Quantity += addProductToBasketRequest.Quantity;
                findProductOnBasket.Price = findProductOnBasket.Quantity * findProductOnBasket.IndividualPrice;

                basket.TotalPrice = basket.Items.Sum(x => x.Price);
                basket.UpdateDate = DateTime.UtcNow;
            }

            return Ok(new OrderResponse(basket));
        }

        [HttpDelete("Delete")]
        public async Task<ActionResult<OrderResponse>> Delete(Guid basketId, int productId)
        {
            if (productId <= 0)
            {
                return BadRequest(new OrderResponse("ProductId is not valid"));
            }

            Basket basket = await _basketService.GetBasket(basketId);

            if (basket == null)
            {
                return NotFound(new OrderResponse("Basket not found"));
            }

            if (basket.AlreadyOrder)
            {
                return BadRequest(new OrderResponse("Basket already Ordered"));
            }

            BasketItem findProductOnBasket = basket.Items.FirstOrDefault(x => x.ProductId == productId);

            if (findProductOnBasket == null)
            {
                return NotFound(new OrderResponse("Product not found on Basket!!"));
            }

            basket.Items.Remove(findProductOnBasket);

            basket.TotalPrice = basket.Items.Sum(x => x.Price);

            return Ok(new OrderResponse(basket));

        }

        [HttpPost("Update")]
        public async Task<ActionResult<OrderResponse>> Update(UpdateProductToBasketRequest updateProductToBasketRequest)
        {
            string errorMessage = BasketValidation.Validate(updateProductToBasketRequest);

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                return BadRequest(new OrderResponse(errorMessage));
            }

            Basket basket = await _basketService.GetBasket(updateProductToBasketRequest.BasketId);

            if (basket == null)
            {
                return NotFound(new OrderResponse("Basket not found"));
            }

            if (basket.AlreadyOrder)
            {
                return BadRequest(new OrderResponse("Basket already Ordered"));
            }

            ProductResponse findProduct = await _basketService.GetProductById(updateProductToBasketRequest.ProductId);

            if (findProduct == null)
            {
                return NotFound(new OrderResponse("Product not found"));
            }

            List<ProductResponse> topProducts = await _productService.GetTopProducts();

            if (!topProducts.Any(p => p.Id == findProduct.Id))
            {
                return BadRequest(new OrderResponse("Product is not in top 100."));
            }

            BasketItem findProductOnBasket = basket.Items.FirstOrDefault(x => x.ProductId == updateProductToBasketRequest.ProductId);

            if (findProductOnBasket == null)
            {
                if (updateProductToBasketRequest.Quantity <= 0)
                {
                    return BadRequest(new OrderResponse("The quantity of product is not valid."));
                }

                decimal productPrice = findProduct.Price * updateProductToBasketRequest.Quantity;

                basket.Items.Add(new BasketItem
                {
                    Price = productPrice,
                    IndividualPrice = findProduct.Price,
                    ProductId = findProduct.Id,
                    Quantity = updateProductToBasketRequest.Quantity
                });

                basket.TotalPrice = basket.Items.Sum(x => x.Price);
                basket.UpdateDate = DateTime.UtcNow;
            }
            else
            {
                if (updateProductToBasketRequest.Quantity <= 0)
                {
                    basket.Items.Remove(findProductOnBasket);
                }
                else
                {
                    decimal newPrice = findProduct.Price * updateProductToBasketRequest.Quantity;

                    findProductOnBasket.Quantity = updateProductToBasketRequest.Quantity;
                    findProductOnBasket.Price = newPrice;
                    findProductOnBasket.IndividualPrice = findProduct.Price;
                }
                basket.TotalPrice = basket.Items.Sum(x => x.Price);
                basket.UpdateDate = DateTime.UtcNow;
            }

            return Ok(new OrderResponse(basket));
        }

        [HttpPost("CreateOrder")]
        public async Task<ActionResult<OrderResponse>> CreatOrder(Guid guid)
        {
            Basket basket = await _basketService.GetBasket(guid);

            if (basket == null)
            {
                return NotFound(new OrderResponse("Basket not found"));
            }

            if (basket.AlreadyOrder)
            {
                return BadRequest(new OrderResponse("Basket already Ordered"));
            }

            if (basket.Items.Count == 0)
            {
                return BadRequest(new OrderResponse("Basket don't have any items"));
            }

            var orderLines = new List<OrderLine>();

            List<int> listProductIds = basket.Items.Select(x => x.ProductId).ToList();

            List<ProductResponse> productsInfo = await _productService.GetProductByIds(listProductIds);

            if (productsInfo.Count != listProductIds.Count)
            {
                return NotFound(new OrderResponse("Some products as not found"));
            }

            foreach (BasketItem items in basket.Items)
            {
                ProductResponse productInfo = productsInfo.FirstOrDefault(x => x.Id == items.ProductId);

                orderLines.Add(new OrderLine
                {
                    ProductId = items.ProductId,
                    Quantity = items.Quantity,
                    TotalPrice = items.Price,
                    ProductUnitPrice = items.IndividualPrice,
                    ProductName = productInfo.Name,
                    ProductSize = productInfo.Size.ToString()
                });
            }

            var createOrderRequest = new CreateOrderRequest
            {
                UserEmail = ImpactApiClient._emailDefault,
                TotalAmount = basket.TotalPrice,
                OrderLines = orderLines
            };

            CreateOrderResponse order = await _productService.CreateOrder(createOrderRequest);

            if (order == null)
            {
                return BadRequest("The response to create order is null.");
            }

            await _basketService.UpdateBasket(guid, order.OrderId);

            return Ok(new OrderResponse(order));
        }

        [HttpGet("GetOrder")]
        public async Task<ActionResult<OrderResponse>> GetOrder(string guid)
        {
            return Ok(new OrderResponse(await _productService.GetOrder(guid)));
        }
    }
}
