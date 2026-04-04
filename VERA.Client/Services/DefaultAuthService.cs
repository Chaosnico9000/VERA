namespace VERA.Services
{
    /// <summary>
    /// Fallback für Plattformen ohne Biometrie-API (z.B. MacCatalyst).
    /// Gibt immer Success zurück – kein Sperren auf Desktop-Plattformen.
    /// </summary>
    public class DefaultAuthService : IAuthService
    {
        public bool IsAvailable => false;

        public Task<AuthResult> AuthenticateAsync(string reason)
            => Task.FromResult(AuthResult.Success);
    }
}
