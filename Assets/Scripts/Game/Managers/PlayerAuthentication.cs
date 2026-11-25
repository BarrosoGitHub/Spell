using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;

public sealed class PlayerAuthentication
{
    private Task signInTask;

    public bool IsSignedIn => UnityServices.State == ServicesInitializationState.Initialized &&
        AuthenticationService.Instance.IsSignedIn;

    public string PlayerId => IsSignedIn ? AuthenticationService.Instance.PlayerId : null;

    public Task SignInAsync()
    {
        if (signInTask != null && !signInTask.IsCompleted)
            return signInTask;

        if (IsSignedIn)
            return Task.CompletedTask;

        signInTask = SignInCoreAsync();
        return signInTask;
    }

    private async Task SignInCoreAsync()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
}
