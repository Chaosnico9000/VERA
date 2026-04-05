namespace VERA.Services
{
    public class ThemeService
    {
        private const string PrefKey = "app_theme"; // "dark" | "light" | "system"

        // ── Dark Palette ──────────────────────────────────────────────────
        private static readonly Dictionary<string, Color> DarkColors = new()
        {
            ["AppBackground"] = Color.FromArgb("#080C1E"),
            ["SurfaceColor"]  = Color.FromArgb("#0D1232"),
            ["CardColor"]     = Color.FromArgb("#131A42"),
            ["CardElevated"]  = Color.FromArgb("#182155"),
            ["BorderColor"]   = Color.FromArgb("#1A2460"),
            ["DividerColor"]  = Color.FromArgb("#111840"),
            ["TextPrimary"]   = Color.FromArgb("#FFFFFF"),
            ["TextSecondary"] = Color.FromArgb("#8FA0DC"),
            ["TextMuted"]     = Color.FromArgb("#424F8A"),
        };

        // ── Light Palette ─────────────────────────────────────────────────
        private static readonly Dictionary<string, Color> LightColors = new()
        {
            ["AppBackground"] = Color.FromArgb("#F0F4FF"),
            ["SurfaceColor"]  = Color.FromArgb("#E8EEFF"),
            ["CardColor"]     = Color.FromArgb("#FFFFFF"),
            ["CardElevated"]  = Color.FromArgb("#EEF2FF"),
            ["BorderColor"]   = Color.FromArgb("#C5D0F5"),
            ["DividerColor"]  = Color.FromArgb("#D8E0F7"),
            ["TextPrimary"]   = Color.FromArgb("#080C1E"),
            ["TextSecondary"] = Color.FromArgb("#3D4D8A"),
            ["TextMuted"]     = Color.FromArgb("#7A8CC0"),
        };

        public string SavedTheme => Preferences.Default.Get(PrefKey, "dark");

        public void Apply(string theme)
        {
            Preferences.Default.Set(PrefKey, theme);
            if (Application.Current is null) return;

            var appTheme = theme switch
            {
                "light"  => AppTheme.Light,
                "system" => AppTheme.Unspecified,
                _        => AppTheme.Dark,
            };
            Application.Current.UserAppTheme = appTheme;

            // Effektives Theme bestimmen (bei "system" das OS-Theme auslesen)
            var effective = appTheme == AppTheme.Unspecified
                ? Application.Current.RequestedTheme
                : appTheme;

            var palette = effective == AppTheme.Light ? LightColors : DarkColors;
            var res     = Application.Current.Resources;
            foreach (var (key, color) in palette)
                res[key] = color;
        }

        public void ApplySaved() => Apply(SavedTheme);
    }
}
