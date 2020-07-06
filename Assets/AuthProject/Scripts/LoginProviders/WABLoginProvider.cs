using System;
using System.Threading.Tasks;

#if ENABLE_WINMD_SUPPORT
using Windows.Security.Authentication.Web;
#endif

public class WABLoginProvider : BaseLoginProvider
{
    public WABLoginProvider(IAADLogger Logger, IUserStore userStore, string clientId, string authority) : 
        base(Logger, userStore, clientId, authority)
    {
    }

    public override string UserIdKey
    {
        get
        {
            return "UserIdKeyWAB";
        }
    }

    public override string Description => $"WebAuthenticationBroker facilitates acquisition of auth tokens using OAuth by handling the presentaion and redirects of an indentity provider page and returning the tokens back to your app.";

    public override string ProviderName => $"WebAuthenticationBroker";

    public async override Task<IToken> LoginAsync()
    {
        string accessToken = string.Empty;
        Logger.Log("Loggin in with WebAuthenticationBroker...");

#if ENABLE_WINMD_SUPPORT
        var redirectUri = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().AbsoluteUri;

        var state = Guid.NewGuid().ToString();
        var nonce = Guid.NewGuid().ToString();

        //string url = "https://login.microsoftonline.com/common";
        string url = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";

        var uri = new Uri($"{url}?" +
            $"client_id={ClientId}&" +
            $"scope={Uri.EscapeDataString("https://sts.mixedreality.azure.com/mixedreality.signin")} openid&" +
            $"response_type=token&" +
            $"state={Uri.EscapeDataString(state)}&" +
            $"nonce={Uri.EscapeDataString(nonce)}&" +
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}");
        //+ $"prompt=select_account"); 

        bool useEnterpriseAuth = false;
        var options = useEnterpriseAuth == true ? WebAuthenticationOptions.UseCorporateNetwork : WebAuthenticationOptions.None;

        Logger.Log("Using Start URI: ");
        Logger.Log(uri.AbsoluteUri);

        Logger.Log("Waiting for authentication...");
        try
        {
            WebAuthenticationResult result;

            Logger.Log("Trying silent auth");
            result = await WebAuthenticationBroker.AuthenticateSilentlyAsync(uri);
            Logger.Log($"Silent Auth result: {result.ResponseStatus.ToString()}");

            if (result.ResponseStatus != WebAuthenticationStatus.Success)
            {
                Logger.Log($"{result.ResponseData} : [{result.ResponseErrorDetail.ToString()}]");
                Logger.Log("Trying UI auth");
                result = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, uri);//, new Uri(redirectUri));
            }

            Logger.Log("Waiting for authentication complete.");

            switch (result.ResponseStatus)
            {
                case WebAuthenticationStatus.Success:
                    Logger.Log("Authentication Successful!");
                    Logger.Log("Received data:");
                    Logger.Log(result.ResponseData);
                    accessToken = result.ResponseData.Split('=')[1];
                    break;
                case WebAuthenticationStatus.UserCancel:
                    Logger.Log("User cancelled authentication. Try again.");
                    break;
                case WebAuthenticationStatus.ErrorHttp:
                    Logger.Log("HTTP Error. Try again.");
                    Logger.Log(result.ResponseErrorDetail.ToString());
                    break;
                default:
                    Logger.Log("Unknown Response");
                    break;
            }
        }
        catch (Exception e)
        {
            Logger.Log($"Unhandled {e} - {e.Message}");
            Logger.Log($" >> Have you enabled the Internet Client capability?");
            Logger.Log($" >> If {url} is directly accessible via a Local Network, try enabling 'Use Corporate Network'.");
        }
#endif

        return new AADToken(accessToken);
    }

    public override async Task SignOutAsync()
    {
        Logger.Clear();
        Logger.Log("Sign out initiated...");
#if ENABLE_WINMD_SUPPORT
        var redirectUri = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().AbsoluteUri;
        var state = Guid.NewGuid().ToString();
        var logoutUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/logout";

        var uri = new Uri($"{logoutUrl}?" +
            $"state={Uri.EscapeDataString(state)}");

        Logger.Log($"Using signout URI: {uri.AbsoluteUri}");

        Logger.Log($"Waiting for signout...");
        var result = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, uri);
#endif
        Username = string.Empty;
        AADToken = string.Empty;
        AccessToken = string.Empty;

        Logger.Log($"Sign Out Complete");
    }
}
