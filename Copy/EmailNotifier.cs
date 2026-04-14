using Copy.Types;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Copy
{
    internal sealed class EmailNotifier(string[] recipients, SmtpSettings smtp)
    {
        private readonly string[] _recipients = recipients;
        private readonly SmtpSettings _smtp = smtp;

        public static EmailNotifier? Create(string[] recipients, SmtpSettings? smtp)
        {
            if (recipients.Length == 0 || smtp == null)
            {
                return null;
            }

            return new EmailNotifier(recipients, smtp);
        }

        public static string[] NormalizeRecipients(IEnumerable<string>? recipients)
        {
            return recipients?
                .Where(address => !string.IsNullOrWhiteSpace(address))
                .Select(address => address.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? [];
        }

        public static bool IsValidEmailAddress(string emailAddress)
        {
            try
            {
                _ = new MailAddress(emailAddress);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public void Send(string severity, string contextPrefix, string message)
        {
            using MailMessage email = new()
            {
                From = new MailAddress(_smtp.From),
                Subject = $"[Copy][{severity}] {_smtp.Host}",
                Body = BuildBody(severity, contextPrefix, message)
            };

            foreach (string recipient in _recipients)
            {
                email.To.Add(recipient);
            }

            using SmtpClient client = new(_smtp.Host, _smtp.Port)
            {
                EnableSsl = _smtp.EnableSsl, DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(_smtp.Username))
            {
                client.Credentials = new NetworkCredential(_smtp.Username, _smtp.Password);
            }

            client.Send(email);
        }

        private static string BuildBody(string severity, string contextPrefix, string message)
        {
            StringBuilder body = new();
            body.AppendLine($"Timestamp: {DateTime.Now:O}");
            body.AppendLine($"Severity: {severity}");
            if (!string.IsNullOrWhiteSpace(contextPrefix))
            {
                body.AppendLine($"Context: {contextPrefix.Trim()}");
            }

            body.AppendLine();
            body.AppendLine(message);
            return body.ToString();
        }
    }
}