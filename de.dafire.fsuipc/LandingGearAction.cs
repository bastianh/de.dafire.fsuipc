using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ServiceModel.PeerResolvers;
using System.Text;
using System.Threading.Tasks;


namespace de.dafire.fsuipc
{
    [PluginActionId("de.dafire.fsuipc.landinggear")]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class GearsAction : PluginBase
    {
        private readonly FsIpcConnection _fsIpc = FsIpcConnection.GetInstance();
        private IconState _currentIconState = IconState.None;

        private enum IconState
        {
            None,
            InActive,
            Up,
            Down,
            Transit,
        }

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

        public GearsAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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
            _fsIpc.toggle_Gear();
            Logger.Instance.LogMessage(TracingLevel.INFO, "Gear Toggled");
        }

        public override void KeyReleased(KeyPayload payload)
        {
        }

        public override async void OnTick()
        {
            IconState newState = IconState.None;
            if (!_fsIpc.GearIsRetractable) newState = IconState.InActive;
            else
            {
                if (_fsIpc.GearIsInTransit) newState = IconState.Transit;
                else newState = _fsIpc.GearControlIsUp ? IconState.Up : IconState.Down;
            }
            if (_currentIconState == newState) return;
            _currentIconState = newState;
            await DrawIcon(newState);
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
        }

        private async Task DrawIcon(IconState state)
        {
            try
            {
                var bmp = Tools.GenerateGenericKeyImage(out Graphics graphics);
                var iconHeight = bmp.Height;
                var iconWidth = bmp.Width;
                var gearWidth = iconHeight / 20;
                var gearHeight = iconHeight / 10;

                var whitePen = new Pen(Color.White, 3);
                var greyPen = new Pen(Color.White, 3);
                var redBrush = new SolidBrush(Color.DarkRed);
                var greenBrush = new SolidBrush(Color.Green);

                var rectangles = new[]
                {
                    new Rectangle(iconWidth / 2 - gearWidth, iconHeight / 3 - gearHeight, gearWidth * 2,
                        gearHeight * 2),
                    new Rectangle(iconWidth / 6 * 2 - gearWidth, iconHeight / 7 * 4 - gearHeight, gearWidth * 2,
                        gearHeight * 2),
                    new Rectangle(iconWidth / 6 * 4 - gearWidth, iconHeight / 7 * 4 - gearHeight, gearWidth * 2,
                        gearHeight * 2),
                };

                switch (state)
                {
                    case IconState.None:
                        break;
                    case IconState.InActive:
                        graphics.DrawRectangles(greyPen, rectangles);
                        break;
                    case IconState.Up:
                        graphics.DrawRectangles(whitePen, rectangles);
                        break;
                    case IconState.Down:
                        graphics.FillRectangles(greenBrush, rectangles);
                        break;
                    case IconState.Transit:
                        graphics.FillRectangle(redBrush, iconWidth / 10, iconHeight/10, iconWidth - iconWidth / 5, iconHeight - iconHeight / 3);
                        graphics.DrawRectangles(whitePen, rectangles);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(state), state, null);
                }

                var imgBase64 = Tools.ImageToBase64(bmp, true);
                await Connection.SetImageAsync(imgBase64);

                graphics.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"DrawSymbolData Exception: {ex}");
            }
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