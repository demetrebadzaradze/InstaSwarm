using InstaSwarm.services;
using System.Runtime.Intrinsics.Arm;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

DotNetEnv.Env.Load();   
string ytDlpPath = DotNetEnv.Env.GetString("YTDLP_PATH") ?? @"yt-dlp.exe";
YtDlp ytDlp = new YtDlp(ytDlpPath, DotNetEnv.Env.GetString("COOKIES_PATH") ?? "cookies.txt");

app.MapGet("/", () => {
    InstagramClient client = new InstagramClient(DotNetEnv.Env.GetString("INSTAGRAM_USER_TOKEN"));
    return client.PostMedia(new InstagramMediaContainer(InstagramMediaType.Image, "https://urlme.me/success/typed_a_url/made_a_meme.jpg?source=www", "test https://urlme.me/success/typed_a_url/made_a_meme.jpg?source=www"));
})      
.WithName("GetUserInfo")
.WithOpenApi();

app.MapGet("/dowloadvideo", (string reelUrl) =>
{
    return ytDlp.DownloadVideo(reelUrl);
})
.WithName("DownloadReel")
.WithOpenApi();

app.MapGet("/getvideoinfo", async (string videoURL) =>
{
    return ytDlp.GetVideoInfo(videoURL);
})
.WithName("GetVideoInfo")
.WithOpenApi();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
