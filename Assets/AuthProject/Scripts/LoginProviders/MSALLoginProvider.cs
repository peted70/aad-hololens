using Microsoft.Identity.Client;
using System;

using System.Threading.Tasks;
public class MSALLoginProvider : BaseLoginProvider
{
    public MSALLoginProvider(IAADLogger Logger, IUserStore store, string clientId, string authority, string tenantId) : 
        base(Logger, store, clientId, authority)
    {
        TenantId = tenantId;
    }

    public bool UseDeviceCodeFlow { get; set; } = false;

    public override string UserIdKey
    {
        get
        {
            return "UserIdMSAL";
        }
    }

    public string TenantId { get; }

    public override string Description => $"Microsoft Authentication Library (MSAL) enables developers to acquire tokens from the Microsoft identity platform endpoint in order to access secured web APIs. These web APIs can be the Microsoft Graph, other Microsoft APIs, third-party web APIs, or your own web API. MSAL is available for .NET, JavaScript, Android, and iOS, which support many different application architectures and platforms.";

    public override string ProviderName => $"Microsoft Authentication Library (MSAL)";

    public override async Task<IToken> LoginAsync()
    {
        Logger.Log("Logging in with MSAL...");
        string url = $"https://login.microsoftonline.com/{TenantId}";

#if UNITY_EDITOR
        var platformSupportsUI = false;
#else
        var platformSupportsUI = true;
#endif
        string userId = Store.GetUserId(UserIdKey);
        Logger.Log("User Id: " + userId);

        var ret = await Task.Run(async () =>
        {
#if LATEST_MSAL
            var app = PublicClientApplicationBuilder.Create(ClientId)
                .WithDefaultRedirectUri()
                .WithAuthority(url)
                .WithLogging(LoggingCallback)
                .Build();
#else
        var app = new PublicClientApplication(ClientId);
#endif

            IAccount account = null;
            if (!string.IsNullOrEmpty(userId))
            {
                account = await app.GetAccountAsync(userId);
                if (account != null)
                    Logger.Log($"Account found id = {account.HomeAccountId}");
            }

            //string[] scopes = new string[] { "user.read" };//, 

            //string[] scopes = new string[] { "https://sts.mixedreality.azure.com/mixedreality.signin" };
            string[] scopes = new string[] { "https://sts.mixedreality.azure.com//.default" };
            
            Microsoft.Identity.Client.AuthenticationResult authResult = null;

            //Declaring the Public Client properties
            //string tenantID = {Your tenant ID or .onmicrosoft.com domain};

            //For this example, the API to be accessed is EWS
            //string[] scopes = new string[] { "https://outlook.office365.com/.default" };

            //string redirectURI = {Your app registration's redirectURI};
            try
            {
#if LATEST_MSAL
                authResult = await app.AcquireTokenSilent(scopes, account).ExecuteAsync();
#else
                authResult = await app.AcquireTokenSilentAsync(scopes, account);
#endif
            }
            catch (MsalUiRequiredException)
            {
                try
                {
                    if (platformSupportsUI && !UseDeviceCodeFlow)
                    {
#if LATEST_MSAL
                        authResult = await app.AcquireTokenInteractive(scopes).ExecuteAsync();
#else
                        authResult = await app.AcquireTokenAsync(scopes);
#endif
                    }
                    else
                    {
                        authResult = await app.AcquireTokenWithDeviceCode(scopes, devicecoderesult =>
                        {
                            Logger.Log(devicecoderesult.Message);
                            return Task.FromResult(0);
                        })
                        .ExecuteAsync();
                    }
                }
                catch (MsalException msalEx)
                {
                    Logger.Log(msalEx.Message);
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.Message);
                }
            }
            catch (MsalException msalEx)
            {
                Logger.Log(msalEx.Message);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
            }

            if (authResult == null)
                return null;

            Username = authResult.Account.Username;
            Logger.Log("Access Token: " + authResult.AccessToken);

            return authResult;
        });


        if (ret.Account != null && !string.IsNullOrEmpty(ret.Account.HomeAccountId.Identifier))
        {
            Store.SaveUser(UserIdKey, ret.Account.HomeAccountId.Identifier);
        }

        AADToken = ret.AccessToken;
        return new AADToken(ret.AccessToken);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async override Task SignOutAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        Logger.Clear();
        Store.ClearUser(UserIdKey);
        AADToken = string.Empty;
        AccessToken = string.Empty;
        Username = string.Empty;
        Logger.Log("Signed Out");
    }

    private void LoggingCallback(LogLevel level, string message, bool containsPii)
    {
        Logger.Log(message);
    }
}