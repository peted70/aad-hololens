using Microsoft.MixedReality.Toolkit;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

public class LogStringQueue
{
    const int MaxLines = 20;
    Queue<int> LineLengths = new Queue<int>(MaxLines);
    private StringBuilder _logStringBuilder = new StringBuilder();

    public void ClearLog()
    {
        _logStringBuilder.Clear();
        LineLengths.Clear();
    }

    public override string ToString()
    {
        return _logStringBuilder.ToString();
    }

    public void Log(string msg)
    {
        while (LineLengths.Count >= MaxLines)
        {
            int length = LineLengths.Dequeue();
            _logStringBuilder.Remove(0, length + 1);
        }

        LineLengths.Enqueue(msg.Length);
        _logStringBuilder.AppendLine(msg);
    }
}

public class FixedSizeQueue<T> : Queue<T>
{
    public int Limit { get; set; }

    public FixedSizeQueue(int limit) : base(limit)
    {
        Limit = limit;
    }

    public new void Enqueue(T item)
    {
        while (Count >= Limit)
        {
            Dequeue();
        }
        base.Enqueue(item);
    }
}

public abstract class BaseLoginProvider : BaseExtensionService, ILoginProvider
{
    public BaseLoginProvider(IAADLogger logger, IUserStore userStore, string clientId, string authority)
    {
        Logger = logger;
        ClientId = clientId;
        Authority = authority;
        Store = userStore;
    }

    private LogStringQueue logStrings = new LogStringQueue();

    public byte[] UserPicture { get; set; }

    public abstract string UserIdKey { get; }

    public string LogContent { get { return logStrings.ToString(); } }

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
        logStrings.ClearLog();
    }

    public void Log(string msg)
    {
        logStrings.Log(msg);
    }

    public abstract Task<IToken> LoginAsync();

    public abstract Task SignOutAsync();
}
