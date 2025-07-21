using System.Text.Json.Serialization;

namespace InstaSwarm.services
{
    public class InstagramUser
    {
        [JsonPropertyName("id")]
        public string ID { get; set; }
        [JsonPropertyName("user_id")]
        public string? UserID { get; set; }
        [JsonPropertyName("username")]
        public string Username { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("profile_picture_url")]
        public string? ProfilePictureUrl { get; set; }
        [JsonPropertyName("account_type")]
        public string? AccountType { get; set; }
        [JsonPropertyName("followers_count")]
        public int? FollowersCount { get; set; }
        [JsonPropertyName("follows_count")]
        public int? FollowsCount { get; set; }
        [JsonPropertyName("media_count")]
        public int? MediaCount { get; set; }

        public static readonly InstagramUser Error = new InstagramUser()
        {
            ID = "Error",
            UserID = "Error",
            Username = "Error",
            Name = "Error",
            ProfilePictureUrl = "Error",
            AccountType = "Error",
            FollowersCount = 0,
            FollowsCount = 0,
            MediaCount = 0,
        };
        public static readonly InstagramUser Empty = new InstagramUser()
        {
            ID = "",
            UserID = "",
            Username = "",
            Name = "",
            ProfilePictureUrl = "",
            AccountType = "",
            FollowersCount = 0,
            FollowsCount = 0,
            MediaCount = 0,
        };
        public void ListPropertiesInTerminal()
        {
            Console.WriteLine($"id ID: {ID}");
            Console.WriteLine($"user_id UserID: {UserID}");
            Console.WriteLine($"username Username: {Username}");
            Console.WriteLine($"name Name: {Name}");
            Console.WriteLine($"profile_picture_url ProfilePictureUrl: {ProfilePictureUrl}");
            Console.WriteLine($"account_type AccountType: {AccountType}");
            Console.WriteLine($"followers_count FollowersCount: {FollowersCount}");
            Console.WriteLine($"follows_count FollowsCount: {FollowsCount}");
            Console.WriteLine($"media_count MediaCount: {MediaCount}");

        }
    }
}