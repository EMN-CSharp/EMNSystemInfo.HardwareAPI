// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using HidSharp;

namespace EMNSystemInfo.HardwareAPI.Cooler
{
    internal sealed class AquaComputerOcto : ICooler
    {
        private double? _inputVoltage;

        private readonly byte[] _rawData = new byte[1025];
        private readonly double?[] _rpmSensors = new double?[8];
        private readonly HidStream _stream;
        private readonly double?[] _temperatures = new double?[4];
        private readonly double?[] _voltages = new double?[8];
        private readonly double?[] _currents = new double?[8];
        private readonly double?[] _powers = new double?[8];

        public AquaComputerOcto(HidDevice dev)
        {
            if (dev.TryOpen(out _stream))
            {
                //Reading output report instead of feature report, as the measurements are in the output report
                _stream.Read(_rawData);

                Name = $"OCTO";
                FirmwareVersion = GetConvertedValue(OctoDataIndexes.FIRMWARE_VERSION).GetValueOrDefault(0);
            }
        }

        public string Name { get; }

        public bool IsWaterCoolingSystem => true;

        public CoolerType Type => CoolerType.AquaComputerOcto;

        public ushort FirmwareVersion { get; private set; }

        public double? InputVoltage => _inputVoltage;

        public double?[] FanSpeeds => _rpmSensors;

        public double?[] Temperatures => _temperatures;

        public double?[] Voltages => _voltages;

        public double?[] Currents => _currents;

        public double?[] Powers => _powers;

        public void Close()
        {
            _stream.Close();
        }

        public void Update()
        {
            //Reading output report instead of feature report, as the measurements are in the output report
            _stream.Read(_rawData);

            _temperatures[0] = GetConvertedValue(OctoDataIndexes.TEMP_1) / 100d; // Temp 1
            _temperatures[1] = GetConvertedValue(OctoDataIndexes.TEMP_2) / 100d; // Temp 2
            _temperatures[2] = GetConvertedValue(OctoDataIndexes.TEMP_3) / 100d; // Temp 3
            _temperatures[3] = GetConvertedValue(OctoDataIndexes.TEMP_4) / 100d; // Temp 4

            _rpmSensors[0] = GetConvertedValue(OctoDataIndexes.FAN_SPEED_1); // Fan 1 speed
            _rpmSensors[1] = GetConvertedValue(OctoDataIndexes.FAN_SPEED_2); // Fan 2 speed
            _rpmSensors[2] = GetConvertedValue(OctoDataIndexes.FAN_SPEED_3); // Fan 3 speed
            _rpmSensors[3] = GetConvertedValue(OctoDataIndexes.FAN_SPEED_4); // Fan 4 speed
            _rpmSensors[4] = GetConvertedValue(OctoDataIndexes.FAN_SPEED_5); // Fan 5 speed
            _rpmSensors[5] = GetConvertedValue(OctoDataIndexes.FAN_SPEED_6); // Fan 6 speed
            _rpmSensors[6] = GetConvertedValue(OctoDataIndexes.FAN_SPEED_7); // Fan 7 speed
            _rpmSensors[7] = GetConvertedValue(OctoDataIndexes.FAN_SPEED_8); // Fan 8 speed

            _inputVoltage = GetConvertedValue(OctoDataIndexes.VOLTAGE) / 100d; // Input voltage
            _voltages[0] = GetConvertedValue(OctoDataIndexes.FAN_VOLTAGE_1) / 100d; // Fan 1 voltage
            _voltages[1] = GetConvertedValue(OctoDataIndexes.FAN_VOLTAGE_2) / 100d; // Fan 2 voltage
            _voltages[2] = GetConvertedValue(OctoDataIndexes.FAN_VOLTAGE_3) / 100d; // Fan 3 voltage
            _voltages[3] = GetConvertedValue(OctoDataIndexes.FAN_VOLTAGE_4) / 100d; // Fan 4 voltage
            _voltages[4] = GetConvertedValue(OctoDataIndexes.FAN_VOLTAGE_5) / 100d; // Fan 5 voltage
            _voltages[5] = GetConvertedValue(OctoDataIndexes.FAN_VOLTAGE_6) / 100d; // Fan 6 voltage
            _voltages[6] = GetConvertedValue(OctoDataIndexes.FAN_VOLTAGE_7) / 100d; // Fan 7 voltage
            _voltages[7] = GetConvertedValue(OctoDataIndexes.FAN_VOLTAGE_8) / 100d; // Fan 8 voltage

            _currents[0] = GetConvertedValue(OctoDataIndexes.FAN_CURRENT_1) / 1000d; // Fan 1 current
            _currents[1] = GetConvertedValue(OctoDataIndexes.FAN_CURRENT_2) / 1000d; // Fan 2 current
            _currents[2] = GetConvertedValue(OctoDataIndexes.FAN_CURRENT_3) / 1000d; // Fan 3 current
            _currents[3] = GetConvertedValue(OctoDataIndexes.FAN_CURRENT_4) / 1000d; // Fan 4 current
            _currents[4] = GetConvertedValue(OctoDataIndexes.FAN_CURRENT_5) / 1000d; // Fan 5 current
            _currents[5] = GetConvertedValue(OctoDataIndexes.FAN_CURRENT_6) / 1000d; // Fan 6 current
            _currents[6] = GetConvertedValue(OctoDataIndexes.FAN_CURRENT_7) / 1000d; // Fan 7 current
            _currents[7] = GetConvertedValue(OctoDataIndexes.FAN_CURRENT_8) / 1000d; // Fan 8 current

            _powers[0] = GetConvertedValue(OctoDataIndexes.FAN_POWER_1) / 100d; // Fan 1 power
            _powers[1] = GetConvertedValue(OctoDataIndexes.FAN_POWER_2) / 100d; // Fan 2 power
            _powers[2] = GetConvertedValue(OctoDataIndexes.FAN_POWER_3) / 100d; // Fan 3 power
            _powers[3] = GetConvertedValue(OctoDataIndexes.FAN_POWER_4) / 100d; // Fan 4 power
            _powers[4] = GetConvertedValue(OctoDataIndexes.FAN_POWER_5) / 100d; // Fan 5 power
            _powers[5] = GetConvertedValue(OctoDataIndexes.FAN_POWER_6) / 100d; // Fan 6 power
            _powers[6] = GetConvertedValue(OctoDataIndexes.FAN_POWER_7) / 100d; // Fan 7 power
            _powers[7] = GetConvertedValue(OctoDataIndexes.FAN_POWER_8) / 100d; // Fan 8 power
        }

