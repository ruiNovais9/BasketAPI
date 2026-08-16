using BasketAPI.DTOs;
using BasketAPI.Validations;

namespace BasketAPI.Tests
{
    [TestClass]
    public sealed class BasketValidationTests
    {
        [TestMethod]
        public void Validate_Add_ReturnsMessage_WhenRequestIsNull()
        {
            Assert.IsNotNull(BasketValidation.Validate((AddProductToBasketRequest)null!));
        }

        [TestMethod]
        public void Validate_Add_ReturnsMessage_WhenQuantityIsZeroOrLess()
        {
            var request = new AddProductToBasketRequest { ProductId = 1, Quantity = 0 };
            Assert.IsNotNull(BasketValidation.Validate(request));
        }

        [TestMethod]
        public void Validate_Add_ReturnsMessage_WhenProductIdIsZeroOrLess()
        {
            var request = new AddProductToBasketRequest { ProductId = 0, Quantity = 1 };
            Assert.IsNotNull(BasketValidation.Validate(request));
        }

        [TestMethod]
        public void Validate_Add_ReturnsBothMessages_WhenBothInvalid()
        {
            var request = new AddProductToBasketRequest { ProductId = 0, Quantity = 0 };
            var result = BasketValidation.Validate(request);
            StringAssert.Contains(result, "Quantity");
            StringAssert.Contains(result, "ProductId");
        }

        [TestMethod]
        public void Validate_Add_ReturnsNull_WhenValid()
        {
            var request = new AddProductToBasketRequest { ProductId = 1, Quantity = 1 };
            Assert.IsNull(BasketValidation.Validate(request));
        }

        [TestMethod]
        public void Validate_Update_ReturnsMessage_WhenRequestIsNull()
        {
            Assert.IsNotNull(BasketValidation.Validate((UpdateProductToBasketRequest)null!));
        }

        [TestMethod]
        public void Validate_Update_ReturnsMessage_WhenProductIdIsZeroOrLess()
        {
            var request = new UpdateProductToBasketRequest { ProductId = 0, Quantity = 5 };
            Assert.IsNotNull(BasketValidation.Validate(request));
        }

        [TestMethod]
        public void Validate_Update_ReturnsNull_WhenValid()
        {
            var request = new UpdateProductToBasketRequest { ProductId = 1, Quantity = 0 };
            Assert.IsNull(BasketValidation.Validate(request));
        }
    }
}