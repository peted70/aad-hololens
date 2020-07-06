using System.Threading.Tasks;

#if ENABLE_WINMD_SUPPORT
using Windows.UI.ApplicationSettings;
#endif

public class WAPLoginProvider : BaseLoginProvider
{
    public WAPLoginProvider(IAADLogger logger, IUserStore userStore, string clientId, string authority) : 
        base(logger, userStore, clientId, authority)
    {
    }

    public override string UserIdKey
    {
        get
        {
            return "UserIdKeyWAP";
        }
    }

    public override string Description => $"Use the AccountsSettingsPane to connect your Universal Windows Platform (UWP) app to external identity providers";

    public override string ProviderName => $"WindowsAccountProvider";

    public override async Task<IToken> LoginAsync()
    {
        Logger.Log("Loggin in with WindowsAccountProvider...");

        await ChooseFromAccountsAsync();
        return null;
    }

    public async Task ChooseFromAccountsAsync()
    {
#if ENABLE_WINMD_SUPPORT
        //await AccountsSettingsPane.ShowAddAccountAsync();
        AccountsSettingsPane.Show();
#endif
    }

    public async override Task SignOutAsync()
    {
        Username = string.Empty;
        AADToken = string.Empty;
        AccessToken = string.Empty;
    }
}
