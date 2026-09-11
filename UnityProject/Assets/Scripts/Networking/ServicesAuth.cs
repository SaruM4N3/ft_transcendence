using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;

// Shared Unity Services init and anonymous sign-in.
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
