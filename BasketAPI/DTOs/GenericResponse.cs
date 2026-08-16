using BasketAPI.Domain;

namespace BasketAPI.DTOs
{
    public class GenericResponse
    {
        public GenericResponse()
        {
            IsSuccess = true;
        }
        public GenericResponse(string errorMessage)
        {
            ErrorMessage = errorMessage;
            IsSuccess = false;
        }
        public string ErrorMessage { get; set; }
        public bool IsSuccess { get; set; }
    }
}
