using System.IO;
using UnityEngine;

public partial class AADLoginProviders
{
    internal class FileTokenCache : Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCache
    {
        private static readonly object FileLock = new object();
        private IAADLogger _logger;
        private string filePath;

        public FileTokenCache(IAADLogger logger, string filename = "TokenCache.dat")
        {
            _logger = logger;
            filePath = Application.persistentDataPath + Path.DirectorySeparatorChar + filename;

            AfterAccess = OnAfterAccess;
            BeforeAccess = OnBeforeAccess;
        }

        private void OnBeforeAccess(Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCacheNotificationArgs args)
        {
            lock (FileLock)
            {
                if (File.Exists(filePath))
                {
                    var bytes = File.ReadAllBytes(filePath);
                    _logger.Log($"Token Cache read bytes from disk");

                    // Tell ADAL about our own cached data so it can sync it's in-memory version..
                    DeserializeAdalV3(bytes);
                    _logger.Log($"Token Cache deserialized {bytes.Length} bytes");
                }
            }
        }

        private void OnAfterAccess(Microsoft.IdentityModel.Clients.ActiveDirectory.TokenCacheNotificationArgs args)
        {
            if (HasStateChanged)
            {
                lock (FileLock)
                {
                    // TODO: check this data is not plain text
                    byte[] bytes = SerializeAdalV3();
                    _logger.Log($"Token Cache serialized bytes: {bytes.Length} bytes");
                    File.WriteAllBytes(filePath, bytes);
                    _logger.Log($"Token Cache written bytes to disk");
                }
            }
        }

        public override void Clear()
        {
            base.Clear();
            lock (FileLock)
            {
                File.Delete(filePath);
            }
        }
    }
}
