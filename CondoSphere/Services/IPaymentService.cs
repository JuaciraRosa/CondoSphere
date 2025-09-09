namespace CondoSphere.Services
{
    public interface IPaymentService
    {
        Task<string> CreateCheckoutSessionForQuotaAsync(int quotaId, string successUrl, string cancelUrl);
        Task<string> ConfirmAndMarkAsync(string paymentIntentId);
        Task HandleWebhookAsync(string json, string signatureHeader);
        Task<(string clientSecret, string paymentIntentId)> CreateCardIntentAsync(int quotaId);
    }
}
