using Eve.Caching;

namespace Eve.Settings
{
    public class ManagerSettings
    {

        public ICacheProvider<string, object> CacheProvider { get; set; }

        public string SettingsFolder { get; set; }

        public string SettingsFileExtentions { get; set; } = "*.json";

        public int UpdateDelay { get; set; } = 5000;
    }
}
