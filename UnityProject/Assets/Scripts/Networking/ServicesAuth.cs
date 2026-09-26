using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;

// Shared Unity Services init + anonymous sign-in, used by Relay hosting/joining and the friends system.
public static class ServicesAuth
{
    public static async Task EnsureSignedInAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
}
