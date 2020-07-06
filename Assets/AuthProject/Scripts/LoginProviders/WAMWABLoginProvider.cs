using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#if ENABLE_WINMD_SUPPORT
using Windows.Security.Authentication.Web;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
#endif

public class WAMWABLoginProvider : BaseLoginProvider
{
    public WAMWABLoginProvider(IAADLogger Logger, IUserStore userStore, string clientId, string authority) :
        base(Logger, userStore, clientId, authority)
    {
    }

    public override string UserIdKey
    {
        get
        {
            return "UserIdWAMWAB";
        }
    }

    public override string Description => $"";

    public override string ProviderName => $"WebAuthenticationBroker & WebAuthenticationCoreManager";

    public async override Task<IToken> LoginAsync()
    {
        Logger.Log("Loggin in with a combination of WebAuthenticationBroker and WebAuthenticationCoreManager...");

        string accessToken = string.Empty;
#if ENABLE_WINMD_SUPPORT
        // RUN THIS ON HOLOLENS - SEE IF WE GET A SYSTEM DIALOG IN AN IMMERSIVE APP
        //
        string userId = Store.GetUserId(UserIdKey);
        Logger.Log("User Id: " + userId);

        //string URI = string.Format("ms-appx-web://Microsoft.AAD.BrokerPlugIn/{0}", 
        //    WebAuthenticationBroker.GetCurrentApplicationCallbackUri().Host.ToUpper());
        WebAccountProvider wap =
            await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", Authority);

        Logger.Log($"Found Web Account Provider for organizations: {wap.DisplayName}");

        var accts = await WebAuthenticationCoreManager.FindAllAccountsAsync(wap);

        Logger.Log($"Find All Accounts Status = {accts.Status}");

        if (accts.Status == FindAllWebAccountsStatus.Success)
        {
            foreach (var acct in accts.Accounts)
            {
                Logger.Log($"Account: {acct.UserName} {acct.State.ToString()}");
            }
        }

        var sap = await WebAuthenticationCoreManager.FindSystemAccountProviderAsync(wap.Id);
        if (sap != null)
        {
            string displayName = "Not Found";
            if (sap.User != null)
            {
                displayName = (string)await sap.User.GetPropertyAsync("DisplayName");
                Logger.Log($"Found system account provider {sap.DisplayName} with user {displayName} {sap.User.AuthenticationStatus.ToString()}");
            }
        }

        Logger.Log("Web Account Provider: " + wap.DisplayName);

        string resource = "https://sts.mixedreality.azure.com";

        //var scope = "https://management.azure.com/user_impersonation";
        //WebTokenRequest wtr = new WebTokenRequest(wap, scope, "3c663152-fdf9-4033-963f-c398c21212d9");
        //WebTokenRequest wtr = new WebTokenRequest(wap, scope, "5c8c830a-4cf8-470e-ba0d-6d815feba800");

        WebTokenRequest wtr = new WebTokenRequest(wap, "https://sts.mixedreality.azure.com/mixedreality.signin", ClientId);
        wtr.Properties.Add("resource", resource);

        WebAccount account = null;

        if (!string.IsNullOrEmpty((string)userId))
        {
            account = await WebAuthenticationCoreManager.FindAccountAsync(wap, (string)userId);
            if (account != null)
            {
                Logger.Log("Found account: " + account.UserName);
            }
            else
            {
                Logger.Log("Account not found");
            }
        }

        WebTokenRequestResult tokenResponse = null;
        try
        {
            if (account != null)
            {
                tokenResponse = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(wtr, account);
            }
            else
            {
                tokenResponse = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(wtr);
            }
        }
        catch (Exception ex)
        {
            Logger.Log(ex.Message);
        }

        Logger.Log("Silent Token Response: " + tokenResponse.ResponseStatus.ToString());
        if (tokenResponse.ResponseError != null)
        {
            Logger.Log("Error Code: " + tokenResponse.ResponseError.ErrorCode.ToString());
            Logger.Log("Error Msg: " + tokenResponse.ResponseError.ErrorMessage.ToString());
            foreach (var errProp in tokenResponse.ResponseError.Properties)
            {
                Logger.Log($"Error prop: ({errProp.Key}, {errProp.Value})");
            }
        }

        if (tokenResponse.ResponseStatus == WebTokenRequestStatus.UserInteractionRequired)
        {
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

            var result = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, uri, new Uri(redirectUri));

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

            if (account != null && !string.IsNullOrEmpty(account.Id))
            {
                Store.SaveUser(UserIdKey, account.Id);
            }
        }
#endif

        return new AADToken(accessToken);
    }

    public override async Task SignOutAsync()
    {
        Logger.Clear();
        string userId = Store.GetUserId(UserIdKey);
        if (!string.IsNullOrEmpty((string)userId))
        {
#if ENABLE_WINMD_SUPPORT

            WebAccountProvider wap =
                await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", Authority);
            var account = await WebAuthenticationCoreManager.FindAccountAsync(wap, (string)userId);
            Logger.Log($"Found account: {account.UserName} State: {account.State.ToString()}");
            await account.SignOutAsync();
#endif
            Store.ClearUser(UserIdKey);
            Username = string.Empty;
            AADToken = string.Empty;
            AccessToken = string.Empty;
            Logger.Log("Signed Out");
        }
    }
}