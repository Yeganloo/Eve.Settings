using Eve.Caching;

namespace Eve.Settings
{
    public class ManagerSettings
    {
        public ManagerSettings(ICacheProvider<string, object> cacheProvider = null)
        {
            this.CacheProvider = cacheProvider ?? new SimpleCacheProvider<object>();
        }

        public ICacheProvider<string, object> CacheProvider { get; set; }

        public string SettingsFolder { get; set; } = "Settings";

        public string SettingsFileExtentions { get; set; } = "*.json";

        public int UpdateDelay { get; set; } = 5000;
    }
}
