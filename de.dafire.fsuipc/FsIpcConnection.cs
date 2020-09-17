using System;
using System.Collections.Generic;
using System.Timers;
using BarRaider.SdTools;
using FSUIPC;

namespace de.dafire.fsuipc
{
    public class FsIpcConnection
    {
 
        #region Singleton

        private static volatile FsIpcConnection _instance;

        private static readonly object Lock = new object();

        public static FsIpcConnection GetInstance()
        {
            if (_instance != null) return _instance;
            lock (Lock)
            {
                if (_instance == null)
                {
                    _instance = new FsIpcConnection();
                }
            }

            return _instance;
        }

        #endregion

        #region ButtonRegistration

        private int _connectedButtons = 0;
        private FsIpcConnection()
        {
            SetupConnectionTimer();
        }

        public void RegisterButton()
        {
            _connectedButtons += 1;
            Logger.Instance.LogMessage(TracingLevel.INFO, $"buttons " + _connectedButtons);
        }

        public void UnRegisterButton()
        {
            _connectedButtons -= 1;
            Logger.Instance.LogMessage(TracingLevel.INFO, $"buttons " + _connectedButtons);
        }

        #endregion

        #region Pause
        
        private readonly Offset<ushort> _simPausedOffset = new Offset<ushort>(0x0264);    
        private readonly Offset<ushort> _pauseControlOffset = new Offset<ushort>(0x0262);

        public bool SimulationPaused = false;
        
        public void toggle_Pause()
        {
            _pauseControlOffset.Value = (ushort) (SimulationPaused ? 0 : 1);
        }

        private void CheckPauseStatus()
        {
            SimulationPaused = _simPausedOffset.Value == 1;
        }
        
        #endregion
        
        #region LandingGear

        private enum LandingGearStatus
        {
            Up = 0,
            Down = 16383
        }
        
        private readonly Offset<byte> _gearType = new Offset<byte>(0x060c);
        private readonly Offset<int> _gearControl = new Offset<int>(0x0be8);
        private readonly Offset<int> _gearPositionNose = new Offset<int>(0x0bec);

        public bool GearIsRetractable = false;
        public bool GearControlIsUp = false;
        public bool GearIsInTransit = false;

        private void CheckGearStatus()
        {
            GearControlIsUp = _gearControl.Value == (int) LandingGearStatus.Up;
            GearIsRetractable = _gearType.Value == 1;
            GearIsInTransit = _gearControl.Value != _gearPositionNose.Value;
        }

        public void toggle_Gear()
        {
            _gearControl.Value = GearControlIsUp ? (int) LandingGearStatus.Down : (int) LandingGearStatus.Up;
        }

        #endregion

        
        #region MainTimer

        private Timer _mainTimer;

        private void SetupMainTimer()
        {
            _mainTimer = new Timer(250);
            _mainTimer.Elapsed += OnMainTimerEvent;
            _mainTimer.AutoReset = true;
            _mainTimer.Enabled = true;
        }

        private void OnMainTimerEvent(Object source, ElapsedEventArgs e)
        {
            FSUIPCConnection.Process();
            CheckGearStatus();
            CheckPauseStatus();
        }

        #endregion

        #region Connection

        private Timer _connectionTimer;

        public bool connected => FSUIPCConnection.IsOpen;
        public event EventHandler<bool> ConnectionChanged;

        private void SetupConnectionTimer()
        {
            _connectionTimer = new Timer(2000);
            _connectionTimer.Elapsed += OnConnectionTimerEvent;
            _connectionTimer.AutoReset = true;
            _connectionTimer.Enabled = true;
        }

        private void OnConnectionTimerEvent(Object source, ElapsedEventArgs e)
        {
            try
            {
                FSUIPCConnection.Open();
            }
            catch
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, "connection failed");
            }

            if (FSUIPCConnection.IsOpen)
            {
                _connectionTimer.Enabled = false;
                Logger.Instance.LogMessage(TracingLevel.INFO, "connected");
                ConnectionChanged?.Invoke(this, true);
                SetupMainTimer();
            }
        }

        #endregion
    }
}