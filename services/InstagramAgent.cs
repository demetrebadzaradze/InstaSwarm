using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using System;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace InstaSwarm.services
{
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
        public readonly string Reel = "ig_reel";
        public string AdminUsername { get; set; }
        public string PublicBaseURL { get; set; }
        public List<InstagramClient>? Clients { get; set; } = new List<InstagramClient>();
        public InstagramAgent(string[] tokens)
        {
            foreach (string token in tokens)
            {
                Clients.Add(new InstagramClient(token));
            }

            DotNetEnv.Env.Load();
            PublicBaseURL = DotNetEnv.Env.GetString("PUBLIC_BASE_URL") ?? throw new InvalidOperationException("PUBLIC_BASE_URL is missing in environment variables"); ;
            AdminUsername = DotNetEnv.Env.GetString("ADMIN_INSTAGRAM_USER_USERNAME") ?? throw new InvalidOperationException("ADMIN_INSTAGRAM_USER_USERNAME is missing in environment variables"); ;
        }
        public async Task<string> PublishVideoFromLink(string videoLink, string caption, YtDlp ytDlp)
        {
            string result = string.Empty;
            string videoPath = ytDlp.DownloadVideo(videoLink).Replace("video/", "");
            string EncodedvideoPath = Uri.EscapeDataString(videoPath.Replace("\"", ""));
            string publicVideoURL = $"{PublicBaseURL}{EncodedvideoPath}";

            foreach (InstagramClient client in Clients)
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
        public bool IsMessageFromAdmin(InstagramUser user)
        {
            if (user.Username == AdminUsername)
            {
                return true;
            }
            else
            {
                Console.WriteLine($"Message from non-admin user: {user.Username}");
                return false;
            }
        }
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
        public bool IsValidVideoLink(string videoLink, out string cleanLink )
        {
            if (string.IsNullOrEmpty(videoLink))
            {
                cleanLink = "";
                return false;
            }
            Uri uriResult;
            bool result = Uri.TryCreate(videoLink, UriKind.Absolute, out uriResult)
                          && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            if (result)
            {
                cleanLink = $"{uriResult.Scheme}://{uriResult.Host}{uriResult.AbsolutePath}";
                return result;
            }
            cleanLink = "";
            return false;
        }

    }
}
