using Microsoft.AspNetCore.Mvc.Formatters;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text.Json;

namespace InstaSwarm.services
{
    public class InstagramClient
    {
        private readonly double charactersToShowWhileLogging = 0.3;
        private readonly ILogger<InstagramClient> logger;
        [Required]
        private string _userKey { get; set; }
        private HttpClient httpClient = new HttpClient();
        public static string IG_API_baseUrl = "graph.instagram.com";
        public static string IG_API_Version = "v23.0";
        public InstagramMediaContainer LatestInstagramMediaContainer { get; set; } = InstagramMediaContainer.Empty;
        public InstagramUser User { get; set; } = InstagramUser.Empty;

        public InstagramClient(string userKey, ILoggerFactory loggerFactory)
        {
            _userKey = userKey;
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _userKey);
            logger = loggerFactory.CreateLogger<InstagramClient>();
            logger.BeginScope($"InstagramClient: ");
            logger.LogInformation($"InstagramClient initialized with user key: {_userKey.Substring(0, (int)(_userKey.Length * charactersToShowWhileLogging))}");
            User = InitializeUserInfo().GetAwaiter().GetResult();
        }
        public async Task<InstagramUser> InitializeUserInfo(
            string? token = null,
            string? UserID = "me",
            string? creatorOnlyPropsToget = ",user_id,account_type,profile_picture_url,followers_count,follows_count,media_count")
        {
            logger.BeginScope("InstagramClient.InitializeUserInfo: ");
            logger.LogInformation($"Fetching user info for UserID: {UserID} \nwith token: {token?.Substring(0, (int)(token.Length * charactersToShowWhileLogging)) ?? "null"} \nwith propertys {creatorOnlyPropsToget} ");

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
                    logger.LogError($"Failed to fetch user info: {ResponseMessage.StatusCode} || HINT: Probably token is invalid");
                    return InstagramUser.Error;
                }

            }
            catch (Exception ex)
            {
                logger.LogError($"An error occurred: {ex.Message}");
                return InstagramUser.Error;
            }
        }
        public async Task<int> GetAvalableContentPublishesCount()
        {
            logger.BeginScope("InstagramClient.GetAvalableContentPublishesCount: ");
            int daylyPublishLimit = 100;
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(
                    $"https://{IG_API_baseUrl}/{IG_API_Version}/{User.ID}/content_publishing_limit?access_token={_userKey}");
                if (response.IsSuccessStatusCode)
                {
                    JsonDocument jsonresponse = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    int usedPublishesCount = jsonresponse.RootElement.GetProperty("data")[0].GetProperty("quota_usage").GetInt32();
                    logger.LogInformation($"Used publishes count: {usedPublishesCount}");
                    return daylyPublishLimit - usedPublishesCount;
                }
            }
            catch (HttpRequestException ex)
            {
                logger.LogError($"HttpRequestException: {ex.Message}");
                return -1;
            }
            catch (Exception ex)
            {
                logger.LogError($"An error occurred: {ex.Message}");
                return -1;
            }
            return -1; // Default return value in case of failure
        }
        public async Task<InstagramMediaContainer> CreateMediaContainer(InstagramMediaContainer iGMediaContainer)
        {
            logger.BeginScope("InstagramClient.CreateMediaContainer: ");
            logger.LogInformation($"Creating media container with type: {iGMediaContainer.MediaType}, " +
                $"\nURL: {iGMediaContainer.MediaUrl}, " +
                $"\nCaption: {iGMediaContainer.Caption?.Substring(0, (int)(iGMediaContainer.Caption.Length * charactersToShowWhileLogging)) ?? "null"}");

            string url = $"https://{IG_API_baseUrl}/{IG_API_Version}/{User.UserID}/media";

            try
            {
                HttpResponseMessage response = await httpClient.PostAsync(url, iGMediaContainer.GetJson());
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    JsonDocument jsonDocument = JsonDocument.Parse(responseBody);

                    try
                    {
                        string containerID = jsonDocument.RootElement.GetProperty("id").GetString()!;
                        iGMediaContainer.Id = containerID;
                        logger.LogInformation($"Media container created successfully with ID: {containerID}");
                        LatestInstagramMediaContainer = iGMediaContainer;
                        return iGMediaContainer;
                    }
                    catch (KeyNotFoundException)
                    {
                        logger.LogError("Container ID not found in the response.");
                        return InstagramMediaContainer.Empty;
                    }
                }
                else
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    logger.LogError($"Error creating media container: {response.StatusCode}, {responseBody}");      // could add response.Content.ToString response.Headers
                    return InstagramMediaContainer.Empty;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"An error occurred: {ex.Message}");
                return InstagramMediaContainer.Empty;
            }
        }
        /// <summary>
        /// returns id of the media container that was published
        /// </summary>
        public async Task<string> PublishMediaContainer()
        {
            logger.BeginScope("InstagramClient.PublishMediaContainer: ");
            logger.LogInformation($"Publishing media container with ID: {LatestInstagramMediaContainer.Id} for User: {User.Username}");

            string url = $"https://{IG_API_baseUrl}/{IG_API_Version}/{User.UserID}/media_publish";

            try
            {
                HttpResponseMessage response = await httpClient.PostAsync(url, InstagramMediaContainer.ContainerPublishingIDAsJson(LatestInstagramMediaContainer.Id));
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    JsonDocument jsonDocument = JsonDocument.Parse(responseBody);
                    string publishedId = jsonDocument.RootElement.GetProperty("id").GetString()!;
                    logger.LogInformation($"Published media container successfully with ID: {publishedId}");
                    return publishedId;
                }
                else
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    logger.LogError($"Error publishing media container: {response.StatusCode}, {responseBody}");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"An error occurred : {ex.Message}");
                return string.Empty;
            }
        }
        public async Task<string> PostMedia(InstagramMediaContainer iGMediaContainer, int delayBeforePublishingInSeconds = 15)
        {
            logger.BeginScope("InstagramClient.PostMedia: ");
            logger.LogInformation($"Posting media with type: {iGMediaContainer.MediaType}, " +
                $"\nURL: {iGMediaContainer.MediaUrl}, " +
                $"\nCaption: {iGMediaContainer.Caption?.Substring(0, (int)(iGMediaContainer.Caption.Length * charactersToShowWhileLogging)) ?? "null"}");
            await CreateMediaContainer(iGMediaContainer);
            await Task.Delay(1 * 1000 * delayBeforePublishingInSeconds); 
            string mediaID = string.Empty;
            try
            {
                mediaID = await PublishMediaContainer();
                return mediaID;
            }
            catch (Exception ex)
            {
                logger.LogError($"An error occurred while waiting: {ex.Message}");
                logger.LogInformation($"Trying again with another {delayBeforePublishingInSeconds} sec delay");
                await Task.Delay(1 * 1000 * delayBeforePublishingInSeconds * 2); 
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
            
            logger.BeginScope("InstagramClient.RefreshAccessToken: ");
            logger.LogInformation($"Refreshing access token for User: {User.Username} \n" +
                $"with token: {longLivedAccessToken?.Substring(0, (int)(longLivedAccessToken.Length * charactersToShowWhileLogging)) ?? "null"}");

            if (string.IsNullOrEmpty(longLivedAccessToken))
            {
                longLivedAccessToken = _userKey;
                logger.LogWarning("Long-lived access token is not provided, using the current user key.");
            }

            string url = $"https://{IG_API_baseUrl}/refresh_access_token?grant_type=ig_refresh_token&access_token={longLivedAccessToken}";
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    JsonDocument jsonDocument = JsonDocument.Parse(responseBody);
                    logger.LogInformation($"Response from refresh access token: {jsonDocument.RootElement.GetProperty("access_token").GetString()!}");
                    _userKey = jsonDocument.RootElement.GetProperty("access_token").GetString()!;
                    Environment.SetEnvironmentVariable("INSTAGRAM_USER_TOKEN", _userKey);
                    DotNetEnv.Env.Load();
                    logger.LogInformation($"New access token set: {DotNetEnv.Env.GetString("INSTAGRAM_USER_TOKEN")}");
                    return jsonDocument.RootElement.GetProperty("expires_in").GetInt32().ToString();
                }
                else
                {
                    logger.LogError($"Failed to refresh access token: {response.StatusCode}");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"An error occurred : {ex.Message}");
                return string.Empty;
            }
        }
    }
}
