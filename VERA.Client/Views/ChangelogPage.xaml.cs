using VERA.Shared;

namespace VERA.Views
{
    // ── Datenmodelle ────────────────────────────────────────────────────────

    public class ChangelogItem
    {
        public string Title { get; init; } = string.Empty;
        public string Body  { get; init; } = string.Empty;
        public bool HasTitle => !string.IsNullOrWhiteSpace(Title);
    }

    public class ChangelogRelease
    {
        public string Version     { get; init; } = string.Empty;
        public string DateDisplay { get; init; } = string.Empty;
        public bool   IsCurrent   { get; init; }

        public List<ChangelogItem> AddedItems   { get; init; } = [];
        public List<ChangelogItem> FixedItems   { get; init; } = [];
        public List<ChangelogItem> ChangedItems { get; init; } = [];

        public bool HasAdded   => AddedItems.Count   > 0;
        public bool HasFixed   => FixedItems.Count   > 0;
        public bool HasChanged => ChangedItems.Count > 0;

        public bool HasAddedAndFixed    => HasAdded   && HasFixed;
        public bool HasFixedAndChanged  => HasFixed   && HasChanged;

        public string AddedCountLabel   => $"+{AddedItems.Count}";
        public string FixedCountLabel   => $"×{FixedItems.Count}";
        public string ChangedCountLabel => $"~{ChangedItems.Count}";

        // Farben: aktuelle Version hervorgehoben
        public Color VersionColor         => IsCurrent ? Color.FromArgb("#4ECCA3") : Color.FromArgb("#7AA5FF");
        public Color HeaderBackground     => IsCurrent ? Color.FromArgb("#0D2218") : Color.FromArgb("#131A42");
        public Color HeaderBorderColor    => IsCurrent ? Color.FromArgb("#2A6044") : Color.FromArgb("#1A2460");
        public Color DateColor            => IsCurrent ? Color.FromArgb("#4ECCA3") : Color.FromArgb("#424F8A");
        public Color CurrentBadgeBackground => Color.FromArgb("#1A4030");
    }

    // ── Page ────────────────────────────────────────────────────────────────

    public partial class ChangelogPage : ContentPage
    {
        public ChangelogPage(string content)
        {
            InitializeComponent();
            var releases = ParseChangelog(content);
            ReleasesView.ItemsSource = releases;
            ReleaseBadgeLabel.Text   = $"{releases.Count} Releases";
        }

        private async void OnBackClicked(object? sender, EventArgs e)
            => await Navigation.PopAsync();

        // ── Markdown-Parser ─────────────────────────────────────────────────

        private static List<ChangelogRelease> ParseChangelog(string markdown)
        {
            var releases = new List<ChangelogRelease>();
            var lines    = markdown.Split('\n');

            string? currentVersion  = null;
            string  currentDate     = string.Empty;
            string  currentSection  = string.Empty;

            var addedItems   = new List<ChangelogItem>();
            var fixedItems   = new List<ChangelogItem>();
            var changedItems = new List<ChangelogItem>();

            void FlushRelease()
            {
                if (currentVersion is null) return;
                if (currentVersion.Equals("Unreleased", StringComparison.OrdinalIgnoreCase)) return;
                // Leere Releases ohne Einträge trotzdem anzeigen (z.B. 1.4.2)
                releases.Add(new ChangelogRelease
                {
                    Version     = $"v{currentVersion}",
                    DateDisplay = currentDate,
                    IsCurrent   = currentVersion == AppVersion.Current,
                    AddedItems   = [.. addedItems],
                    FixedItems   = [.. fixedItems],
                    ChangedItems = [.. changedItems],
                });
                addedItems.Clear();
                fixedItems.Clear();
                changedItems.Clear();
            }

            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimEnd();

                // ## [1.5.0] - 2026-04-05  oder  ## [Unreleased]
                if (line.StartsWith("## [", StringComparison.Ordinal))
                {
                    FlushRelease();
                    currentSection = string.Empty;
                    var inner = line[4..];
                    var closeBracket = inner.IndexOf(']');
                    if (closeBracket < 0) continue;
                    currentVersion = inner[..closeBracket].Trim();
                    var rest = inner[(closeBracket + 1)..].Trim();
                    currentDate = rest.StartsWith("- ") ? rest[2..].Trim() : string.Empty;
                    continue;
                }

                // ### Added / Fixed / Changed / Security / Deprecated / Removed
                if (line.StartsWith("### ", StringComparison.Ordinal))
                {
                    currentSection = line[4..].Trim().ToLowerInvariant();
                    continue;
                }

                // Bullet-Zeile: "- **Titel:** Beschreibung" oder "- Nur Text"
                if (line.TrimStart().StartsWith("- ", StringComparison.Ordinal))
                {
                    var bullet = line.TrimStart()[2..].Trim();
                    var item   = ParseBullet(bullet);
                    switch (currentSection)
                    {
                        case "added":
                        case "neu":
                            addedItems.Add(item); break;
                        case "fixed":
                        case "behoben":
                        case "fixes":
                            fixedItems.Add(item); break;
                        case "changed":
                        case "geändert":
                        case "security":
                        case "deprecated":
                        case "removed":
                            changedItems.Add(item); break;
                    }
                }
            }

            FlushRelease();
            return releases;
        }

        // Parst "**Titel:** Beschreibungstext" → ChangelogItem
        private static ChangelogItem ParseBullet(string text)
        {
            // Format: **Titel:** Rest-Text (Backtick-Inline-Code wird behalten)
            if (text.StartsWith("**", StringComparison.Ordinal))
            {
                var endBold = text.IndexOf("**", 2, StringComparison.Ordinal);
                if (endBold > 0)
                {
                    var rawTitle = text[2..endBold];
                    var afterBold = text[(endBold + 2)..].TrimStart(':', ' ');
                    // Backticks entfernen für saubere Anzeige
                    return new ChangelogItem
                    {
                        Title = rawTitle,
                        Body  = StripMarkdown(afterBold),
                    };
                }
            }
            return new ChangelogItem { Body = StripMarkdown(text) };
        }

        // Entfernt `backticks`, **bold**, _italic_ für die Anzeige
        private static string StripMarkdown(string s)
        {
            // Backtick-Code
            var result = System.Text.RegularExpressions.Regex.Replace(s, @"`([^`]+)`", "$1");
            // **bold**
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\*\*(.+?)\*\*", "$1");
            // _italic_ oder *italic*
            result = System.Text.RegularExpressions.Regex.Replace(result, @"[_\*](.+?)[_\*]", "$1");
            return result.Trim();
        }
    }
}
