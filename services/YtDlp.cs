using System.Diagnostics;
using System.Text;

namespace InstaSwarm.services
{
    public class YtDlp
    {
        private string ytDlpPath {  get; set; }
        public string YtDlpPath {  get { return ytDlpPath; } set { ytDlpPath = value; } }

        public YtDlp(string pathToYtDlp = "yt-dlp.exe")
        {
            ytDlpPath = pathToYtDlp;
        }

        public string GetVideoInfo(string videoUrl, string cookiesPath = "")
        {
            string cookiesArgument = string.IsNullOrEmpty(cookiesPath) ? "" : $"--cookies \"{cookiesPath}\"";
            if (string.IsNullOrEmpty(videoUrl))
            {
                throw new ArgumentException("Video URL cannot be null or empty.", nameof(videoUrl));
            }
            try
            {
                // Configure the process to run yt-dlp
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

                // Capture output and errors
                process.OutputDataReceived += (sender, e) => { if (e.Data != null) output.AppendLine(e.Data); };
                process.ErrorDataReceived += (sender, e) => { if (e.Data != null) error.AppendLine(e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    // Output contains the JSON with video info
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
    }
}