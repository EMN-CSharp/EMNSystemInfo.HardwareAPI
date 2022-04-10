using EMNSystemInfo.HardwareAPI.CPU;
using Microsoft.Win32;
using System;
using System.Linq;

namespace EMNSystemInfo.HardwareAPI.GPU
{
    public class IntelIntegratedGPU : GPU
    {
        private readonly IntelCPU _intelCPU;
        private double? _powerSensor;

        public double? TotalPower => _powerSensor;

        internal IntelIntegratedGPU(IntelCPU intelCPU, string deviceId)
        {
            Type = GPUType.IntelIntegratedGPU;

            _intelCPU = intelCPU;
            _d3dDeviceId = deviceId;

            _gpuName = GetName(_d3dDeviceId);

            intelCPU.Update();
        }

        public override void Update()
        {
            base.Update();

            _powerSensor = _intelCPU.PowerSensors[2 /*Graphics*/]?.Value;
        }

        private static string GetName(string deviceId)
        {
            var path = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\" + D3DDisplayDevice.GetActualDeviceIdentifier(deviceId);

            if (Registry.GetValue(path, "DeviceDesc", null) is string deviceDesc)
            {
                return deviceDesc.Split(';').Last();
            }

            return "Intel Integrated Graphics";
        }
    }
}
