using Microsoft.AspNetCore;
using System;

namespace InstaSwarm.services
{
    //stuff to improve: each instagramclient class object has its own logger, but it would be better to have a single logger for the whole InstagramAgent class somohow.
    //stuff to add: better logging, like sending messages to the admin user about the status of the video processing, like if it was successfully posted or not. 
    // <summary>
    //    Design Goals for InstagramAgent
    //    Coordinate Workflow: Manage the process of receiving video links, downloading, storing, and uploading to Instagram accounts.
    //    Queue Management: Handle a queue of videos(initially using Instagram DMs, with potential SQLite integration).
    //    File Management: Interface with the YtDlp class to download videos and store them in the./videos directory.
    //    API Integration: Use the InstagramClient to upload videos to multiple Instagram accounts.
    //    Moderation: Restrict video submissions to approved contributors and allow the owner to approve/reject videos.
    //    Extensibility: Support future expansion to TikTok/YouTube and a dashboard.
    //    Error Handling: Robustly handle failures (e.g., invalid links, API limits, download errors).
    // </summary>
    public class InstagramAgent
    {
        private readonly ILogger<InstagramAgent> logger;
        private const int MaxUniqueWebhookTimes = 20;
        private Queue<string>? _uniqueMessagingMIDs = new();
        public readonly string Reel = "ig_reel";
        public InstagramUser AdminUser { get; set; } = new();
        public string PublicBaseURL { get; set; }
        public List<InstagramClient>? Clients { get; set; } = new List<InstagramClient>();
        public InstagramAgent(string[] tokens, string AdminInstagramID, ILoggerFactory loggerFactory1)
        {
            logger = loggerFactory1.CreateLogger<InstagramAgent>();

            foreach (string token in tokens)
            {
                Clients.Add(new InstagramClient(token, loggerFactory1));
            }

            DotNetEnv.Env.Load();
            PublicBaseURL = DotNetEnv.Env.GetString("PUBLIC_BASE_URL") ?? throw new InvalidOperationException("PUBLIC_BASE_URL is missing in environment variables");

            InitializeAdmin(AdminInstagramID).Wait();
        }
        private async Task InitializeAdmin(string AdminInstagramID)
        {
            logger.BeginScope($"InstagramAgent.InitializeAdmin: ");
            logger.LogInformation($"Initializing Admin...");

            AdminUser = await Clients![0].InitializeUserInfo(
                    UserID: AdminInstagramID,
                    creatorOnlyPropsToget: ""
                    );
        }
        public async Task<string> PublishVideoFromLink(string videoLink, string caption, YtDlp ytDlp)
        {
            string result = string.Empty;
            string videoPath = ytDlp.DownloadVideo(videoLink).Replace("video/", "");
            string EncodedvideoPath = Uri.EscapeDataString(videoPath.Replace("\"", ""));
            string publicVideoURL = $"{PublicBaseURL}{EncodedvideoPath}";

            foreach (InstagramClient client in Clients!)
            {
                string containerId = await client.PostMedia(
                    new InstagramMediaContainer(
                        InstagramMediaType.REELS,
                        publicVideoURL,
                        caption),
                100);

                if (containerId != string.Empty)
                {
                    result += $"Video published successfully on account {client.User.Username} with container ID: {containerId}\n";
                }
                else
                {
                    result += $"Failed to publish video on account {client.User.Username}.\n";
                }
            }
            return result;
        }
        /// <summary>
        /// this method will post on every account in the Clients list
        /// </summary>
        public async Task<string> PostToAllAccounts(InstagramMediaContainer mediaContainer, int delayBeforePublishingInSeconds = 15)
        {
            if (string.IsNullOrEmpty(mediaContainer.MediaUrl))
            {
                throw new ArgumentException("in media container Video link(MediaUrl) cannot be null or empty, in the InstagramAgent.PostToAllAcounts", nameof(mediaContainer.MediaUrl));
            }

            string result = string.Empty;

            foreach (var client in Clients!)
            {
                if (await client.HasEnoughPublishesLeftForToday())
                {
                    logger.LogInformation($"Client {client.User.Username} has enough publishes left for today.");
                }
                else
                {
                    logger.LogWarning($"Client {client.User.Username} has no publishes left for today, skipping posting.");
                    continue; // Skip posting if the client has no publishes left for today
                }
                result += $"Posting to account: {client.User.Username}\n";
                string responce = await client.PostMedia(mediaContainer, delayBeforePublishingInSeconds);
                result += responce != string.Empty ? $"Posted successfully with container ID: {responce}\n" : $"Failed to post on account: {client.User.Username}\n";
            }
            return result;
        }
        /// <summary>
        /// this will check if the video link is already in the queue
        /// </summary>
        public string ExtractVideoLinkFromMessage(string messageText)
        {
            if (string.IsNullOrEmpty(messageText))
            {
                throw new ArgumentException("Message text cannot be null or empty.", nameof(messageText));
            }

            List<string> words = [];

            string currentWord = "";
            string videoLink = "";

            for (int i = 0; i < messageText.Length; i++)
            {
                char currentCharacter = messageText[i];

                if (currentCharacter == ' ' || currentCharacter == '\n' || currentCharacter == '\r')
                {
                    words.Add(currentWord);

                    if (IsValidVideoLink(currentWord, out videoLink))
                    {
                        return videoLink;
                    }

                    currentWord = "";
                }
                else
                {
                    currentWord += currentCharacter;
                }
            }
            return "";
        }
        public bool IsValidVideoLink(string videoLink, out string cleanLink)
        {
            if (string.IsNullOrEmpty(videoLink))
            {
                cleanLink = "";
                return false;
            }
            Uri uriResult;
            bool result = Uri.TryCreate(videoLink, UriKind.Absolute, out uriResult!)
                          && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (result)
            {
                cleanLink = $"{uriResult!.Scheme}://{uriResult.Host}{uriResult.AbsolutePath}";
                return result;
            }
            cleanLink = "";
            return false;
        }
        // <summary>`
        // this methis is used to determine if webhook is unique or was it already processed
        // </summary>
        public bool IsUniqueMeassagingMID(Messaging messaging)
        {
            logger.BeginScope($"InstagramAgent.IsUniqueMeassagingMID: ");
            logger.LogInformation($"Checking uniqueness of Message MID that was sent at: {messaging.Timestamp}");

            if (_uniqueMessagingMIDs!.Contains(messaging.Message.Mid))
            {
                logger.LogInformation($"Meassage with MID {messaging.Message.Mid} is not unique, already processing.");
                return false;
            }
            else
            {
                logger.LogInformation($"Webhook with ID {messaging.Message.Mid} is unique, processing it.");
                _uniqueMessagingMIDs.Enqueue(messaging.Message.Mid);
                TrimOldUniqueMessagingMIDs();
                return true;
            }
        }
        /// <summary>
        /// method proceses the webhook and returns a response
        /// </summary>
        public async Task<string> ProcessWebhook(InstagramWebhook webhook, YtDlp ytDlp)
        {
            logger.BeginScope($"InstagramAgent.ProcessWebhook: ");
            logger.LogInformation($"Processing webhook with Object: {webhook.Object}");

            if (webhook == null || webhook.Entry == null || webhook.Entry.Count == 0 || webhook.Object != "instagram")
            {
                logger.LogWarning("Invalid empty or not needed webhook payload received");
                return "Invalid or not needed webhook payload";
            }

            foreach (Entry entry in webhook.Entry)
            {
                if (entry.Messaging == null || entry.Messaging.Count == 0)
                {
                    logger.LogWarning($"No messaging data found in the webhook entry id={entry.Id}.");
                    continue;
                }
                foreach (Messaging msg in entry.Messaging)
                {
                    string senderID = msg.Sender.Id;
                    if (senderID != AdminUser.ID)
                    {
                        logger.LogInformation($"sender was not ADMIN. it was someone with id: {senderID}"); // for future maybe get more info about who sent the message
                        continue; // Skip processing if the message is not from the admin
                    }
                    if (!IsUniqueMeassagingMID(msg))
                    {
                        logger.LogInformation($"Webhook is not unique, skipping processing.");
                        continue; // Skip processing if the webhook is not unique
                    }
                    string? text = msg.Message.Text;
                    logger.LogInformation($"Message from admin with ID: {senderID} at [{msg.Timestamp}] texted: {text}");
                    if (msg.Message.Attachments == null)
                    {
                        logger.LogInformation("no attachment was sent with a message. skipping processing");
                        continue; // Skip processing if there are no attachments
                    }

                    foreach (Attachment attachment in msg.Message.Attachments)
                    {
                        if (attachment.Type != Reel)
                        {
                            logger.LogInformation("attachment sent was not a Reel. skipping processing");
                            continue; // Skip processing if the attachment is not a Reel
                        }

                        string fullTitle = DetermineCaption(text, attachment.Payload.Title, entry.Time);
                        string firstLine = fullTitle.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                        string title = ytDlp.CorrectVideoNameFormat(firstLine);
                        string videoURL = attachment.Payload.Url;
                        string videoPath = ytDlp.DownloadVideo(videoURL, $"video/{title}.mp4")
                            ?? throw new Exception("error while downloading the video for upload\nHINT probably cookie problem");

                        string IGAggentResponce = await PostToAllAccounts(
                            new InstagramMediaContainer(
                                InstagramMediaType.REELS,
                                $"{PublicBaseURL}{videoPath.Replace("video/", "")}",
                                fullTitle),
                            100);

                        string result = string.Empty;

                        if (String.IsNullOrEmpty(IGAggentResponce))
                        {
                            return "Failed to post video on all accounts.";
                        }

                        string videosDirectory = videoPath.Replace($"{title}.mp4", "");
                        ytDlp.DeleteOldVideos(videosDirectory);
                        return $"success: {IGAggentResponce}";
                    }
                }
            }
            return "webhook processing finished";
        }
        /// <summary>
        ///     this method determines caption based on arguments, if taxt sent is null or empty it will use the title of the video,
        ///     if not it will use the text as a caption plus the tags extracted from the video caption,
        ///     and if none are present use timestamp of the video as a caption.
        /// </summary>
        public string DetermineCaption(string? text, string videoTitle, long videoTimestamp)
        {
            if (!string.IsNullOrEmpty(text))
            {
                return $"{text} {String.Join(" ", ExtractTags(videoTitle))}";
            }
            else if (!string.IsNullOrEmpty(videoTitle))
            {
                return videoTitle;
            }
            else
            {
                DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(videoTimestamp);
                DateTime dateTimeLocal = dateTimeOffset.LocalDateTime;
                return $"Video uploaded at {dateTimeLocal:yyyy-MM-dd HH:mm:ss}";
            }
        }


        /// <summary>
        ///    this method extracts tags from the video caption or any string.
        ///    tags are considered to be words that start with a hash (#) symbol.
        ///    the tags are returned as a list of strings. so use join to combine them into a single string.
        /// </summary>
        public List<string> ExtractTags(string caption)
        {
            if (string.IsNullOrEmpty(caption))
            {
                return new List<string>();
            }
            List<string> tags = new List<string>();
            string[] words = caption.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string word in words)
            {
                if (word.StartsWith("#"))
                {
                    tags.Add(word.Trim());
                }
            }
            return tags;
        }
        private bool IsMessageFromAdmin(InstagramWebhook webhook)
        {
            if (webhook.Entry == null || webhook.Entry.Count == 0)
            {
                return false;
            }
            foreach (var entry in webhook.Entry)
            {
                if (entry.Messaging != null && entry.Messaging.Count > 0)
                {
                    var sender = entry.Messaging[0].Sender;
                    if (sender != null && sender.Id == AdminUser.ID)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private void TrimOldUniqueMessagingMIDs()
        {
            if (_uniqueMessagingMIDs!.Count > MaxUniqueWebhookTimes)
            {
                _uniqueMessagingMIDs.Dequeue();
            }
        }
    }
}
