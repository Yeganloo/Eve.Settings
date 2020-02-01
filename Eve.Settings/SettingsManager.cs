using Eve.Caching;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Timers;

namespace Eve.Settings
{
    public class SettingsManager : IDisposable
    {
        private readonly FileSystemWatcher watcher = null;
        private readonly string _SettingsBaseAddress;
        private const string _DefaultSettingsKey = "Settings";
        private Timer EventSchedule;

        private readonly ICacheProvider<string, object> _Cache;
        private readonly Dictionary<int, string> Names;
        private readonly Dictionary<string, MetaData> SettingsInfoes;

        /// <summary>
        /// Remember a pair of Type and ModuleName
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <exception cref="System.ArgumentException">
        ///     Type Already Registered!
        /// </exception>
        public void RememberMe<T>(string name)
        {
            RememberMe(typeof(T), name);
        }

        /// <summary>
        /// Remember a pair of Type and ModuleName
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <exception cref="System.ArgumentException">
        ///     Type Already Registered!
        /// </exception>
        public void RememberMe(Type type, string name)
        {
            lock (Names)
            {
                try
                {
                    Names.Add(type.GetHashCode(), name);
                }
                catch
                {
                    Names[type.GetHashCode()] = name;
                }
            }
        }

        public SettingsManager(ManagerSettings settings)
        {
            settings.SettingsFolder = string.IsNullOrEmpty(settings.SettingsFolder) ? "Settings" : settings.SettingsFolder;

            _SettingsBaseAddress = $"{Path.GetDirectoryName(typeof(SettingsManager).Assembly.Location)}{Path.DirectorySeparatorChar}{settings.SettingsFolder}{Path.DirectorySeparatorChar}";

            EventSchedule = new Timer(settings.UpdateDelay);
            EventSchedule.AutoReset = false;
            EventSchedule.Elapsed += EventSchedule_Elapsed;


            watcher = new FileSystemWatcher(_SettingsBaseAddress, settings.SettingsFileExtentions);
            watcher.Changed += Settings_Changed;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.IncludeSubdirectories = false;
            watcher.EnableRaisingEvents = true;

            _Cache = settings.CacheProvider;
            SettingsInfoes = new Dictionary<string, MetaData>();
            Names = new Dictionary<int, string>();
        }

        private void EventSchedule_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (EventSchedule)
            {
                foreach (var i in SettingsInfoes)
                {
                    if (i.Value.Updated && _Cache.HasKey(_DefaultSettingsKey, i.Key))
                    {
                        ReloadSettings(i.Key, i.Value.SettingsType);
                        i.Value.OnChange?.Invoke(_Cache.Get(_DefaultSettingsKey, i.Key, i.Value.SettingsType));
                    }
                    i.Value.Updated = false;
                }
            }
        }

        private void Settings_Changed(object sender, FileSystemEventArgs e)
        {
            lock (EventSchedule)
            {
                EventSchedule.Stop();
                var module = e.Name.Remove(e.Name.Length - 5, 5);
                if (SettingsInfoes.ContainsKey(module))
                {
                    var info = SettingsInfoes[module];
                    info.Updated = true;
                    this.ReloadSettings(module, info.SettingsType);
                    info.OnChange?.Invoke(_Cache.Get(_DefaultSettingsKey, module, info.SettingsType));
                }
                EventSchedule.Start();
            }
        }

        public ST GetSettings<ST>()
        {
            return GetSettings<ST>(Names[typeof(ST).GetHashCode()]);
        }

        public ST GetSettings<ST>(string moduleName)
        {
            return (ST)GetSettings(typeof(ST), moduleName);
        }

        public object GetSettings(Type type)
        {
            return GetSettings(type, Names[type.GetHashCode()]);
        }

        public object GetSettings(Type type, string moduleName)
        {
            if (!_Cache.HasKey(_DefaultSettingsKey, moduleName))
            {
                ReloadSettings(moduleName, type);
                lock (SettingsInfoes)
                {
                    SettingsInfoes.Add(moduleName, new MetaData { OnChange = null, SettingsType = type });
                }
            }
            return _Cache.Get(_DefaultSettingsKey, moduleName, type);
        }

        public void RegisterEvent(string moduleName, Action<object> action)
        {
            lock (SettingsInfoes)
            {
                if (SettingsInfoes.ContainsKey(moduleName))
                    SettingsInfoes[moduleName].OnChange = SettingsInfoes[moduleName].OnChange + action;
            }
        }

        public void RemoveEvent(string moduleName, Action<object> action)
        {
            lock (SettingsInfoes)
            {
                if (SettingsInfoes.ContainsKey(moduleName))
                    SettingsInfoes[moduleName].OnChange -= action;
            }
        }

        public void SaveSettings<T>(T settings)
        {
            SaveSettings(Names[typeof(T).GetHashCode()], settings);
        }

        public void SaveSettings<T>(string moduleName, T settings)
        {
            RememberMe<T>(moduleName);
            lock (_Cache)
            {
                using (var f = File.Open($"{_SettingsBaseAddress}{moduleName}.json", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                {
                    f.Position = 0;
                    var tmp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(settings));
                    f.Write(tmp, 0, tmp.Length);
                    f.Flush();
                    f.SetLength(f.Position);
                }
                _Cache.Cache(_DefaultSettingsKey, moduleName, settings);
            }
        }

        private void ReloadSettings(string moduleName, Type type)
        {
            lock (_Cache)
            {
                using (var f = new StreamReader(File.Open($"{_SettingsBaseAddress}{moduleName}.json", FileMode.Open, FileAccess.Read, FileShare.Read)))
                    _Cache.Cache(_DefaultSettingsKey, moduleName, JsonConvert.DeserializeObject(f.ReadToEnd(), type));
            }
        }

        public void Dispose()
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
    }
}
