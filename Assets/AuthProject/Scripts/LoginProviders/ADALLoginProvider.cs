using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading.Tasks;
using static AADLoginProviders;

public class ADALLoginProvider : BaseLoginProvider
{
    private AuthenticationContext authenticationContext;
    private FileTokenCache fileTokenCache;

    public ADALLoginProvider(IAADLogger Logger, IUserStore userStore, string clientId, string authority, string tenantId) : 
        base(Logger, userStore, clientId, authority)
    {
        TenantId = tenantId;
    }

    public override string UserIdKey
    {
        get
        {
            return "UserIdADAL";
        }
    }

    public string TenantId { get; }

    public override string Description => $"ADAL.NET (Microsoft.IdentityModel.Clients.ActiveDirectory) is an authentication library which enables you to acquire tokens from Azure AD and ADFS, to access protected Web APIs (Microsoft APIs or applications registered with Azure Active Directory). ADAL.NET is available on several .NET platforms (Desktop, Universal Windows Platform, Xamarin Android, Xamarin iOS, and .NET Core).";

    public override string ProviderName => $"Active Directory Authentication Library (ADAL)";

    public async override Task<IToken> LoginAsync()
    {
        string accessToken = string.Empty;
        Logger.Log("Loggin in with ADAL...");

#if UNITY_EDITOR
        var platformSupportsUI = false;
#else
        var platformSupportsUI = true;
#endif

        var authority = $"https://login.microsoftonline.com/{TenantId}";
        var resource = "https://sts.mixedreality.azure.com";

        // TRY https://login.microsoftonline.com/organizations
        authenticationContext = new AuthenticationContext(authority, new FileTokenCache(Logger));

        Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationResult authenticationResult = null;
        try
        {
            authenticationResult = await authenticationContext.AcquireTokenSilentAsync("https://sts.mixedreality.azure.com",
                ClientId);
        }
        catch (AdalSilentTokenAcquisitionException adalEx)
        {
            // Exception: AdalSilentTokenAcquisitionException
            // Caused when there are no tokens in the cache or a required refresh failed. 
            // Action: Case 1, resolvable with an interactive request. 
            Logger.Log($"AdalSilentTokenAcquisitionException Message: {adalEx.Message}");
            Logger.Log($"AdalSilentTokenAcquisitionException Message: {adalEx.ErrorCode.ToString()}");

            try
            {
                var userId = Store.GetUserId(UserIdKey);
                UserIdentifier userIdentifier = UserIdentifier.AnyUser;
                if (!string.IsNullOrEmpty(userId))
                {
                    userIdentifier = new UserIdentifier(userId, UserIdentifierType.UniqueId);
                }
                string extraQueryParameters = string.Empty;

                if (platformSupportsUI)
                {
                    authenticationResult =
                        await authenticationContext.AcquireTokenAsync(resource, ClientId, new Uri("urn:ietf:wg:oauth:2.0:oob"),
                            new PlatformParameters(PromptBehavior.Auto, false), userIdentifier, extraQueryParameters);
                }
                else
                {
                    DeviceCodeResult codeResult =
                        await authenticationContext.AcquireDeviceCodeAsync(resource, ClientId, extraQueryParameters);

                    // Here we need a ui notification to the user to go and login elsewhere..
                    // display codeResult.message...
                    Logger.Log(codeResult.Message);

                    authenticationResult = await authenticationContext.AcquireTokenByDeviceCodeAsync(codeResult);
                }
            }
            catch (AdalServiceException e)
            {
                HandleAdalServiceException(e, Logger);
            }

            catch (AdalException e)
            {
                HandleAdalException(e, Logger);
            }
        }

        catch (AdalServiceException e)
        {
            HandleAdalServiceException(e, Logger);
        }

        catch (AdalException e)
        {
            HandleAdalException(e, Logger);
        }

        if (authenticationResult != null)
        {
            if (authenticationResult.UserInfo != null)
            {
                Logger.Log($"Provider: {authenticationResult.UserInfo.IdentityProvider}");
                Logger.Log($"Given Name: {authenticationResult.UserInfo.GivenName}");
                Logger.Log($"Family Name: {authenticationResult.UserInfo.FamilyName}");
                Logger.Log($"ID: {authenticationResult.UserInfo.DisplayableId}");
            }

            // Cache the user identifier...
            Store.SaveUser(UserIdKey, authenticationResult.UserInfo.UniqueId);

            accessToken = authenticationResult.AccessToken;
            Username = authenticationResult.UserInfo.DisplayableId;
            Logger.Log($"Access Token: {accessToken}");
        }

        return new AADToken(accessToken);
    }

    private void HandleAdalServiceException(AdalServiceException e, IAADLogger Logger)
    {
        // Exception: AdalServiceException 
        // Represents an error produced by the STS. 
        // e.ErrorCode contains the error code and description, which can be used for debugging. 
        // NOTE: Do not code a dependency on the contents of the error description, as it can change over time.

        // Design time consideration: Certain errors may be caused at development and exposed through this exception. 
        // Looking inside the description will give more guidance on resolving the specific issue. 

        // Action: Case 1: Non-Retryable 
        // Do not perform an immediate retry. Only retry after user action. 
        // Example Errors: default case
        Logger.Log($"AdalServiceException Message: {e.Message}");
        Logger.Log($"AdalServiceException Status Code: {e.StatusCode.ToString()}");
    }

    private void HandleAdalException(AdalException e, IAADLogger Logger)
    {
        // Exception: AdalException 
        // Represents a library exception generated by ADAL .NET.
        // e.ErrorCode contains the error code

        // Action: Case 1, Non-Retryable 
        // Do not perform an immediate retry. Only retry after user action. 
        // Example Errors: network_not_available, default case
        Logger.Log($"AdalException Message: {e.Message}");
        Logger.Log($"AdalException Error Code: {e.ErrorCode.ToString()}");
    }

    public async override Task SignOutAsync()
    {
        Logger.Clear();
        authenticationContext.TokenCache.Clear();
        Store.ClearUser(UserIdKey);
        Username = string.Empty;
        AADToken = string.Empty;
        AccessToken = string.Empty;
        Logger.Log("Signed Out");
    }
}
