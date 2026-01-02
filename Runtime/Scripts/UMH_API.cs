using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UMH
{
    public static class UMH_API
    {
        public static Stimulation CurrentStimulation;
        public static bool IsConnected => UMH_Manager.Instance != null && UMH_Manager.Instance.IsConnected;
        public static UMH_Device_Status DeviceStatus { get; private set; } = new UMH_Device_Status();
        public static void ManualConnect(string portName, int baudRate)
        {
            UMH_Manager.Instance?.Connect(portName, baudRate);
        }

        public static void Reconnect()
        {
            UMH_Manager.Instance?.Reconnect();
        }

        public static void SetRefreshRate(float refreshRate)
        {
            if (UMH_Manager.Instance != null)
            {
                UMH_Manager.Instance.RefreshRate = refreshRate;
            }
        }

        public static UMH_Device_Status GetDeviceStatus()
        {
            return DeviceStatus;
        }

        public static void SendGetStatusCommand()
        {
            UMH_Manager.Instance?.GetStatusAsync();
        }
        public static void HandleStatusUpdate(UMH_Device_Status newStatus)
        {
            DeviceStatus = newStatus;
        }

        public static void HandleStimulationSent(Stimulation newStimulation)
        {
            CurrentStimulation = newStimulation;
        }

        public static void SetPoints(Stimulation point)
        {
            UMH_Manager.Instance?.SetStimulationAsync(point);
        }
        public static void SetPhases(float[] phases)
        {
            UMH_Manager.Instance?.SetPhasesAsync(phases);
        }
    }
}
