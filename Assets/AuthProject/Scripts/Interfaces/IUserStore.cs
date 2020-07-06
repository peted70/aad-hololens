public interface IUserStore
{
    void SaveUser(string key, string userId);
    string GetUserId(string key);

    void ClearUser(string key);
}
