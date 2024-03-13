using Copy.Types;
using Microsoft.Exchange.WebServices.Data;
using System.Text.RegularExpressions;

namespace Copy.Clients
{
    internal class Exchange : IClient
    {
        /// <summary>
        /// Size used for every <see cref="ItemView"/>
        /// </summary>
        private static readonly int viewSize = 100;
        /// <summary>
        /// Client credentials.
        /// </summary>
        private readonly Client _credentials;
        /// <summary>
        /// FTP client.
        /// </summary>
        private readonly ExchangeService ExchangeService;

        public Client Config => _credentials;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="credentials"></param>
        public Exchange(Client credentials)
        {
            _credentials = credentials;

            if (credentials.Autodiscover)
            {
                ExchangeService = new ExchangeService()
                {
                    Credentials = new WebCredentials(credentials.Username, credentials.Password)
                };
                ExchangeService.AutodiscoverUrl(credentials.Username);
            }
            else
            {
                ExchangeService = new ExchangeService()
                {
                    Credentials = new WebCredentials(credentials.Username, credentials.Password),
                    Url = new Uri(credentials.Host)
                };
            }
        }

        public bool DoFileExist(ListResult element)
        {
            FindItemsResults<Item> findResults = ExchangeService.FindItems(WellKnownFolderName.Inbox, element.Location, new ItemView(1));
            return findResults.Items.Count > 0;
        }

        public ListResult[] ListFiles(string path, CopyFilter filter)
        {
            ItemView view = new(viewSize)
            {
                PropertySet = new PropertySet(BasePropertySet.FirstClassProperties)
            };
            FindItemsResults<Item> findResults;
            List<EmailMessage> emails = [];

            do
            {
                findResults = ExchangeService.FindItems(WellKnownFolderName.Inbox, view);
                foreach (var item in findResults.Items)
                {
                    emails.Add((EmailMessage)item);
                }
                view.Offset += viewSize;
            }
            while (findResults.MoreAvailable);

            Regex nameRegex = new(filter.Name);
            Regex authorRegex = new(filter.Author);
            List<ListResult> results = [];

            foreach (var item in emails)
            {
                if (item is null) continue;
                if (!item.HasAttachments) continue;
                if (item.DateTimeReceived <= filter.CreatedAfter) continue;
                if (!authorRegex.IsMatch(item.Sender.Address)) continue;

                foreach (var attachment in item.Attachments)
                {
                    if (attachment is FileAttachment fileAttachment)
                    {
                        if ((ulong)fileAttachment.Size >= filter.MaxSize) continue;
                        if ((ulong)fileAttachment.Size <= filter.MinSize) continue;
                        if (!nameRegex.IsMatch(fileAttachment.Name)) continue;

                        results.Add(new ListResult(item.Id.UniqueId, fileAttachment.Name));
                    }
                }
            }

            return [.. results];
        }

        public Stream GetFile(ListResult element)
        {
            FindItemsResults<Item> findResults = ExchangeService.FindItems(WellKnownFolderName.Inbox, element.Location, new ItemView(1));
            EmailMessage email = (EmailMessage)findResults.Items[0];
            FileAttachment fileAttachment = (FileAttachment)email.Attachments.Where(a => a.Name == element.ElementName).First();
            var stream = new MemoryStream();
            fileAttachment.Load(stream);
            stream.Position = 0;
            return stream;
        }

        public void PutFile(ListResult element, Stream stream)
        {
            EmailMessage email = new(ExchangeService)
            {
                Subject = element.Location,
                Body = new MessageBody(BodyType.Text, "Attachment"),
                ToRecipients = { new EmailAddress(element.Location) },
            };

            email.Attachments.AddFileAttachment(element.ElementName, stream);
            email.SendAndSaveCopy();
        }

        public void MoveElement(ListResult sourceElement, ListResult destinationElement)
        {
            FindItemsResults<Item> findResults = ExchangeService.FindItems(WellKnownFolderName.Inbox, sourceElement.Location, new ItemView(1));
            EmailMessage sourceEmail = (EmailMessage)findResults.Items[0];
            FileAttachment fileAttachment = (FileAttachment)sourceEmail.Attachments.Where(a => a.Name == sourceElement.ElementName).First();

            var stream = new MemoryStream();
            fileAttachment.Load(stream);
            stream.Position = 0;

            sourceEmail.Delete(DeleteMode.MoveToDeletedItems);

            EmailMessage destinationEmail = new(ExchangeService)
            {
                Subject = destinationElement.Location,
                Body = new MessageBody(BodyType.Text, "Attachment"),
                ToRecipients = { new EmailAddress(destinationElement.Location) },
            };

            destinationEmail.Attachments.AddFileAttachment(destinationElement.ElementName, stream);
            destinationEmail.SendAndSaveCopy();
        }

        public void CopyElement(ListResult sourceElement, ListResult destinationElement)
        {
            FindItemsResults<Item> findResults = ExchangeService.FindItems(WellKnownFolderName.Inbox, sourceElement.Location, new ItemView(1));
            EmailMessage sourceEmail = (EmailMessage)findResults.Items[0];
            FileAttachment fileAttachment = (FileAttachment)sourceEmail.Attachments.Where(a => a.Name == sourceElement.ElementName).First();

            var stream = new MemoryStream();
            fileAttachment.Load(stream);
            stream.Position = 0;

            EmailMessage destinationEmail = new(ExchangeService)
            {
                Subject = destinationElement.Location,
                Body = new MessageBody(BodyType.Text, "Attachment"),
                ToRecipients = { new EmailAddress(destinationElement.Location) },
            };

            destinationEmail.Attachments.AddFileAttachment(destinationElement.ElementName, stream);
            destinationEmail.SendAndSaveCopy();
        }

        public void DeleteElement(ListResult element)
        {
            FindItemsResults<Item> findResults = ExchangeService.FindItems(WellKnownFolderName.Inbox, element.Location, new ItemView(1));
            EmailMessage email = (EmailMessage)findResults.Items[0];
            email.Delete(DeleteMode.MoveToDeletedItems);
        }

        public void Dispose()
        {
            // No resources to dispose in this class
        }
    }
}