        private sealed class OctoDataIndexes
        {
            public const int FIRMWARE_VERSION = 13;

            public const int TEMP_1 = 61;
            public const int TEMP_2 = 63;
            public const int TEMP_3 = 65;
            public const int TEMP_4 = 67;

            public const int FAN_SPEED_1 = 133;
            public const int FAN_SPEED_2 = 146;
            public const int FAN_SPEED_3 = 159;
            public const int FAN_SPEED_4 = 172;
            public const int FAN_SPEED_5 = 185;
            public const int FAN_SPEED_6 = 198;
            public const int FAN_SPEED_7 = 211;
            public const int FAN_SPEED_8 = 224;

            public const int FAN_POWER_1 = 131;
            public const int FAN_POWER_2 = 144;
            public const int FAN_POWER_3 = 157;
            public const int FAN_POWER_4 = 170;
            public const int FAN_POWER_5 = 183;
            public const int FAN_POWER_6 = 196;
            public const int FAN_POWER_7 = 209;
            public const int FAN_POWER_8 = 222;

            public const int VOLTAGE = 117;
            public const int FAN_VOLTAGE_1 = 127;
            public const int FAN_VOLTAGE_2 = 140;
            public const int FAN_VOLTAGE_3 = 153;
            public const int FAN_VOLTAGE_4 = 166;
            public const int FAN_VOLTAGE_5 = 179;
            public const int FAN_VOLTAGE_6 = 192;
            public const int FAN_VOLTAGE_7 = 205;
            public const int FAN_VOLTAGE_8 = 218;

            public const int FAN_CURRENT_1 = 129;
            public const int FAN_CURRENT_2 = 142;
            public const int FAN_CURRENT_3 = 155;
            public const int FAN_CURRENT_4 = 168;
            public const int FAN_CURRENT_5 = 181;
            public const int FAN_CURRENT_6 = 194;
            public const int FAN_CURRENT_7 = 207;
            public const int FAN_CURRENT_8 = 220;
        }

        private ushort? GetConvertedValue(int index)
        {
            if (_rawData[index] == sbyte.MaxValue)
                return null;

            return Convert.ToUInt16(_rawData[index + 1] | (_rawData[index] << 8));
        }
    }
}