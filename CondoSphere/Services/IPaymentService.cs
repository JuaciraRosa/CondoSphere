namespace CondoSphere.Services
{
    public interface IPaymentService
    {

        Task<(string clientSecret, string paymentIntentId)> CreateCardIntentAsync(int quotaId);
        Task<(string paymentIntentId, string entityRef, DateTime expiresAt)> CreateMultibancoAsync(int quotaId);
        Task HandleWebhookAsync(string json, string signatureHeader);
    }
}
