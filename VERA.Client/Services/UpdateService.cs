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
        /// Lädt die APK herunter und startet den Android-System-Installer.
        /// Gibt false zurück wenn der Download fehlschlägt oder die Plattform kein Android ist.
        /// </summary>
        public async Task<bool> DownloadAndInstallAsync(string downloadUrl, IProgress<double>? progress = null)
        {
#if ANDROID
            try
            {
                var context = Android.App.Application.Context;
                var cacheDir = context.CacheDir!.AbsolutePath;
                var apkPath  = Path.Combine(cacheDir, "vera-update.apk");

                // Alte APK löschen falls vorhanden
                if (File.Exists(apkPath)) File.Delete(apkPath);

                // APK herunterladen mit Fortschrittsanzeige
                using var response = await _http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var total   = response.Content.Headers.ContentLength ?? -1L;
                var buffer  = new byte[81920];
                long downloaded = 0;

                await using var stream = await response.Content.ReadAsStreamAsync();
                await using var file   = File.Create(apkPath);

                int read;
                while ((read = await stream.ReadAsync(buffer)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, read));
                    downloaded += read;
                    if (total > 0) progress?.Report((double)downloaded / total);
                }

                // FileProvider-URI erzeugen und Installer starten
                var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(
                    context,
                    context.PackageName + ".fileprovider",
                    new Java.IO.File(apkPath));

                var intent = new Android.Content.Intent(Android.Content.Intent.ActionView);
                intent.SetDataAndType(uri, "application/vnd.android.package-archive");
                intent.AddFlags(Android.Content.ActivityFlags.GrantReadUriPermission);
                intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                context.StartActivity(intent);
                return true;
            }
            catch { return false; }
#else
            await Launcher.OpenAsync(new Uri(downloadUrl));
            return true;
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
