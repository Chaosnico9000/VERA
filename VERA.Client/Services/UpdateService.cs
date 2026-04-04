using System.Net.Http.Json;
using System.Text.Json.Serialization;
using VERA.Shared;

namespace VERA.Services
{
    public record UpdateInfo(string LatestVersion, string DownloadUrl, bool IsNewer);

    public class UpdateService
    {
        private static readonly HttpClient _http = new()
        {
            DefaultRequestHeaders = { { "User-Agent", "VERA-App" } },
            Timeout = TimeSpan.FromSeconds(10)
        };

        private const string ReleasesApiUrl = "https://api.github.com/repos/Chaosnico9000/VERA/releases/latest";
        private const string ApkAssetName   = "vera-android.apk";

        public async Task<UpdateInfo?> CheckAsync()
        {
            try
            {
                var release = await _http.GetFromJsonAsync<GitHubRelease>(ReleasesApiUrl);
                if (release is null) return null;

                var tag = release.TagName.TrimStart('v');
                if (!Version.TryParse(tag,                 out var latest)  ||
                    !Version.TryParse(AppVersion.Current,  out var current))
                    return null;

                var apkAsset    = release.Assets.FirstOrDefault(a => a.Name == ApkAssetName);
                var downloadUrl = apkAsset?.BrowserDownloadUrl ?? release.HtmlUrl;

                return new UpdateInfo(tag, downloadUrl, latest > current);
            }
            catch { return null; }
        }

        private record GitHubRelease(
            [property: JsonPropertyName("tag_name")] string        TagName,
            [property: JsonPropertyName("html_url")] string        HtmlUrl,
            [property: JsonPropertyName("assets")]   List<GitHubAsset> Assets);

        private record GitHubAsset(
            [property: JsonPropertyName("name")]                  string Name,
            [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl);
    }
}
