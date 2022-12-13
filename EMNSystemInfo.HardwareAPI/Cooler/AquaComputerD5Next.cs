// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using HidSharp;

namespace EMNSystemInfo.HardwareAPI.Cooler
{
    public sealed class AquaComputerD5Next : ICooler
    {
        //Available Reports, found them by looking at the below methods
        //var test = dev.GetRawReportDescriptor();
        //var test2 = dev.GetReportDescriptor();

        // ID 1; Length 158; INPUT
        // ID 2; Length 11; OUTPUT
        // ID 3; Length 1025; <-- works FEATURE
        // ID 8; Length 1025; <-- works FEATURE
        // ID 12; Length 1025; <-- 0xC FEATURE

        private readonly byte[] _rawData = new byte[1025];
        private readonly HidStream _stream;

        public AquaComputerD5Next(HidDevice dev)
        {
            if (dev.TryOpen(out _stream))
            {
                //Reading output report instead of feature report, as the measurements are in the output report
                _stream.Read(_rawData);

                Name = $"AquaComputer D5Next";
            }
        }

        public double WaterTemperature { get; private set; }

        public double PumpRPM { get; private set; }

        public ushort FirmwareVersion { get; private set; }

        public string Name { get; }

        public bool IsWaterCoolingSystem => true;

        public CoolerType Type => CoolerType.AquaComputerD5Next;

        public void Close()
        {
            _stream.Close();
        }

        public void Update()
        {
            //Reading output report instead of feature report, as the measurements are in the output report
            _stream.Read(_rawData);
            WaterTemperature = (_rawData[88] | (_rawData[87] << 8)) / 100f; //Water Temp
            PumpRPM = (_rawData[117] | (_rawData[116] << 8)); //Pump RPM
        }
    }
}
