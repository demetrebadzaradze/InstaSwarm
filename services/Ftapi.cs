using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InstaSwarm.services
{
    public class Ftapi
    {
        private static readonly HttpClient _httpClient = new();

        /// <summary>
        ///     translate text to english using ftapi, witch is an external free translation api, based on google translate.
        /// </summary>
        /// <param name="Text">Text to translate</param>
        /// <returns>Translated text to desired language, if that fails it returnes same text</returns>
        {
            InitializedHttpClient();
            HttpResponseMessage responce = await _httpClient.GetAsync($"translate?dl=en&text={Uri.EscapeDataString(Text)}");
            string responceAsString = await responce.Content.ReadAsStringAsync();

            FtapiResponce ftapiResponce = JsonSerializer.Deserialize<FtapiResponce>(responceAsString) ?? new FtapiResponce();

            return ftapiResponce.DestinationText  ?? "";
        }
        private static void InitializedHttpClient()
        {
            _httpClient.BaseAddress = new Uri("https://ftapi.pythonanywhere.com/");
            _httpClient.Timeout = TimeSpan.FromSeconds(3);
        }
    }

    public class FtapiResponce
    {
        [JsonPropertyName("source-language")]
        public string? SourceLanguage { get; set; }

        [JsonPropertyName("source-text")]
        public string? SourceText { get; set; }

        [JsonPropertyName("destination-language")]
        public string? DestinationLanguage { get; set; }

        [JsonPropertyName("destination-text")]
        public string? DestinationText { get; set; }

        [JsonPropertyName("pronunciation")]
        public Pronunciation? Pronunciation { get; set; }

        [JsonPropertyName("translations")]
        public Translations? Translations { get; set; }

        [JsonPropertyName("definitions")]
        public object? Definitions { get; set; }

        [JsonPropertyName("see-also")]
        public object? SeeAlso { get; set; }
    }

    public class Pronunciation
    {
        [JsonPropertyName("source-text-phonetic")]
        public string? SourceTextPhonetic { get; set; }

        [JsonPropertyName("source-text-audio")]
        public string? SourceTextAudio { get; set; }

        [JsonPropertyName("destination-text-audio")]
        public string? DestinationTextAudio { get; set; }
    }

    public class Translations
    {
        [JsonPropertyName("all-translations")]
        public object? AllTranslations { get; set; }

        [JsonPropertyName("possible-translations")]
        public object? PossibleTranslations { get; set; }

        [JsonPropertyName("possible-mistakes")]
        public object? PossibleMistakes { get; set; }
    }
}