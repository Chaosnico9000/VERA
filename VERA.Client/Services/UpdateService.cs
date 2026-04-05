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
            Timeout = TimeSpan.FromSeconds(30)
        };

        private const string ReleasesApiUrl = "https://api.github.com/repos/Chaosnico9000/VERA/releases/latest";
        private const string ApkAssetName   = "vera-android.apk";

        public async Task<UpdateInfo?> CheckAsync()
        {
            try
            {
                var release = await _http.GetFromJsonAsync<GitHubRelease>(ReleasesApiUrl).ConfigureAwait(false);
                if (release is null) return null;

                var tag = release.TagName.TrimStart('v');
                if (!Version.TryParse(tag,                out var latest)  ||
                    !Version.TryParse(AppVersion.Current, out var current))
                    return null;

                var apkAsset    = release.Assets.FirstOrDefault(a => a.Name == ApkAssetName);
                var downloadUrl = apkAsset?.BrowserDownloadUrl ?? release.HtmlUrl;

                return new UpdateInfo(tag, downloadUrl, latest > current);
            }
            catch { return null; }
        }

        /// <summary>
        /// Startet den Download auf Android als Foreground-Service (läuft auch wenn App minimiert wird).
        /// Auf anderen Plattformen wird der Browser-Download geöffnet.
        /// </summary>
        public Task<bool> DownloadAndInstallAsync(string downloadUrl, IProgress<double>? progress = null)
        {
#if ANDROID
            try
            {
                var context = Android.App.Application.Context;
                var intent  = new Android.Content.Intent(context,
                    typeof(VERA.Platforms.Android.Services.UpdateDownloadService));
                intent.SetAction(VERA.Platforms.Android.Services.UpdateDownloadService.ActionStart);
                intent.PutExtra(VERA.Platforms.Android.Services.UpdateDownloadService.ExtraUrl, downloadUrl);

                if (OperatingSystem.IsAndroidVersionAtLeast(26))
                    context.StartForegroundService(intent);
                else
                    context.StartService(intent);

                return Task.FromResult(true);
            }
            catch { return Task.FromResult(false); }
#else
            _ = Launcher.OpenAsync(new Uri(downloadUrl));
            return Task.FromResult(true);
#endif
        }

        private record GitHubRelease(
            [property: JsonPropertyName("tag_name")] string           TagName,
            [property: JsonPropertyName("html_url")] string           HtmlUrl,
            [property: JsonPropertyName("assets")]   List<GitHubAsset> Assets);

        private record GitHubAsset(
            [property: JsonPropertyName("name")]                  string Name,
            [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl);
    }
}
