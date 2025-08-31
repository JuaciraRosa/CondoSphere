namespace CondoSphere.Messaging
{
    public interface IEmailSender
    {
        System.Threading.Tasks.Task SendAsync(string to, string subject, string htmlBody);
    }
}
