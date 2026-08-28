using AndroidX.Biometric;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;
using Java.Lang;

namespace Plugin.Maui.SecureSession;

sealed class PlatformBiometricGate : IBiometricGate
{
    const int Authenticators =
        BiometricManager.Authenticators.BiometricStrong |
        BiometricManager.Authenticators.BiometricWeak;

    public static IBiometricGate Create() => new PlatformBiometricGate();

    public Task<BiometricAvailability> GetAvailabilityAsync()
    {
        var context = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity
                      ?? Microsoft.Maui.ApplicationModel.Platform.AppContext;
        if (context is null)
        {
            return Task.FromResult(BiometricAvailability.Unavailable);
        }

        var result = BiometricManager.From(context).CanAuthenticate(Authenticators);
        var availability = result switch
        {
            BiometricManager.BiometricSuccess => BiometricAvailability.Available,
            BiometricManager.BiometricErrorNoneEnrolled => BiometricAvailability.NotEnrolled,
            BiometricManager.BiometricErrorNoHardware => BiometricAvailability.NotSupported,
            BiometricManager.BiometricErrorHwUnavailable => BiometricAvailability.Unavailable,
            BiometricManager.BiometricErrorUnsupported => BiometricAvailability.NotSupported,
            _ => BiometricAvailability.Unavailable
        };

        return Task.FromResult(availability);
    }

    public Task<bool> AuthenticateAsync(string reason, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is not FragmentActivity activity)
                {
                    tcs.TrySetException(new BiometricAuthenticationException(
                        "Biometric unlock requires a foreground activity."));
                    return;
                }

                var callback = new AuthCallback(tcs);
                var executor = ContextCompat.GetMainExecutor(activity)
                    ?? throw new BiometricAuthenticationException("No main executor is available.");
                var prompt = new BiometricPrompt(activity, executor, callback);
                var info = new BiometricPrompt.PromptInfo.Builder()
                    .SetTitle(reason)
                    .SetSubtitle("Confirm your identity")
                    .SetNegativeButtonText("Cancel")
                    .SetAllowedAuthenticators(Authenticators)
                    .Build();

                if (cancellationToken.CanBeCanceled)
                {
                    cancellationToken.Register(() =>
                    {
                        prompt.CancelAuthentication();
                        tcs.TrySetCanceled(cancellationToken);
                    });
                }

                prompt.Authenticate(info);
            }
            catch (System.Exception ex)
            {
                tcs.TrySetException(new BiometricAuthenticationException("Biometric prompt failed to start.", ex));
            }
        });

        return tcs.Task;
    }

    sealed class AuthCallback : BiometricPrompt.AuthenticationCallback
    {
        readonly TaskCompletionSource<bool> _tcs;

        public AuthCallback(TaskCompletionSource<bool> tcs)
        {
            _tcs = tcs;
        }

        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result) =>
            _tcs.TrySetResult(true);

        public override void OnAuthenticationError(int errorCode, ICharSequence errString)
        {
            if (errorCode is BiometricPrompt.ErrorUserCanceled
                or BiometricPrompt.ErrorNegativeButton
                or BiometricPrompt.ErrorCanceled)
            {
                _tcs.TrySetResult(false);
                return;
            }

            _tcs.TrySetException(new BiometricAuthenticationException(errString?.ToString() ?? "Biometric authentication failed."));
        }
    }
}
