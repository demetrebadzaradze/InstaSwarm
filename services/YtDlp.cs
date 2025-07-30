using System.Diagnostics;
using System.Text;

namespace InstaSwarm.services
{
    public class YtDlp
    {
        private string ytDlpPath {  get; set; }
        public string YtDlpPath {  get { return ytDlpPath; } set { ytDlpPath = value; } }
        private string cookiespath { get; set; }
        public string CookiesPath { get { return cookiespath; } set { cookiespath = value; } }

        public YtDlp(string pathToYtDlp = "yt-dlp.exe", string pathOfCookies = "cookies.txt")
        {
            ytDlpPath = pathToYtDlp;
            cookiespath = pathOfCookies;
        }

        public string DownloadVideo(string videoUrl, string outputDirectory = "video/%(title)s.%(ext)s", string customCookiesPath = "cookies.txt")
        {
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
                        Console.WriteLine(e.Data);
                        destination = e.Data.Substring("[download] Destination: ".Length).Trim();
                        Console.WriteLine(destination);
                    }
                    //  [download] video / Video by ittybitinggs.mp4 has already been downloaded
                    else if (e.Data.StartsWith("[download] ") && e.Data.Contains(" has already been downloaded"))
                    {
                        Console.WriteLine(e.Data);
                        destination = e.Data.Substring("[download] ".Length, e.Data.IndexOf(" has already been downloaded") - "[download] ".Length).Trim();
                        Console.WriteLine(destination);
                    }
                };
                process.ErrorDataReceived += (sender, e) => { if (e.Data != null) error.AppendLine(e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine("Video Info (JSON):");
                    Console.WriteLine(output.ToString());
                    //return output.ToString();
                    return destination; // Return the destination path of the downloaded video
                }
                else
                {
                    Console.WriteLine("Error running yt-dlp:");
                    Console.WriteLine(error.ToString());
                    return $"Error: {error}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        public string GetVideoInfo(string videoUrl, string customCookiesPath = "")
        {
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
                    Console.WriteLine("Video Info (JSON):");
                    Console.WriteLine(output.ToString());
                    return output.ToString();
                }
                else
                {
                    Console.WriteLine("Error running yt-dlp:");
                    Console.WriteLine(error.ToString());
                    return $"Error: {error}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
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
        // methid that will get a string that will be used as a path to the video file and will return the path but with corected format like no spaces and no dots
        public string CorrectVideoNameFormat(string videoPath)
        {
            if (string.IsNullOrEmpty(videoPath))
            {
                throw new ArgumentException("Video path cannot be null or empty.", nameof(videoPath));
            }
            // Replace spaces with underscores and remove dots
            string correctedPath = videoPath.Replace(" ", "_").Replace(".", "");
            return correctedPath;
        }
    }
}