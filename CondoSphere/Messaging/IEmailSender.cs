namespace CondoSphere.Messaging
{
    public interface IEmailSender
    {
        System.Threading.Tasks.Task SendAsync(string to, string subject, string htmlBody);
        System.Threading.Tasks.Task SendBulkBccAsync(
          IEnumerable<string> bccList,
          string subject,
          string htmlBody,
          string? attachmentUrl = null);
    }
}
