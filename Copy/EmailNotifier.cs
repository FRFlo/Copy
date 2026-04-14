using Copy.Types;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Globalization;

namespace Copy
{
    internal sealed record NotificationEmailEntry(DateTime Timestamp, string Severity, string Context, string Message);

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

        public void SendImmediate(string severity, string contextPrefix, string message)
        {
            SendMail($"[Copy][{severity}] {_smtp.Host}", BuildImmediateBody(severity, contextPrefix, message));
        }

        public void SendTaskSummary(string taskId, string? runId, IReadOnlyList<NotificationEmailEntry> entries)
        {
            if (entries.Count == 0)
            {
                return;
            }

            string severity = entries.Any(entry => entry.Severity == "ERREUR") ? "ERREUR" : "WARN";
            string subject = $"[Copy][{severity}][Task {taskId}] {_smtp.Host}";

            StringBuilder body = new();
            body.AppendLine($"Task: {taskId}");
            if (!string.IsNullOrWhiteSpace(runId))
            {
                body.AppendLine($"Run: {runId}");
            }

            body.AppendLine($"Reported at: {FormatTimestamp(DateTime.Now)}");
            body.AppendLine($"Warnings: {entries.Count(entry => entry.Severity == "WARN")}");
            body.AppendLine($"Errors: {entries.Count(entry => entry.Severity == "ERREUR")}");
            body.AppendLine();
            body.AppendLine("Events:");

            foreach (NotificationEmailEntry entry in entries)
            {
                body.AppendLine($"- {FormatTimestamp(entry.Timestamp)} | {entry.Severity}");
                if (!string.IsNullOrWhiteSpace(entry.Context))
                {
                    body.AppendLine($"  Context: {entry.Context}");
                }

                foreach (string line in entry.Message.Split(Environment.NewLine))
                {
                    body.AppendLine($"  {line}");
                }

                body.AppendLine();
            }

            SendMail(subject, body.ToString().TrimEnd());
        }

        private static string BuildImmediateBody(string severity, string contextPrefix, string message)
        {
            StringBuilder body = new();
            body.AppendLine($"Timestamp: {FormatTimestamp(DateTime.Now)}");
            body.AppendLine($"Severity: {severity}");
            if (!string.IsNullOrWhiteSpace(contextPrefix))
            {
                body.AppendLine($"Context: {contextPrefix.Trim()}");
            }

            body.AppendLine();
            body.AppendLine(message);
            return body.ToString();
        }

        private void SendMail(string subject, string body)
        {
            using MailMessage email = new()
            {
                From = new MailAddress(_smtp.From),
                Subject = subject,
                Body = body
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

        private static string FormatTimestamp(DateTime timestamp)
        {
            return timestamp.ToLocalTime().ToString("dddd d MMMM yyyy HH:mm:ss", CultureInfo.CurrentCulture);
        }
    }
}
