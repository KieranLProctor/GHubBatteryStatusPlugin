using System.Collections.Generic;
using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using GHubBatteryStatusPlugin.Internal;

namespace GHubBatteryStatusPlugin
{
    [PluginActionId("com.gerenuk.ghub-battery-status")]
    public class PluginAction : KeypadBase
    {
        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                var instance = new PluginSettings
                {
                    Devices = null,
                    Device = null,
                    OutputFileName = string.Empty,
                    InputString = string.Empty
                };
                return instance;
            }

            [JsonProperty(PropertyName = "devices")]
            public List<DeviceInfo> Devices { get; set; }

            [JsonProperty(PropertyName = "device")]
            public string Device { get; set; }

            [FilenameProperty]
            [JsonProperty(PropertyName = "outputFileName")]
            public string OutputFileName { get; set; }

            [JsonProperty(PropertyName = "inputString")]
            public string InputString { get; set; }
        }

        #region Private Members

        private readonly PluginSettings _settings;

        #endregion

        public PluginAction(ISDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                _settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                _settings = payload.Settings.ToObject<PluginSettings>();
            }

            #region Event Init
            Connection.OnApplicationDidLaunch += Connection_OnApplicationDidLaunch;
            Connection.OnApplicationDidTerminate += Connection_OnApplicationDidTerminate;
            Connection.OnTitleParametersDidChange += Connection_OnTitleParametersDidChange;
            Connection.OnSendToPlugin += Connection_OnSendToPlugin;
            Connection.OnPropertyInspectorDidAppear += Connection_OnPropertyInspectorDidAppear;
            Connection.OnPropertyInspectorDidDisappear += Connection_OnPropertyInspectorDidDisappear;
            Connection.OnDeviceDidConnect += Connection_OnDeviceDidConnect;
            Connection.OnDeviceDidDisconnect += Connection_OnDeviceDidDisconnect;
            #endregion
        }

        #region Events
        private void Connection_OnApplicationDidLaunch(object sender,
            BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.ApplicationDidLaunch> e)
        {

        }

        private void Connection_OnApplicationDidTerminate(object sender,
            BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.ApplicationDidTerminate> e)
        {

        }

        private void Connection_OnTitleParametersDidChange(object sender,
            BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.TitleParametersDidChange> e)
        {

        }

        private void Connection_OnSendToPlugin(object sender,
            BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.SendToPlugin> e)
        {

        }

        private void Connection_OnPropertyInspectorDidAppear(object sender,
            BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.PropertyInspectorDidAppear> e)
        {
            _settings.Devices = GHubReader.Instance.GetAllDevices();
            SaveSettings();
        }

        private void Connection_OnPropertyInspectorDidDisappear(object sender,
            BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.PropertyInspectorDidDisappear> e)
        {

        }

        private void Connection_OnDeviceDidConnect(object sender,
            BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.DeviceDidConnect> e)
        {

        }

        private void Connection_OnDeviceDidDisconnect(object sender,
            BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<BarRaider.SdTools.Events.DeviceDidDisconnect> e)
        {

        }
        #endregion

        public override void Dispose()
        {
            #region Event Dispose
            Connection.OnApplicationDidLaunch -= Connection_OnApplicationDidLaunch;
            Connection.OnApplicationDidTerminate -= Connection_OnApplicationDidTerminate;
            Connection.OnTitleParametersDidChange -= Connection_OnTitleParametersDidChange;
            Connection.OnSendToPlugin -= Connection_OnSendToPlugin;
            Connection.OnPropertyInspectorDidAppear -= Connection_OnPropertyInspectorDidAppear;
            Connection.OnPropertyInspectorDidDisappear -= Connection_OnPropertyInspectorDidDisappear;
            Connection.OnDeviceDidConnect -= Connection_OnDeviceDidConnect;
            Connection.OnDeviceDidDisconnect -= Connection_OnDeviceDidDisconnect;
            #endregion

            Logger.Instance.LogMessage(TracingLevel.INFO, "Destructor called");
        }

        public override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");
        }

        public override void KeyReleased(KeyPayload payload) { }

        public override async void OnTick()
        {
            var stats = GHubReader.Instance.GetBatteryStats(_settings.Device);
            if (stats != null)
            {
                // Need to init something here?
                return;
            }

            var title = $"{(int)stats.Percentage}%";
            await Connection.SetTitleAsync(title);
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(_settings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(_settings));
        }

        #endregion
    }
}