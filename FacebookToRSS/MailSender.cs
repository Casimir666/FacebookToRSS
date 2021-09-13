using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Proxy;
using MailKit.Net.Smtp;
using MimeKit;

namespace FacebookToRSS
{
    class MailSender
    {
        public MailSender()
        {
            Logger.LogMessage("Mail client configured for:");
            Logger.LogMessage($"  - Server {Configuration.Default.SmtpServer} on port {Configuration.Default.SmtpPort}");
            if (!string.IsNullOrEmpty(Configuration.Default.ProxyHost))
            {
                Logger.LogMessage($"  - Server {Configuration.Default.ProxyHost} on port {Configuration.Default.ProxyPort}");
            }
            Logger.LogMessage($"  - Sender {Configuration.Default.SenderAddress}");
            Logger.LogMessage($"  - Recipients {Configuration.Default.Recipients}");
        }

        public async Task SendAsync(string subject, string recipients, string html, CancellationToken cancellationToken = default)
        {
            var message = new MimeMessage();
            var bodyBuilder = new BodyBuilder();
            message.From.Add(new MailboxAddress(Configuration.Default.SenderAddress));
            foreach (var recipient in recipients.Split(";"))
            {
                message.To.Add(new MailboxAddress(recipient));
            }

            message.Subject = subject;
            bodyBuilder.HtmlBody = html;
            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                if (!string.IsNullOrEmpty(Configuration.Default.ProxyHost))
                {
                    client.ProxyClient = new Socks5Client(Configuration.Default.ProxyHost, Configuration.Default.ProxyPort);
                }

                await client.ConnectAsync(Configuration.Default.SmtpServer, Configuration.Default.SmtpPort, cancellationToken: cancellationToken);
                await client.AuthenticateAsync(Configuration.Default.SenderAddress, Configuration.Default.SenderPassword, cancellationToken);

                await client.SendAsync(message, cancellationToken);

                await client.DisconnectAsync(true, cancellationToken);
            }
        }
    }
}
