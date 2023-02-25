using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using Microsoft.Data.Sqlite;

namespace GHubBatteryStatusPlugin.Internal
{
    internal class GHubReader
    {
        private readonly string _settingsFile = @"LGHUB\settings.db";
        private readonly string _batterySection = "percentage";
        private readonly string _batteryWarningSection = "warning";

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
                _settingsFile);

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

            return _batteryStats.Keys.Select(s => new DeviceInfo() { Name = s }).ToList();
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
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} RefreshStats Error: Could not read GHub settings");
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

                    if (splitName[2] != _batterySection && splitName[2] != _batteryWarningSection)
                    {
                        continue;
                    }

                    if (_batteryStats.ContainsKey(splitName[1]) && splitName[2] == _batteryWarningSection)
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
                using (var con = new SqliteConnection(fileName))
                {
                    con.Open();

                    const string query = "SELECT FILE FROM DATA ORDER BY _id DESC";
                    using (var cmd = new SqliteCommand(query, con))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return JObject.Parse(reader.GetString(0));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"{this.GetType()} ReadSettingsDB Exception: {ex}");
            }

            return null;
        }
    }
}
