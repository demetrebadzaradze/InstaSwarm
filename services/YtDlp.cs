using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace InstaSwarm.services
{
    public class YtDlp
    {
        private readonly ILogger<YtDlp> logger;
        private string ytDlpPath {  get; set; }
        public string YtDlpPath {  get { return ytDlpPath; } set { ytDlpPath = value; } }
        private string cookiespath { get; set; }
        public string CookiesPath { get { return cookiespath; } set { cookiespath = value; } }

        public YtDlp(ILoggerFactory loggerFactory, string pathToYtDlp = "yt-dlp.exe", string pathOfCookies = "cookies.txt")
        {
            ytDlpPath = pathToYtDlp;
            cookiespath = pathOfCookies;
            logger = loggerFactory.CreateLogger<YtDlp>();
            logger.BeginScope($"YtDlp: ");
            logger.LogInformation($"YtDlp initialized with path: {ytDlpPath} and cookies path: {cookiespath}");
        }

        public string DownloadVideo(string videoUrl, string outputDirectory = "video/%(title)s.%(ext)s", string customCookiesPath = "cookies.txt")
        {
            logger.BeginScope($"YtDlp.DownloadVideo: ");
            logger.LogInformation($"DownloadVideo: videoUrl={videoUrl}, outputDirectory={outputDirectory}, customCookiesPath={customCookiesPath}");
            string cookiesArgument = string.IsNullOrEmpty(customCookiesPath) ? $"--cookies \"{CookiesPath}\"" : $"--cookies \"{customCookiesPath}\"";
            if (string.IsNullOrEmpty(videoUrl))
            {
                throw new ArgumentException("Video URL cannot be null or empty.", nameof(videoUrl));
            }
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = $"\"{videoUrl}\" -o {outputDirectory}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                string destination = string.Empty;

                using Process process = new Process { StartInfo = processInfo };
                StringBuilder output = new();
                StringBuilder error = new();

                process.OutputDataReceived += (sender, e) =>        //  kinda junky, will fix it later
                {
                    if (e.Data == null) return;
                    if (e.Data != null) output.AppendLine(e.Data);
                    if (e.Data.StartsWith("[Merger] Merging formats into "))    //  this kinda got deleted, but I will keep it for now
                    {
                            destination = e.Data.Substring("[Merger] Merging formats into ".Length).Trim();
                    }
                    //  [download] Destination: video / Video by ittybitinggs.mp4
                    if (e.Data.StartsWith("[download] Destination: "))
                    {
                        destination = e.Data.Substring("[download] Destination: ".Length).Trim();
                        logger.LogInformation($"Download destination: {destination}");
                    }
                    //  [download] video / Video by ittybitinggs.mp4 has already been downloaded
                    else if (e.Data.StartsWith("[download] ") && e.Data.Contains(" has already been downloaded"))
                    {
                        logger.LogInformation($"Video already downloaded: {e.Data}");
                        destination = string.Empty;
                    }
                };
                process.ErrorDataReceived += (sender, e) => { if (e.Data != null) error.AppendLine(e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    logger.LogInformation($"Video Info (JSON): {output.ToString()}");
                    return destination;
                }
                else
                {
                    logger.LogError($"Error running yt-dlp: {error.ToString()}");
                    return $"Error: {error}";
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"An error occurred: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        public string GetVideoInfo(string videoUrl, string customCookiesPath = "")
        {
            logger.BeginScope($"YtDlp.GetVideoInfo: ");
            logger.LogInformation($"GetVideoInfo: videoUrl={videoUrl}, customCookiesPath={customCookiesPath}");
            string cookiesArgument = string.IsNullOrEmpty(customCookiesPath) ? $"--cookies \"{CookiesPath}\"" : $"--cookies \"{customCookiesPath}\"";
            if (string.IsNullOrEmpty(videoUrl))
            {
                throw new ArgumentException("Video URL cannot be null or empty.", nameof(videoUrl));
            }
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    Arguments = $"--dump-json \"{videoUrl}\" {cookiesArgument}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };


                using Process process = new Process { StartInfo = processInfo };
                StringBuilder output = new();
                StringBuilder error = new();

                process.OutputDataReceived += (sender, e) => { if (e.Data != null) output.AppendLine(e.Data); };
                process.ErrorDataReceived += (sender, e) => { if (e.Data != null) error.AppendLine(e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    logger.LogInformation($"Video Info (JSON): {output.ToString()}");
                    return output.ToString();
                }
                else
                {
                    logger.LogError($"Error running yt-dlp: {error.ToString()}");
                    return $"Error: {error}";
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"An error occurred: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }
        public static bool DeleteVideoFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                else
                {
                    Console.WriteLine($"File not found: {filePath}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// methid that will get a string that will be used as a path to the video file and will return the path but with corected format like no spaces and no dots
        /// </summary>
        public string CorrectVideoNameFormat(string videoName)
        {
            if (string.IsNullOrEmpty(videoName))
            {
                throw new ArgumentException("Video name cannot be null or empty.", nameof(videoName));
            }
            // Replace spaces with underscores and remove dots
            string correctedPath = videoName.Replace(" ", "_").Replace(".", "").ToLower();

            // Remove all non-letter characters (optional: add numbers if you want)
            correctedPath = Regex.Replace(correctedPath, @"[^a-zA-Z_]", "");

            //dublicate handling for laiter
            //int counter = 1;
            //while (File.Exists(videoName))
            //{
            //    finalName = $"{safeName}_{counter}";
            //    newFullPath = Path.Combine(directory, finalName + extension);
            //    counter++;
            //}

            return correctedPath;
        }
    }
}