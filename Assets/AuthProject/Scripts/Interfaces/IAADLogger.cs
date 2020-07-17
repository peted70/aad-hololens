public interface IAADLogger
{
    void Log(string msg, bool toConsole = true);

    void Clear();
}
