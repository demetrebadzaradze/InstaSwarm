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
        /// <param name="targetLanguage"">Target shorgen language name</param>
        /// <returns>Translated text to desired language, if that fails it returnes same text</returns>
        public static async Task<string> TranslateText(string Text, FtapiLanguages targetLanguage = FtapiLanguages.en)
        {
            InitializedHttpClient();
            HttpResponseMessage responce = await _httpClient.GetAsync($"translate?dl={targetLanguage}&text={Uri.EscapeDataString(Text)}");
            string responceAsString = await responce.Content.ReadAsStringAsync();

            FtapiResponce ftapiResponce = JsonSerializer.Deserialize<FtapiResponce>(responceAsString) ?? new FtapiResponce();

            return ftapiResponce.DestinationText ?? "";
        }
        private static void InitializedHttpClient()
        {
            _httpClient.BaseAddress = new Uri("https://ftapi.pythonanywhere.com/");  // not an public api just for demos, host this on your own server if you need it.
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }
        /// <summary>
        /// Enum representing supported languages for the FTAPI, with short codes as values and full names in comments.
        /// </summary>
        public enum FtapiLanguages
        {
            /// <summary>Afrikaans</summary>
            af,
            /// <summary>Albanian</summary>
            sq,
            /// <summary>Amharic</summary>
            am,
            /// <summary>Arabic</summary>
            ar,
            /// <summary>Armenian</summary>
            hy,
            /// <summary>Azerbaijani</summary>
            az,
            /// <summary>Basque</summary>
            eu,
            /// <summary>Belarusian</summary>
            be,
            /// <summary>Bengali</summary>
            bn,
            /// <summary>Bosnian</summary>
            bs,
            /// <summary>Bulgarian</summary>
            bg,
            /// <summary>Catalan</summary>
            ca,
            /// <summary>Cebuano</summary>
            ceb,
            /// <summary>Chichewa</summary>
            ny,
            /// <summary>Chinese (Simplified)</summary>
            zh_cn,
            /// <summary>Chinese (Traditional)</summary>
            zh_tw,
            /// <summary>Corsican</summary>
            co,
            /// <summary>Croatian</summary>
            hr,
            /// <summary>Czech</summary>
            cs,
            /// <summary>Danish</summary>
            da,
            /// <summary>Dutch</summary>
            nl,
            /// <summary>English</summary>
            en,
            /// <summary>Esperanto</summary>
            eo,
            /// <summary>Estonian</summary>
            et,
            /// <summary>Filipino</summary>
            tl,
            /// <summary>Finnish</summary>
            fi,
            /// <summary>French</summary>
            fr,
            /// <summary>Frisian</summary>
            fy,
            /// <summary>Galician</summary>
            gl,
            /// <summary>Georgian</summary>
            ka,
            /// <summary>German</summary>
            de,
            /// <summary>Greek</summary>
            el,
            /// <summary>Gujarati</summary>
            gu,
            /// <summary>Haitian Creole</summary>
            ht,
            /// <summary>Hausa</summary>
            ha,
            /// <summary>Hawaiian</summary>
            haw,
            /// <summary>Hebrew</summary>
            he, // Note: 'iw' and 'he' both map to Hebrew; using 'he' as primary
            /// <summary>Hindi</summary>
            hi,
            /// <summary>Hmong</summary>
            hmn,
            /// <summary>Hungarian</summary>
            hu,
            /// <summary>Icelandic</summary>
            @is,
            /// <summary>Igbo</summary>
            ig,
            /// <summary>Indonesian</summary>
            id,
            /// <summary>Irish</summary>
            ga,
            /// <summary>Italian</summary>
            it,
            /// <summary>Japanese</summary>
            ja,
            /// <summary>Javanese</summary>
            jw,
            /// <summary>Kannada</summary>
            kn,
            /// <summary>Kazakh</summary>
            kk,
            /// <summary>Khmer</summary>
            km,
            /// <summary>Korean</summary>
            ko,
            /// <summary>Kurdish (Kurmanji)</summary>
            ku,
            /// <summary>Kyrgyz</summary>
            ky,
            /// <summary>Lao</summary>
            lo,
            /// <summary>Latin</summary>
            la,
            /// <summary>Latvian</summary>
            lv,
            /// <summary>Lithuanian</summary>
            lt,
            /// <summary>Luxembourgish</summary>
            lb,
            /// <summary>Macedonian</summary>
            mk,
            /// <summary>Malagasy</summary>
            mg,
            /// <summary>Malay</summary>
            ms,
            /// <summary>Malayalam</summary>
            ml,
            /// <summary>Maltese</summary>
            mt,
            /// <summary>Maori</summary>
            mi,
            /// <summary>Marathi</summary>
            mr,
            /// <summary>Mongolian</summary>
            mn,
            /// <summary>Myanmar (Burmese)</summary>
            my,
            /// <summary>Nepali</summary>
            ne,
            /// <summary>Norwegian</summary>
            no,
            /// <summary>Odia</summary>
            or,
            /// <summary>Pashto</summary>
            ps,
            /// <summary>Persian</summary>
            fa,
            /// <summary>Polish</summary>
            pl,
            /// <summary>Portuguese</summary>
            pt,
            /// <summary>Punjabi</summary>
            pa,
            /// <summary>Romanian</summary>
            ro,
            /// <summary>Russian</summary>
            ru,
            /// <summary>Samoan</summary>
            sm,
            /// <summary>Scots Gaelic</summary>
            gd,
            /// <summary>Serbian</summary>
            sr,
            /// <summary>Sesotho</summary>
            st,
            /// <summary>Shona</summary>
            sn,
            /// <summary>Sindhi</summary>
            sd,
            /// <summary>Sinhala</summary>
            si,
            /// <summary>Slovak</summary>
            sk,
            /// <summary>Slovenian</summary>
            sl,
            /// <summary>Somali</summary>
            so,
            /// <summary>Spanish</summary>
            es,
            /// <summary>Sundanese</summary>
            su,
            /// <summary>Swahili</summary>
            sw,
            /// <summary>Swedish</summary>
            sv,
            /// <summary>Tajik</summary>
            tg,
            /// <summary>Tamil</summary>
            ta,
            /// <summary>Telugu</summary>
            te,
            /// <summary>Thai</summary>
            th,
            /// <summary>Turkish</summary>
            tr,
            /// <summary>Ukrainian</summary>
            uk,
            /// <summary>Urdu</summary>
            ur,
            /// <summary>Uyghur</summary>
            ug,
            /// <summary>Uzbek</summary>
            uz,
            /// <summary>Vietnamese</summary>
            vi,
            /// <summary>Welsh</summary>
            cy,
            /// <summary>Xhosa</summary>
            xh,
            /// <summary>Yiddish</summary>
            yi,
            /// <summary>Yoruba</summary>
            yo,
            /// <summary>Zulu</summary>
            zu
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