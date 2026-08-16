using BasketAPI.Domain;

namespace BasketAPI.DTOs
{
    public class GenericResponse
    {
        public GenericResponse()
        {
            IsSucess = true;
        }
        public GenericResponse(string errorMessage)
        {
            ErrorMessage = errorMessage;
            IsSucess = false;
        }
        public string ErrorMessage { get; set; }
        public bool IsSucess { get; set; }
    }
}
