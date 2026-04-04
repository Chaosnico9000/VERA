namespace VERA.Services
{
    public enum AuthResult
    {
        Success,
        Failure,
        Cancelled,
        NotAvailable
    }

    public interface IAuthService
    {
        /// <summary>Gibt true zurück wenn Biometrie oder Gerätesperre verfügbar ist.</summary>
        bool IsAvailable { get; }

        /// <summary>Fordert den Nutzer zur Authentifizierung auf (Fingerabdruck, Gesicht, PIN, Muster).</summary>
        Task<AuthResult> AuthenticateAsync(string reason);
    }
}
