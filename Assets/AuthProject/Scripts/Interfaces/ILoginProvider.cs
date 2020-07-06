using System.Threading.Tasks;

public interface ILoginProvider
{
    string UserIdKey { get; }
    string LogContent { get; }
    string AADToken { get; }
    string AccessToken { get; }
    string Username { get; }
    bool IsSignedIn { get; }

    byte[] UserPicture { get; set; }

    Task<IToken> LoginAsync();

    Task SignOutAsync();

    void Log(string msg);
    void ClearLog();
    string Description { get; }
    string ProviderName { get; }
}
