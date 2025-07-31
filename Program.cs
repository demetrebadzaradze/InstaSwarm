using InstaSwarm.services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.Intrinsics.Arm;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(opts =>
    opts.SerializerOptions.PropertyNameCaseInsensitive = true);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

DotNetEnv.Env.Load();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080); // HTTP
    options.ListenAnyIP(8081, listenOptions =>
    {
        listenOptions.UseHttps("https-dev.pfx", DotNetEnv.Env.GetString("HTTPS_CERT_PASSWORD")); // Enable HTTPS
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Initialize InstagramAgent with tokens from environment variables
InstagramAgent IGagent = new InstagramAgent(
    DotNetEnv.Env.GetString("INSTAGRAM_USER_TOKENS")?.Split(',') ?? Array.Empty<string>());

// Initialize YtDlp with the path from environment variables or default to "yt-dlp.exe"s
string ytDlpPath = DotNetEnv.Env.GetString("YTDLP_PATH");
if (String.IsNullOrEmpty(ytDlpPath))
{
    ytDlpPath = "yt-dlp.exe"; // Default path if not set in environment variables
}
YtDlp ytDlp = new YtDlp(ytDlpPath, DotNetEnv.Env.GetString("COOKIES_PATH") ?? "cookies.txt");

app.MapGet("/", () =>
{
    //string secret = DotNetEnv.Env.GetString("INSTAGRAM_USER_TOKEN");
    //InstagramClient client = new InstagramClient(secret);
    //return client.RefreshAccessToken();
})
.WithName("RefreshAccessToken")
.WithOpenApi();

app.MapGet("/dowloadvideo", (string videoURL) =>
{
    return ytDlp.DownloadVideo(videoURL);
})
.WithName("DownloadReel")
.WithOpenApi();

app.MapGet("/postvideo", (string videoURL, string caption) =>
{
    string secret = DotNetEnv.Env.GetString("INSTAGRAM_USER_TOKEN");
    InstagramClient client = new InstagramClient(secret);
    return client.PostMedia(
        new InstagramMediaContainer(
            InstagramMediaType.REELS,
            videoURL,
            caption));
})
.WithName("PostVideo")
.WithOpenApi();

app.MapGet("/download-and-upload", async (string videoURL, string caption) =>
{
    string secret = DotNetEnv.Env.GetString("INSTAGRAM_USER_TOKEN");
    string baseurl = DotNetEnv.Env.GetString("PUBLIC_BASE_URL");
    string videoPath = ytDlp.DownloadVideo(videoURL).Replace("video/", "");
    string EncodedvideoPath = Uri.EscapeDataString(videoPath.Replace("\"", ""));
    InstagramClient client = new InstagramClient(secret);
    string localVideoURL = $"{baseurl}{EncodedvideoPath}";
    string containerId = await client.PostMedia(
        new InstagramMediaContainer(
            InstagramMediaType.REELS,
            localVideoURL,
            caption),
        100);
    return $"Video uploaded successfully with ID: {containerId} \nlink: https://www.instagram.com/{client.User.Username}/ \nCurl ed file from {localVideoURL}";
})
.WithName("DownloadAndUploadVideo")
.WithOpenApi();

app.MapGet("/getvideoinfo", (string videoURL) =>
{
    return ytDlp.GetVideoInfo(videoURL);
})
.WithName("GetVideoInfo")
.WithOpenApi();

app.MapGet("/webhook/instagram", (
    [FromQuery(Name = "hub.mode")] string? mode,
    [FromQuery(Name = "hub.verify_token")] string? verifyToken,
    [FromQuery(Name = "hub.challenge")] string? challenge) =>
{
    string tokenForVerifingWebhook = DotNetEnv.Env.GetString("WEBHOOKK_VERIFY_TOKEN");
    if (mode == "subscribe" && verifyToken == tokenForVerifingWebhook)
    {
        return Results.Text(challenge ?? string.Empty, contentType: "text/plain");
    }
    return Results.StatusCode(403);
})
.WithName("InstagramWebhookVerification")
.WithOpenApi(o =>
{
    o.Description = "Instagram Webhook Verification Endpoint";
    o.Parameters[0].Description = "Webhook mode (should be 'subscribe')";
    o.Parameters[1].Description = "Verification token to validate the webhook";
    o.Parameters[2].Description = "Challenge string to return for verification";
    return o;
});

// TODO : add proper error handling and logging
// TODO : also add a way to handle multiple messages in the same webhook call or webhook mesages managment and using its other values to simplify the code
app.MapPost("/webhook/instagram", async (InstagramWebhook webhook) =>   // use HttpContext fro debuging and better understanding of the request
{
    try
    {
        if (webhook == null)
        {
            Console.WriteLine("Failed to deserialize JSON to InstagramWebhook");
            return Results.BadRequest("Invalid webhook payload");
        }

        Console.WriteLine("Deserialized InstagramWebhook Object:");
        Console.WriteLine(JsonSerializer.Serialize(webhook, new JsonSerializerOptions { WriteIndented = true }));

        int recordCount = webhook.Entry?.Count ?? 0;
        Console.WriteLine($"Record Count: {recordCount}");

        for (int i = 0; i < recordCount; i++)
        {
            if (webhook.Entry[i].Messaging != null)
            {
                InstagramUser sender = await IGagent.Clients![i].InitializeUserInfo(
                    UserID: webhook.Entry[i].Messaging![0].Sender.Id,
                    creatorOnlyPropsToget: ""
                    ) ?? throw new Exception("could not fetch the sender user to authenticate\nHINT: eather sender ID is on right or client thats fetching the data");

                Console.WriteLine($"Message from: {sender.Username} | {sender.Name}");

                if (IGagent.IsMessageFromAdmin(sender))
                {
                    Console.WriteLine($"Message from admin user: {sender.Username}");

                    if (webhook.Entry[i].Messaging![0].Message.Attachments != null &&
                        webhook.Entry[i].Messaging![0].Message.Attachments![i].Type == IGagent.Reel)
                    {
                        string fullTitle = webhook.Entry[i].Messaging![0].Message.Attachments![i].Payload.Title ?? webhook.Entry[i].Messaging![0].Timestamp.ToString();
                        string title = ytDlp.CorrectVideoNameFormat(fullTitle.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0]);
                        string videoURL = webhook.Entry[i].Messaging![0].Message.Attachments![i].Payload.Url;
                        string videoPath = ytDlp.DownloadVideo(videoURL, $"video/{title}.mp4").Replace("video/", "")
                            ?? throw new Exception("error while downloading the video for upload\nHINT probably cookie problem");

                        if (String.IsNullOrEmpty(videoPath))
                        {
                            Console.WriteLine("video is already in the queue");
                            return Results.Ok("video is already in the queue");
                        }

                        string EncodedvideoPath = Uri.EscapeDataString(videoPath.Replace("\"", ""));
                        string containerId = await IGagent.PostToAllAccounts(
                            new InstagramMediaContainer(
                                InstagramMediaType.REELS,
                                $"{IGagent.PublicBaseURL}{videoPath.Replace("video/", "")}",
                                fullTitle),  //  webhook.Entry[i].Messaging![0].Message.Text for laiter so the text with the reel will be the caption too
                            100);
                        Console.WriteLine($"Video uploaded successfully with ID: {containerId} \nlink: https://www.instagram.com/{IGagent.Clients[i].User.Username}/ \nCurl ed file from {IGagent.PublicBaseURL}{videoPath}");
                        YtDlp.DeleteVideoFile($"video/{videoPath}");
                        return Results.Ok($"Video uploaded successfully with ID: {containerId} \nlink: https://www.instagram.com/{IGagent.Clients[i].User.Username}/ \nCurl ed file from {IGagent.PublicBaseURL}{videoPath}");
                    }

                }
                else
                {
                    Console.WriteLine($"Message from non-admin user: {sender.Username}");
                    return Results.Ok($"Message from non-admin user: {sender}");
                }
            }
        }
        return Results.Ok("No messaging data found in the webhook entry.");
        }
    catch (Exception ex)
    {
        Console.WriteLine("error: " + ex.Message);
        return Results.BadRequest($"Error processing webhook: {ex.Message}");
    }
})
.WithName("InstagramWebhookData")
.WithOpenApi(o =>
{
    o.Description = "Instagram Webhook Data Endpoint";
    return o;
});

app.Run();