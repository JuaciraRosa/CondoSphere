namespace CondoSphere.Services
{
    public interface IPaymentService
    {

        Task<(string clientSecret, string paymentIntentId)> CreateCardIntentAsync(int quotaId);

        Task HandleWebhookAsync(string json, string signatureHeader);

        Task<string> ConfirmAndMarkAsync(string intentId);



    }
}
