using UnityEngine;

public class UnityUserStore : IUserStore
{
    public void ClearUser(string key)
    {
        PlayerPrefs.DeleteKey(key);
    }

    public string GetUserId(string key)
    {
        return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : string.Empty;
    }

    public void SaveUser(string key, string userId)
    {
        PlayerPrefs.SetString(key, userId);
    }
}
