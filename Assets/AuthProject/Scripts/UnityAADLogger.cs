public class UnityAADLogger : IAADLogger
{
    private readonly ILogContext context;

    public UnityAADLogger(ILogContext context)
    {
        this.context = context;
    }

    public void Clear()
    {
        context.ClearLog();
    }

    public void Log(string msg, bool toConsole = true)
    {
        context.QueueLog(msg, toConsole);
    }
}
