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
        public async Task<InstagramUser> InitializeUserInfo(string? token = null, string? UserID = "me", string? creatorOnlyPropsToget = ",user_id,account_type,profile_picture_url,followers_count,follows_count,media_count")
        {
            _userKey = token ?? _userKey;
            string url = $"https://{IG_API_baseUrl}/{IG_API_Version}/{UserID}?fields=id,username{creatorOnlyPropsToget}&access_token={_userKey}";
            try
            {
                HttpResponseMessage ResponseMessage = await httpClient.GetAsync(url);
                if (ResponseMessage.IsSuccessStatusCode)
                {
                    string responseBody = await ResponseMessage.Content.ReadAsStringAsync();
                    JsonDocument jsonDocument = JsonDocument.Parse(responseBody);

                    InstagramUser fetchedUser = JsonSerializer.Deserialize<InstagramUser>(jsonDocument) ?? InstagramUser.Empty;

                    if (UserID != "me" || UserID != User.UserID)
                    {
                        return fetchedUser;
                    }

                    User = fetchedUser;
                    return User;
                }
                else
                {
                    Console.WriteLine($"Error: {ResponseMessage.StatusCode}" + "\nHINT: Probably token is invalid");
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
                    $"https://{IG_API_baseUrl}/{IG_API_Version}/{User.ID}/content_publishing_limit?access_token={_userKey}");
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
        /// <summary>
        /// returns id of the media container that was published
        /// </summary>
        public async Task<string> PublishMediaContainer()
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
                    string publishedId = jsonDocument.RootElement.GetProperty("id").GetString()!;
                    Console.WriteLine(jsonDocument.RootElement.ToString());
                    return publishedId;
                }
                else
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error: {response.StatusCode}, {responseBody}");
                    return string.Empty; 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return string.Empty;
            }
        }
        public async Task<string> PostMedia(InstagramMediaContainer iGMediaContainer, int delayBeforePublishingInSeconds = 15)
        {
            await CreateMediaContainer(iGMediaContainer);
            await Task.Delay(1 * 1000 * delayBeforePublishingInSeconds); // Wait for 15 seconds before publishing
            string mediaID = string.Empty;
            try
            {
                mediaID = await PublishMediaContainer();
                return mediaID;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while waiting: {ex.Message}");
                Console.WriteLine("trying aggain with another 15 sec delay");
                await Task.Delay(1 * 1000 * delayBeforePublishingInSeconds * 2); // Wait for 15 seconds before publishing
                mediaID = await PublishMediaContainer();
                return mediaID;
            }
        }
        // to implement later:
        // GET https://graph.instagram.com/refresh_access_token
        //  ?grant_type=ig_refresh_token
        //  &access_token=<LONG_LIVED_ACCESS_TOKENS>
        // refresh access token 
        // folow this link for more info: https://developers.facebook.com/docs/instagram-platform/reference/refresh_access_token/
        public async Task<string> RefreshAccessToken(string? longLivedAccessToken = "")
        {
            if (string.IsNullOrEmpty(longLivedAccessToken))
            {
                longLivedAccessToken = _userKey;
                Console.WriteLine("Long-lived access token is required.");
            }

            string url = $"https://{IG_API_baseUrl}/refresh_access_token?grant_type=ig_refresh_token&access_token={longLivedAccessToken}";
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    JsonDocument jsonDocument = JsonDocument.Parse(responseBody);
                    Console.WriteLine("Access token refreshed successfully.");
                    Console.WriteLine(jsonDocument.RootElement.ToString());
                    _userKey = jsonDocument.RootElement.GetProperty("access_token").GetString()!;
                    Environment.SetEnvironmentVariable("INSTAGRAM_USER_TOKEN", _userKey);
                    DotNetEnv.Env.Load();
                    Console.WriteLine(DotNetEnv.Env.GetString("INSTAGRAM_USER_TOKEN"));
                    return jsonDocument.RootElement.GetProperty("expires_in").GetInt32().ToString();
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode}");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
