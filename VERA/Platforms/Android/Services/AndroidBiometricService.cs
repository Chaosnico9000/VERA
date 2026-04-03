using Android.Content;
using Android.OS;
using AndroidX.Biometric;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;
using VERA.Services;
using Java.Util.Concurrent;

namespace VERA.Platforms.Android.Services
{
    public class AndroidBiometricService : IAuthService
    {
        public bool IsAvailable
        {
            get
            {
                var context = global::Android.App.Application.Context;
                var manager = BiometricManager.From(context);
                var result = manager.CanAuthenticate(
                    BiometricManager.Authenticators.BiometricStrong |
                    BiometricManager.Authenticators.DeviceCredential);
                return result == BiometricManager.BiometricSuccess;
            }
        }

        public Task<AuthResult> AuthenticateAsync(string reason)
        {
            var tcs = new TaskCompletionSource<AuthResult>();

            var activity = Platform.CurrentActivity;
            if (activity is not FragmentActivity fragmentActivity)
            {
                tcs.TrySetResult(AuthResult.NotAvailable);
                return tcs.Task;
            }

            var executor = ContextCompat.GetMainExecutor(activity);

            var promptInfo = new BiometricPrompt.PromptInfo.Builder()
                .SetTitle("VERA – Entsperren")
                .SetSubtitle(reason)
                .SetAllowedAuthenticators(
                    BiometricManager.Authenticators.BiometricStrong |
                    BiometricManager.Authenticators.BiometricWeak |
                    BiometricManager.Authenticators.DeviceCredential)
                .Build();

            var callback = new BiometricCallback(tcs);
            var prompt = new BiometricPrompt(fragmentActivity, executor, callback);

            fragmentActivity.RunOnUiThread(() => prompt.Authenticate(promptInfo));

            return tcs.Task;
        }

        private sealed class BiometricCallback : BiometricPrompt.AuthenticationCallback
        {
            private readonly TaskCompletionSource<AuthResult> _tcs;

            public BiometricCallback(TaskCompletionSource<AuthResult> tcs)
                => _tcs = tcs;

            public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result)
                => _tcs.TrySetResult(AuthResult.Success);

            public override void OnAuthenticationError(int errorCode, Java.Lang.ICharSequence errString)
            {
                var result = errorCode == BiometricPrompt.ErrorUserCanceled ||
                             errorCode == BiometricPrompt.ErrorNegativeButton ||
                             errorCode == BiometricPrompt.ErrorCanceled
                    ? AuthResult.Cancelled
                    : AuthResult.Failure;
                _tcs.TrySetResult(result);
            }

            public override void OnAuthenticationFailed()
            {
                // Einzelner Fehlschlag – BiometricPrompt zeigt selbst einen Fehler,
                // wir warten auf OnAuthenticationError für endgültigen Abbruch.
            }
        }
    }
}
