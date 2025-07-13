using InstaSwarm.services;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.Intrinsics.Arm;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080); // HTTP
    options.ListenAnyIP(8081, listenOptions =>
    {
        listenOptions.UseHttps("https-dev.pfx", "Demetre888"); // Enable HTTPS
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

DotNetEnv.Env.Load();
string ytDlpPath = DotNetEnv.Env.GetString("YTDLP_PATH");
if (String.IsNullOrEmpty(ytDlpPath))
{
       ytDlpPath = "yt-dlp.exe"; // Default path if not set in environment variables
}
YtDlp ytDlp = new YtDlp(ytDlpPath, DotNetEnv.Env.GetString("COOKIES_PATH") ?? "cookies.txt");

app.MapGet("/", () =>
{
    string secret = DotNetEnv.Env.GetString("INSTAGRAM_USER_TOKEN");
    InstagramClient client = new InstagramClient(secret);
    return client.PostMedia(
        new InstagramMediaContainer(
            InstagramMediaType.Image,
            "https://urlme.me/success/typed_a_url/made_a_meme.jpg?source=www",
            "test https://urlme.me/success/typed_a_url/made_a_meme.jpg?source=www"));
})
.WithName("GetUserInfo")
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
    string baseurl = "https://tg3w3p.taile6d42d.ts.net/";
    string videoPath = ytDlp.DownloadVideo(videoURL).Replace("video/", "");
    string EncodedvideoPath = Uri.EscapeDataString(videoPath);
    InstagramClient client = new InstagramClient(secret);
    return await client.PostMedia(
        new InstagramMediaContainer(
            InstagramMediaType.REELS,
            $"{baseurl}{EncodedvideoPath}",
            caption),100);
})
.WithName("DownloadAndUploadVideo")
.WithOpenApi();

app.MapGet("/getvideoinfo", (string videoURL) =>
{
    return ytDlp.GetVideoInfo(videoURL);
})
.WithName("GetVideoInfo")
.WithOpenApi();

app.MapGet("webhook/test", async () =>
{
    //var content = await context.ReadAsStringAsync();
    //var token = context.Query["hub.verify_token"];
    //var challenge = context.Query["hub.challenge"];
    await Task.Delay(10); // Simulate some processing delay   
    Console.WriteLine($"Received content: ");
})
.WithName("webhook_test")
.WithOpenApi();

//app.MapPost("/webhook/instagram", () =>
//{
//try
//{
//    using var reader = new StreamReader(HttpRequest.Body);
//    string json = await reader.ReadToEndAsync();
//    var payload = JsonSerializer.Deserialize<InstagramWebhookPayload>(json);

//    if (payload?.Object == "instagram")
//    {
//        foreach (var entry in payload.Entry)
//        {
//            foreach (var messaging in entry.Messaging)
//            {
//                string senderId = messaging.Sender.Id;
//                string messageId = messaging.Message.Mid;
//                string messageText = messaging.Message.Text;

//                if (!string.IsNullOrEmpty(messageText))
//                {
//                    _logger.LogInformation("Received DM from {SenderId}: {MessageText}", senderId, messageText);
//                    await _instagramAgent.ProcessVideoLinkAsync(senderId, messageText, messageId);
//                }
//            }
//        }
//        return Ok("EVENT_RECEIVED");
//    }

//    return NotFound();
//}
//catch (Exception ex)
//{
//    _logger.LogError(ex, "Error processing webhook payload.");
//    return StatusCode(500);
//}
//})
//.WithName("webhook_instagram")
//.WithOpenApi();

app.Run();