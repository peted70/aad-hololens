public interface ILogContext
{
    void ClearLog();

    void QueueLog(string msg, bool toConsole);
}