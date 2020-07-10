using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

#if ENABLE_WINMD_SUPPORT
using Windows.Security.Authentication.Web;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;
#endif

public class WAMLoginProvider : BaseLoginProvider
{
    public WAMLoginProvider(IAADLogger logger, IUserStore userStore, string clientId, string authority, bool biometricsRequired = true) : 
        base(logger, userStore, clientId, authority)
    {
        BiometricsRequired = biometricsRequired;
    }

    public bool BiometricsRequired { get; set; } = false;

    public override string UserIdKey
    {
        get
        {
            return "UserIdWAM";
        }
    }

    public override string Description => $"Contains core methods for obtaining tokens from web account providers.";

    public override string ProviderName => $"WebAuthenticationCoreManager";

    public async override Task<IToken> LoginAsync()
    {
        Logger.Log("Loggin in with WebAuthenticationCoreManager...");

        string accessToken = string.Empty;
#if ENABLE_WINMD_SUPPORT

        if (BiometricsRequired)
        {
            if (await Windows.Security.Credentials.UI.UserConsentVerifier.CheckAvailabilityAsync() == Windows.Security.Credentials.UI.UserConsentVerifierAvailability.Available)
            {
                var consentResult = await Windows.Security.Credentials.UI.UserConsentVerifier.RequestVerificationAsync("Please verify your credentials");
                if (consentResult != Windows.Security.Credentials.UI.UserConsentVerificationResult.Verified)
                {
                    Logger.Log("Biometric verification failed.");
                    return null;
                }
            }
            else
            {
                Logger.Log("Biometric verification is not available or not configured.");
                return null;
            }
        }

        string userId = Store.GetUserId(UserIdKey);
        Logger.Log("User Id: " + userId);

        string URI = string.Format("ms-appx-web://Microsoft.AAD.BrokerPlugIn/{0}", 
            WebAuthenticationBroker.GetCurrentApplicationCallbackUri().Host.ToUpper());
        Logger.Log("Redirect URI: " + URI);

        WebAccountProvider wap =
            await WebAuthenticationCoreManager.FindAccountProviderAsync("https://login.microsoft.com", Authority);

        Logger.Log($"Found Web Account Provider for organizations: {wap.DisplayName}");

        //var accts = await WebAuthenticationCoreManager.FindAllAccountsAsync(wap);

        //Logger.Log($"Find All Accounts Status = {accts.Status}");

        //if (accts.Status == FindAllWebAccountsStatus.Success)
        //{
        //    foreach (var acct in accts.Accounts)
        //    {
        //        Logger.Log($"Account: {acct.UserName} {acct.State.ToString()}");
        //    }
        //}

        //var sap = await WebAuthenticationCoreManager.FindSystemAccountProviderAsync(wap.Id);
        //if (sap != null)
        //{
        //    string displayName = "Not Found";
        //    if (sap.User != null)
        //    {
        //        displayName = (string)await sap.User.GetPropertyAsync("DisplayName");
        //        Logger.Log($"Found system account provider {sap.DisplayName} with user {displayName} {sap.User.AuthenticationStatus.ToString()}");
        //    }
        //}

        Logger.Log("Web Account Provider: " + wap.DisplayName);

        string resource = "https://sts.mixedreality.azure.com";

        //var scope = "https://management.azure.com/user_impersonation";
        //WebTokenRequest wtr = new WebTokenRequest(wap, scope, "3c663152-fdf9-4033-963f-c398c21212d9");
        //WebTokenRequest wtr = new WebTokenRequest(wap, scope, "5c8c830a-4cf8-470e-ba0d-6d815feba800");
        //https://sts.mixedreality.azure.com/mixedreality.signin 

        WebTokenRequest wtr = new WebTokenRequest(wap, "https://sts.mixedreality.azure.com//.default", ClientId);
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
                //tokenResponse = await WebAuthenticationCoreManager.RequestTokenAsync(wtr);
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
            WebTokenRequestResult wtrr = null;
            try
            {
                if (account != null)
                    wtrr = await WebAuthenticationCoreManager.RequestTokenAsync(wtr, account);
                else
                    wtrr = await WebAuthenticationCoreManager.RequestTokenAsync(wtr);
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message);
            }
            Logger.Log("Interactive Token Response: " + wtrr.ResponseStatus.ToString());
            if (wtrr.ResponseError != null)
            {
                Logger.Log("Error Code: " + wtrr.ResponseError.ErrorCode.ToString());
                Logger.Log("Error Msg: " + wtrr.ResponseError.ErrorMessage.ToString());
                foreach (var errProp in wtrr.ResponseError.Properties)
                {
                    Logger.Log($"Error prop: ({errProp.Key}, {errProp.Value})");
                }
            }

            if (wtrr.ResponseStatus == WebTokenRequestStatus.Success)
            {
                accessToken = wtrr.ResponseData[0].Token;
                account = wtrr.ResponseData[0].WebAccount;
                var properties = wtrr.ResponseData[0].Properties;
                Username = account.UserName;
                Logger.Log($"Username = {Username}");
                var ras = await account.GetPictureAsync(WebAccountPictureSize.Size64x64);
                var stream = ras.AsStreamForRead();
                var br = new BinaryReader(stream);
                UserPicture = br.ReadBytes((int)stream.Length);

                Logger.Log("Access Token: " + accessToken);
            }
        }

        if (tokenResponse.ResponseStatus == WebTokenRequestStatus.Success)
        {
            foreach (var resp in tokenResponse.ResponseData)
            {
                var name = resp.WebAccount.UserName;
                accessToken = resp.Token;
                account = resp.WebAccount;
                Username = account.UserName;
                Logger.Log($"Username = {Username}");
                try
                {
                    var ras = await account.GetPictureAsync(WebAccountPictureSize.Size64x64);
                    var stream = ras.AsStreamForRead();
                    var br = new BinaryReader(stream);
                    UserPicture = br.ReadBytes((int)stream.Length);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Exception when reading image {ex.Message}");
                }
            }

            Logger.Log("Access Token: " + accessToken);
        }

        if (account != null && !string.IsNullOrEmpty(account.Id))
        {
            Store.SaveUser(UserIdKey, account.Id);
        }
#endif
        AADToken = accessToken;
        return new AADToken(AADToken);
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
