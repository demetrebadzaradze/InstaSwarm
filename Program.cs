using InstaSwarm.services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Runtime.Intrinsics.Arm;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

var builder = WebApplication.CreateBuilder(args);

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
    string EncodedvideoPath = Uri.EscapeDataString(videoPath.Replace("\"",""));
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
    if (mode == "subscribe" && verifyToken == "VERIFY_TOKEN")
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

app.MapPost("/webhook/instagram", async (HttpRequest request) =>
{
    try
    {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();
        var payload = JsonSerializer.Deserialize<InstagramWebhookPayload>(body);

        Console.WriteLine($"webhook secived\n{payload}");

        return Results.Ok("Webhook received");
    }
    catch (Exception ex)
    {
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