using Microsoft.AspNetCore.Mvc.Formatters;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text.Json;

namespace InstaSwarm.services
{
    public class InstagramClient
    {
        [Required]
        private string _userKey { get; set; }
        private HttpClient httpClient = new HttpClient();
        public static string IG_API_baseUrl = "graph.instagram.com";
        public static string IG_API_Version = "v23.0";
        public InstagramMediaContainer LatestInstagramMediaContainer { get; set; } = InstagramMediaContainer.Empty;
        public InstagramUser User { get; set; } = InstagramUser.Empty;

        public InstagramClient(string userKey)
        {
            _userKey = userKey;
            User = InitializeUserInfo().GetAwaiter().GetResult();
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userKey);
        }
        public async Task<InstagramUser> InitializeUserInfo(string? token = null)
        {
            _userKey = token ?? _userKey;
            string url = $"https://{IG_API_baseUrl}/{IG_API_Version}/me?fields=user_id,username,id,account_type,profile_picture_url,followers_count,follows_count,media_count&access_token={_userKey}";
            try
            {
                HttpResponseMessage ResponseMessage = await httpClient.GetAsync(url);
                if (ResponseMessage.IsSuccessStatusCode)
                {
                    string responseBody = await ResponseMessage.Content.ReadAsStringAsync();
                    JsonDocument jsonDocument = JsonDocument.Parse(responseBody);

                    User = JsonSerializer.Deserialize<InstagramUser>(jsonDocument) ?? InstagramUser.Empty;

                    return User;
                }
                else
                {
                    Console.WriteLine($"Error: {ResponseMessage.StatusCode}");
                    return InstagramUser.Error;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return InstagramUser.Error;
            }
        }
        public async Task<int> GetAvalableContentPublishesCount()
        {
            int daylyPublishLimit = 100;
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(
                    $"https://{IG_API_baseUrl}/{IG_API_Version}/{User.UserID}/content_publishing_limit?access_token={_userKey}");
                if (response.IsSuccessStatusCode)
                {
                    JsonDocument jsonresponse = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    int usedPublishesCount = jsonresponse.RootElement.GetProperty("data")[0].GetProperty("quota_usage").GetInt32();
                    return daylyPublishLimit - usedPublishesCount;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"An error occurred while fetching content publishing limit: {ex.Message}");
                return -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                return -1;
            }
            return -1; // Default return value in case of failure
        }
        public async Task<InstagramMediaContainer> CreateMediaContainer(InstagramMediaContainer iGMediaContainer)
        {
            string url = $"https://{IG_API_baseUrl}/{IG_API_Version}/{User.UserID}/media";

            try
            {
                HttpResponseMessage response = await httpClient.PostAsync(url, iGMediaContainer.GetJson());
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    JsonDocument jsonDocument = JsonDocument.Parse(responseBody);
                    Console.WriteLine("Media container created successfully.");
                    Console.WriteLine(jsonDocument.RootElement.ToString());

                    try
                    {
                        string containerID = jsonDocument.RootElement.GetProperty("id").GetString()!;
                        iGMediaContainer.Id = containerID;
                        LatestInstagramMediaContainer = iGMediaContainer;
                        return iGMediaContainer;
                    }
                    catch (KeyNotFoundException)
                    {
                        Console.WriteLine("Container ID not found in the response.");
                        return InstagramMediaContainer.Empty;
                    }
                }
                else
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error: {response.StatusCode}, {responseBody}");
                    Console.WriteLine($"Error: {response.StatusCode}, {response.Content.ToString}, {response.Headers}");
                    return InstagramMediaContainer.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return InstagramMediaContainer.Empty;
            }
        }
        public async Task PublishMediaContainer()
        {
            string url = $"https://{IG_API_baseUrl}/{IG_API_Version}/{User.UserID}/media_publish";

            try
            {
                HttpResponseMessage response = await httpClient.PostAsync(url, InstagramMediaContainer.ContainerPublishingIDAsJson(LatestInstagramMediaContainer.Id));
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    JsonDocument jsonDocument = JsonDocument.Parse(responseBody);
                    Console.WriteLine("Media container published successfully.");
                    Console.WriteLine(jsonDocument.RootElement.ToString());
                }
                else
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error: {response.StatusCode}, {responseBody}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
        public async Task<string> PostMedia(InstagramMediaContainer iGMediaContainer, int delayBeforePublishingInSeconds = 15)
        {
            await CreateMediaContainer(iGMediaContainer);
            await Task.Delay(1 * 1000 * delayBeforePublishingInSeconds); // Wait for 15 seconds before publishing
            try
            {
                await PublishMediaContainer();
                return LatestInstagramMediaContainer.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while waiting: {ex.Message}");
                Console.WriteLine("trying aggain with another 15 sec delay");
                await Task.Delay(1 * 1000 * delayBeforePublishingInSeconds); // Wait for 15 seconds before publishing
                await PublishMediaContainer();
                return LatestInstagramMediaContainer.Id;
            }
        }
    }
}
