using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Timers;
using BarRaider.SdTools;
using Newtonsoft.Json.Linq;

namespace GHubBatteryStatusPlugin.Internal
{
    internal class GHubReader
    {
        private const string SettingsFile = @"LGHUB\settings.db";
        private readonly string BatterySection = "percentage";
        private readonly string BatteryWarningSection = "warning";

        private static GHubReader instance = null;
        private static readonly object ObjLock = new object();

        private readonly Timer _timerRefreshStatus;
        private readonly string _fullPath;
        private Dictionary<string, GHubBatteryStats> _batteryStats;

        public static GHubReader Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                lock (ObjLock)
                {
                    return instance ?? (instance = new GHubReader());
                }
            }
        }

        private GHubReader()
        {
            _fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                SettingsFile);

            _timerRefreshStatus = new Timer
            {
                Interval = 10000
            };
            _timerRefreshStatus.Elapsed += TimerRefreshStats_Elapsed;
            _timerRefreshStatus.Start();

            RefreshStats();
        }

        public List<DeviceInfo> GetAllDevices()
        {
            if (_batteryStats == null || _batteryStats.Count == 0)
            {
                RefreshStats();
            }

            return _batteryStats.Keys.Select(name => new DeviceInfo() { Name = name }).ToList();
        }

        public GHubBatteryStats GetBatteryStats(string deviceName)
        {
            if (_batteryStats == null || !_batteryStats.ContainsKey(deviceName))
            {
                return null;
            }

            return _batteryStats[deviceName];
        }

        private void TimerRefreshStats_Elapsed(object sender, ElapsedEventArgs e)
        {
            RefreshStats();
        }

        private void RefreshStats()
        {
            try
            {
                _batteryStats = new Dictionary<string, GHubBatteryStats>();

                if (!File.Exists(_fullPath))
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"RefreshStats Error: Cannot find settings file at path: {_fullPath}");
                    _timerRefreshStatus.Stop();
                }

                var settings = ReadSettingsDb(_fullPath);
                if (settings != null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"{GetType()} RefreshStats Error: Could not read GHub settings");
                    return;
                }

                var properties = settings.Properties().Where(p => p.Name.Contains("battery")).ToList();
                foreach (var property in properties)
                {
                    var splitName = property.Name.Split('/');
                    if (splitName.Length != 3)
                    {
                        continue;
                    }

                    if (splitName[2] != BatterySection && splitName[2] != BatteryWarningSection)
                    {
                        continue;
                    }

                    if (_batteryStats.ContainsKey(splitName[1]) && splitName[2] == BatteryWarningSection)
                    {
                        continue;
                    }

                    var stats = property.Value.ToObject<GHubBatteryStats>();
                    _batteryStats[splitName[1]] = stats;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"RefreshStats Error: Failed to parse json: {ex}");
                _timerRefreshStatus.Stop();
            }
        }

        private JObject ReadSettingsDb(string fileName)
        {
            try
            {
                using (var con = new SQLiteConnection($"Data Source={fileName}"))
                {
                    con.Open();

                    const string query = "SELECT FILE FROM DATA ORDER BY _id DESC";
                    using (var cmd = new SQLiteCommand(query, con))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return JObject.Parse(reader.GetString(0));
                        }
                    }

                    Logger.Instance.LogMessage(TracingLevel.INFO, "Made it to this section with");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{GetType()} ReadSettingsDb Exception: {ex}");
            }

            return null;
        }
    }
}
