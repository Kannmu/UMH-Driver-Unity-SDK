using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UMH
{
    public static class UMH_API
    {
        // ================================================= 
        // Connection
        // ================================================= 
        public static bool IsConnected => UMH_Manager.Instance != null && UMH_Manager.Instance.IsConnected;
        
        public static void ManualConnect(string portName, int baudRate)
        {
            UMH_Manager.Instance?.Connect(portName, baudRate);
        }

        public static void Reconnect()
        {
            UMH_Manager.Instance?.Reconnect();
        }

        // ================================================= 
        // Set Parameters
        // ================================================= 
        public static void SetRefreshRate(float refreshRate)
        {
            if (UMH_Manager.Instance != null)
            {
                UMH_Manager.Instance.RefreshRate = refreshRate;
            }
        }

        // ================================================= 
        // Device Configuration
        // ================================================= 
        public static void SetDeviceConfig(UMH_Device_Config config)
        {
            UMH_Device.DeviceConfig = config;
        }
        public static UMH_Device_Config GetDeviceConfig()
        {
            return UMH_Device.DeviceConfig;
        }

        // ================================================= 
        // Device Status
        // ================================================= 
        public static void SetDeviceStatus(UMH_Device_Status status)
        {
            UMH_Device.DeviceStatus = status;
        }
        public static UMH_Device_Status GetDeviceStatus()
        {
            return UMH_Device.DeviceStatus;
        }
        public static void SendGetStatusCommand()
        {
            _ = UMH_Manager.Instance?.GetStatusAsync();
        }
        
        // ================================================= 
        // Get Device Information
        // ================================================= 
        public static Vector2[] GetArrayConnerPoints()
        {
            return UMH_Array.GetArrayConnerPoints();
        }
        
        // ================================================= 
        // Stimulation & Phases Control
        // ================================================= 
        public static Stimulation GetCurrentStimulation()
        {
            return UMH_Stimulation.CurrentStimulation;
        }
        public static void SetStimulation(Stimulation stimulation)
        {
            _ = SetStimulationAsync(stimulation);
        }
        
        public static void SetPhases(float[] phases)
        {
            _ = SetPhasesAsync(phases);
        }

        // ================================================= 
        // Async Stimulation & Phases Control
        // ================================================= 
        public static async Task SetStimulationAsync(Stimulation stimulation)
        {
            if (UMH_Manager.Instance != null)
                await UMH_Manager.Instance.SetStimulationAsync(stimulation);
        }
        
        public static async Task SetPhasesAsync(float[] phases)
        {
            if (UMH_Manager.Instance != null)
                await UMH_Manager.Instance.SetPhasesAsync(phases);
        }
    }
}
