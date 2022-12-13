// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using EMNSystemInfo.HardwareAPI.CPU;
using Microsoft.Win32;
using System.Linq;

namespace EMNSystemInfo.HardwareAPI.GPU
{
    /// <summary>
    /// Class that repersents an individual Intel integrated GPU
    /// </summary>
    public class IntelIntegratedGPU : GPU
    {
        private readonly IntelCPU _intelCPU;
        private double? _powerSensor;

        /// <summary>
        /// Gets the GPU total power, in watts (W). This property is nullable.
        /// </summary>
        public double? TotalPower => _powerSensor;

        internal IntelIntegratedGPU(IntelCPU intelCPU, string deviceId)
        {
            Type = GPUType.IntelIntegratedGPU;

            _intelCPU = intelCPU;
            Initialize(deviceId);
            _gpuName = GetName(_d3dDeviceId);

            intelCPU.Update();
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override void Update()
        {
            base.Update();

            _powerSensor = _intelCPU.PowerSensors.Graphics;
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
