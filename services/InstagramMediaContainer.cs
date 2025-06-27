using Microsoft.AspNetCore.Mvc.Formatters;
using System.Text.Json;
using System.Text;

namespace InstaSwarm.services
{
    public enum InstagramMediaType
    {
        Image,
        CAROUSEL,
        REELS,
        STORIES,
        VIDEO
    };
    public class InstagramMediaContainer
    {
        public InstagramMediaType MediaType { get; set; }
        public string MediaUrl { get; set; }
        public string Caption { get; set; }
        public string Id { get; set; } = string.Empty;

        public static readonly InstagramMediaContainer Empty = new InstagramMediaContainer()
        {
            MediaType = InstagramMediaType.Image,
            MediaUrl = string.Empty,
            Caption = string.Empty,
            Id = string.Empty
        };
        public StringContent GetJson()
        {
            var jsonContent = DetermineJsonContent();
            return new StringContent(jsonContent, Encoding.UTF8, "application/json");
        }
        public static StringContent ContainerPublishingIDAsJson(string containerID)
        {
            var jsonContent = JsonSerializer.Serialize(new
            {
                creation_id = containerID
            });
            return new StringContent(jsonContent, Encoding.UTF8, "application/json");
        }
        public InstagramMediaContainer()
        {
            MediaType = InstagramMediaType.Image;
            MediaUrl = string.Empty;
            Caption = string.Empty;
        }
        public InstagramMediaContainer(InstagramMediaType mediaType, string mediaUrl, string? caption)
        {
            MediaType = mediaType;
            MediaUrl = mediaUrl;
            Caption = caption ?? "";
        }
        private string DetermineJsonContent()
        {
            if (MediaType == InstagramMediaType.CAROUSEL)   // carousel is not implemented yet
            {
                return JsonSerializer.Serialize(new
                {
                    media_type = MediaType.ToString().ToUpper(),
                    children = "", // to implement later
                    caption = Caption
                });
            }
            else if (MediaType == InstagramMediaType.REELS)
            {
                return JsonSerializer.Serialize(new
                {
                    media_type = MediaType.ToString().ToUpper(),
                    video_url = MediaUrl,
                    caption = Caption
                });
            }
            else if (MediaType == InstagramMediaType.STORIES) // Stories are not implemented yet
            {
                return JsonSerializer.Serialize(new
                {
                    media_type = MediaType.ToString().ToUpper(),
                    video_url = MediaUrl,
                    caption = Caption
                });
            }
            else if (MediaType == InstagramMediaType.VIDEO) // Video is not implemented yet
            {
                return JsonSerializer.Serialize(new
                {
                    media_type = MediaType.ToString().ToUpper(),
                    video_url = MediaUrl,
                    caption = Caption
                });
            }
            else // Default to Image
            {
                return JsonSerializer.Serialize(new // needs testing
                {
                    image_url = MediaUrl,
                    caption = Caption
                });
            }
        }
    }
}
