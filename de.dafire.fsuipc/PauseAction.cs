using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ServiceModel.PeerResolvers;
using System.Text;
using System.Threading.Tasks;


namespace de.dafire.fsuipc
{
    [PluginActionId("de.dafire.fsuipc.pause")]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class PauseAction : PluginBase
    {
        private readonly FsIpcConnection _fsIpc = FsIpcConnection.GetInstance();
        private bool _pauseActive = false;
        private bool _initialized = false;

        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.Connected = false;
                return instance;
            }

            [JsonProperty(PropertyName = "connected")]
            public bool Connected { get; set; }
        }

        #region Private Members

        private PluginSettings settings;

        #endregion

        public PauseAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }

            _fsIpc.RegisterButton();
            _fsIpc.ConnectionChanged += _fsIpc_ConnectionChanged;
            CheckSettings();
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
            _fsIpc.ConnectionChanged -= _fsIpc_ConnectionChanged;
            _fsIpc.UnRegisterButton();
        }

        private void CheckSettings()
        {
            if (settings != null && settings.Connected != _fsIpc.connected)
            {
                settings.Connected = _fsIpc.connected;
                SaveSettings();
            }
        }

        public override void KeyPressed(KeyPayload payload)
        {
            _fsIpc.toggle_Pause();
            Logger.Instance.LogMessage(TracingLevel.INFO, "Pause Toggled");
        }

        public override void KeyReleased(KeyPayload payload)
        {
        }

        public override async void OnTick()
        {
            if (_initialized && _pauseActive == _fsIpc.SimulationPaused) return;
            _initialized = true;
            _pauseActive = _fsIpc.SimulationPaused;
            if (_pauseActive) await Connection.SetImageAsync(Image.FromFile(@"Images\Actions\play.png"));
            else await Connection.SetImageAsync(Image.FromFile(@"Images\Actions\pause.png"));
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
        }

        #region Private Methods

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        private void _fsIpc_ConnectionChanged(object sender, bool connected)
        {
            CheckSettings();
        }

        #endregion
    }
}