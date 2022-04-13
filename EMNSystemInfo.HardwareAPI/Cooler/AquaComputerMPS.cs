// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using HidSharp;

namespace EMNSystemInfo.HardwareAPI.Cooler
{
    public sealed class AquaComputerMPS : ICooler
    {
        private const byte MPS_REPORT_ID = 0x2;

        private readonly byte[] _rawData = new byte[64];
        private readonly HidStream _stream;
        
        private ushort _externalTemperature;

        public AquaComputerMPS(HidDevice dev)
        {
            if (dev.TryOpen(out _stream))
            {
                do
                {
                    _rawData[0] = MPS_REPORT_ID;
                    _stream.GetFeature(_rawData);
                }
                while (_rawData[0] != MPS_REPORT_ID);

                Name = "AquaComputer MPS";
                FirmwareVersion = ExtractFirmwareVersion();
            }
        }

        public double? ExternalTemperature { get; private set; }

        public double InternalWaterTemperature { get; private set; }

        public double WaterFlow { get; private set; }

        public ushort FirmwareVersion { get; private set; }

        public string Name { get; }

        public bool IsWaterCoolingSystem => true;

        public CoolerType Type => CoolerType.AquaComputerMPS;

        public void Close()
        {
            _stream.Close();
        }

        public void Update()
        {
            _rawData[0] = MPS_REPORT_ID;
            _stream.GetFeature(_rawData);

            if (_rawData[0] != MPS_REPORT_ID)
                return;


            WaterFlow = BitConverter.ToUInt16(_rawData, MPSDataIndexes.PumpFlow) / 10f;

            _externalTemperature = BitConverter.ToUInt16(_rawData, MPSDataIndexes.ExternalTemperature);
            //sensor reading returns Int16.MaxValue (32767), when not connected
            if (_externalTemperature != short.MaxValue)
            {
                ExternalTemperature = _externalTemperature / 100f;
            }
            else
            {
                ExternalTemperature = null;
            }

            InternalWaterTemperature = BitConverter.ToUInt16(_rawData, MPSDataIndexes.InternalWaterTemperature) / 100f;
        }

        private ushort ExtractFirmwareVersion()
        {
            return BitConverter.ToUInt16(_rawData, 3);
        }

        private sealed class MPSDataIndexes
        {
            public const int ExternalTemperature = 43;
            public const int InternalWaterTemperature = 45;
            public const int PumpFlow = 35;
        }
    }
}
