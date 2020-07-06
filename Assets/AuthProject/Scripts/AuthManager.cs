using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UnityMainThreadDispatcher))]
public class AuthManager : MonoBehaviour, ILogContext
{
    public string ClientId = "8f99f5cf-98a0-4029-bfd8-1aea31c2117d";
    public string TenantId = "70d30580-e80d-45a0-b37b-a369cdbe1185";
    public string Authority = "organizations";

    public TextMeshProUGUI DebugText;
    public RawImage userImage;
    public GameObject ScrollView;

    public TextMeshProUGUI StatusText;
    public TextMeshProUGUI UserText;
    public string ArrAccountId;

    public IUserStore UserStore { get; } = new UnityUserStore();
    public IAADLogger Logger { get; set; }
    public ILoginProvider CurrentLoginProvider { get; set; }
    public UnityMainThreadDispatcher Dispatcher { get; set; }

    public class AccountSettings
    {
        public string Id;
    }

    void Start()
    {
        Dispatcher = GetComponent<UnityMainThreadDispatcher>();
        Logger = new UnityAADLogger(this);

        LoginProviders.RegisterProviders(Logger, UserStore, ClientId, Authority, TenantId);

        TextAsset settings = (TextAsset)AssetDatabase.LoadAssetAtPath("Assets/arr-settings.json",
                                                                              typeof(TextAsset));
        if (settings)
        {
            AccountSettings set = (AccountSettings)JsonUtility.FromJson(settings.text, typeof(AccountSettings));
            ArrAccountId = set.Id;
        }
        else
        {
            Logger.Log("Warning, no arr settings file - won't be able to test token!");
        }
    }

    public void SetCurrentLoginProvider(string id)
    {
        CurrentLoginProvider = LoginProviders.GetProvider(id);
        UpdateUI();
    }

    public void HandleLogin()
    {
        //ClearLog();
        Log("Logging in with current Login Provider...");
        StartLogin(CurrentLoginProvider.LoginAsync).ContinueWith(t =>
        {
            ProcessResult(t);
        });
    }

    public void HandleLogout()
    {
        Log("Logging out with current Login Provider...");
        CurrentLoginProvider.SignOutAsync().ContinueWith(t =>
        {
            ProcessResult(t);
        });
    }

    public void HandleTest()
    {
        Log("Testing token...");
        TestTokenAsync(CurrentLoginProvider.AADToken, ArrAccountId).ContinueWith(t =>
        {
            if (t.Exception != null)
            {
                Log(t.Exception.Message);
            }
        });
    }

    public class StsResponse
    {
        public string AccessToken;
    }

    private async Task TestTokenAsync(string token, string arrAcountId)
    {
        if (string.IsNullOrEmpty(token))
        {
            Log("No valid token provided - can't test");
            return;
        }
        if (string.IsNullOrEmpty(arrAcountId))
        {
            Log("No Azure Remote Rendering Acount Id provided - can't test");
            return;
        }

        var http = new HttpClient();

        var acctId = arrAcountId;
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var resp = await http.GetAsync($"https://sts.mixedreality.azure.com/accounts/{acctId}/token");
        var content = await resp.Content.ReadAsStringAsync();
        Log(content);
        resp.EnsureSuccessStatusCode();

        var tkn = (StsResponse)JsonUtility.FromJson(content, typeof(StsResponse));

        if (tkn == null)
        {
            Log("Token object is null");
        }
        else
        {
            Log(tkn.AccessToken);
        }

        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tkn.AccessToken);

        var resp2 = await http.GetAsync($"https://remoterendering.eastus.mixedreality.azure.com/v1/accounts/{acctId}/sessions");

        resp2.EnsureSuccessStatusCode();
        Log("SUCCEEDED");
    }

    void ProcessResult(Task t)
    {
        Dispatcher.Enqueue(() =>
        {
            if (t.Exception != null)
            {
                t.Exception.InnerExceptions.ToList().ForEach(e => Log(e.Message));
            }
            else
            {
                Log("Completed successfully.");

                // Sync some values to the UI
                //
                UpdateUI();
            }
        });
    }

    private void UpdateUI()
    {
        // Synchronise the UI..
        DebugText.text = CurrentLoginProvider.LogContent;
        UserText.text = CurrentLoginProvider.Username;
        StatusText.text = CurrentLoginProvider.IsSignedIn ? "Signed In" : "Not Signed In";

        if (CurrentLoginProvider.UserPicture != null && CurrentLoginProvider.UserPicture.Length > 0)
        {
            var tex = new Texture2D(64, 64, TextureFormat.BGRA32, false);
            tex.LoadImage(CurrentLoginProvider.UserPicture);
            tex.Apply();
            userImage.texture = tex;
        }
    }

    private async Task ListAccountsAsync(string token)
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await http.GetAsync("https://management.azure.com/providers/Microsoft.MixedReality/operations");
        resp.EnsureSuccessStatusCode();

        var str = await resp.Content.ReadAsStringAsync();
        Log(str);
    }

    public async Task<IToken> StartLogin(Func<Task<IToken>> LoginProvider)
    {
        IToken accessToken = null;
        //await Progress.OpenAsync();
        try
        {
            accessToken = await LoginProvider();
        }
        catch (AggregateException ex)
        {
            foreach (var exc in ex.InnerExceptions)
            {
                Log(exc.Message);
            }
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
        finally
        {
            //await Progress.CloseAsync();
        }
        return accessToken;
    }

    void Log(string text)
    {
        DebugText.text += "\n" + text;
        CurrentLoginProvider.Log("\n" + text);
        Debug.Log(text);
        Canvas.ForceUpdateCanvases();
        var scrollRect = ScrollView.GetComponent<ScrollRect>();
        scrollRect.verticalNormalizedPosition = 0.0f;
    }

    public void ClearLog()
    {
        DebugText.text = string.Empty;
    }

    public void QueueLog(string log)
    {
        Dispatcher.Enqueue(() =>
        {
            Log(log);
        });
    }
}

public static class LoginProviders
{
    private static Dictionary<string, ILoginProvider> Providers { get; } = new Dictionary<string, ILoginProvider>();

    public static void RegisterProviders(IAADLogger logger, IUserStore userStore, string clientId, string authority, string tenantId)
    {
        // Register the login providers
        //
        Providers.Add("WAM", new WAMLoginProvider(logger, userStore, clientId, authority));
        Providers.Add("MSAL", new MSALLoginProvider(logger, userStore, clientId, authority, tenantId));
        Providers.Add("ADAL", new ADALLoginProvider(logger, userStore, clientId, authority, tenantId));
        Providers.Add("WAMWAB", new WAMWABLoginProvider(logger, userStore, clientId, authority));
        Providers.Add("WAB", new WABLoginProvider(logger, userStore, clientId, authority));
        Providers.Add("WAP", new WAPLoginProvider(logger, userStore, clientId, authority));
    }

    public static IEnumerable<ILoginProvider> GetProviders()
    {
        return Providers.Select(p => p.Value);
    }

    public static ILoginProvider GetProvider(string id)
    {
        if (Providers.TryGetValue(id, out ILoginProvider value))
            return value;
        return null;
    }
}
