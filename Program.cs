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
        listenOptions.UseHttps("https-dev.pfx", "Instaswarm12345"); // Enable HTTPS
    });
});

//add proper logging
using ILoggerFactory loggerFactory =
    LoggerFactory.Create(builder =>
        builder.AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        }));
ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


string[] igTokens = DotNetEnv.Env.GetString("INSTAGRAM_USER_TOKENS")?.Split(',') ?? Array.Empty<string>();
// Initialize InstagramAgent with tokens from environment variables
InstagramAgent IGagent = new InstagramAgent(
    igTokens,
    DotNetEnv.Env.GetString("ADMIN_INSTAGRAM_USER_ID") ?? throw new InvalidOperationException("ADMIN_INSTAGRAM_USER_ID is missing in environment variables"),
    loggerFactory
    );

// Initialize YtDlp with the path from environment variables or default to "yt-dlp.exe"s
string ytDlpPath = DotNetEnv.Env.GetString("YTDLP_PATH");
if (String.IsNullOrEmpty(ytDlpPath))
{
    ytDlpPath = "yt-dlp.exe"; // Default path if not set in environment variables
}
YtDlp ytDlp = new YtDlp(loggerFactory, ytDlpPath, DotNetEnv.Env.GetString("COOKIES_PATH") ?? "cookies.txt");

app.MapGet("/", () =>
{
    logger.BeginScope("Welcome to InstaSwarm API!");
    logger.LogCritical("beggining of the API");
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
    InstagramClient client = new InstagramClient(secret, loggerFactory);
    return client.PostMedia(
        new InstagramMediaContainer(
            InstagramMediaType.REELS,
            videoURL,
            caption),100);
})
.WithName("PostVideo")
.WithOpenApi();

app.MapGet("/download-and-upload", async (string videoURL, string caption) =>
{
    string secret = DotNetEnv.Env.GetString("INSTAGRAM_USER_TOKEN");
    string baseurl = DotNetEnv.Env.GetString("PUBLIC_BASE_URL");
    string videoPath = ytDlp.DownloadVideo(videoURL).Replace("video/", "");
    string EncodedvideoPath = Uri.EscapeDataString(videoPath.Replace("\"", ""));
    InstagramClient client = new InstagramClient(secret, loggerFactory);
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
            logger.LogWarning("Invalid or empty webhook payload received");
            return Results.BadRequest(new { error = "Invalid webhook payload" });
        }

        logger.LogInformation("Deserialized InstagramWebhook Object:");
        logger.LogInformation(JsonSerializer.Serialize(webhook, new JsonSerializerOptions { WriteIndented = true }));

        int recordCount = webhook.Entry?.Count ?? 0;
        logger.LogInformation($"Record Count: {recordCount}");

        return Results.Ok(await IGagent.ProcessWebhook(webhook, ytDlp));
    }
    catch (Exception ex)
    {
        logger.LogError("error: " + ex.Message);
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
