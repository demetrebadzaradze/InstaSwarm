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

        //   sudo yt-dlp https://www.instagram.com/reel/DLU_ks_CjSy/  -o "opt/%(title)s.%(ext)s"
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

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null) return;
                    if (e.Data != null) output.AppendLine(e.Data);
                    if (e.Data.StartsWith("[Merger] Merging formats into ")) 
                    {
                        destination = e.Data.Substring("[Merger] Merging formats into ".Length).Trim();
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
    }
}