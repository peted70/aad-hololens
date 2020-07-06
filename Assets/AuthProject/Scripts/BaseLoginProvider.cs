using Microsoft.MixedReality.Toolkit;
using System.Threading.Tasks;

public abstract class BaseLoginProvider : BaseExtensionService, ILoginProvider
{
    public BaseLoginProvider(IAADLogger logger, IUserStore userStore, string clientId, string authority)
    {
        Logger = logger;
        ClientId = clientId;
        Authority = authority;
        Store = userStore;
        LogContent = string.Empty;
    }

    public byte[] UserPicture { get; set; }

    public abstract string UserIdKey { get; }

    public string LogContent { get; private set; }

    public string AADToken { get; protected set; }

    public string AccessToken { get; protected set; }

    public string Username { get; protected set; }

    public bool IsSignedIn { get { return !string.IsNullOrEmpty(AADToken); } }

    public abstract string Description { get; }
    public abstract string ProviderName { get; }

    protected string Authority { get; private set; }

    protected string ClientId { get; private set; }

    protected IAADLogger Logger { get; private set; }

    protected IUserStore Store { get; }

    public void ClearLog()
    {
        LogContent = string.Empty;
    }

    public void Log(string msg)
    {
        LogContent += msg;
    }

    public abstract Task<IToken> LoginAsync();

    public abstract Task SignOutAsync();
}
