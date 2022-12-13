// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using HidSharp;

namespace EMNSystemInfo.HardwareAPI.Cooler
{
    //TODO:
    //Check tested and fix unknown variables in Update()
    //Check if property "Variant" is valid interpreted
    //Implement Fan Control in SetControl()

    public class AquastreamXTTempSensors
    {
        public double ExternalFanVRM { get; internal set; }
        public double External { get; internal set; }
        public double InternalWater { get; internal set; }
    }

    public class AquastreamXTVoltageSensors
    {
        public double ExternalFan { get; internal set; }
        public double Pump { get; internal set; }
    }

    public class AquastreamXTMotorSpeedSensors
    {
        public double ExternalFan { get; internal set; }
        public double Pump { get; internal set; }
    }

    public class AquastreamXTFrequencySensors
    {
        public double PumpFrequency { get; internal set; }
        public double PumpMaxFrequency { get; internal set; }
    }

    public sealed class AquaComputerAquastreamXT : ICooler
    {
        private readonly ControlSensor _fanControl;
        private double _pumpFlow;
        private double _pumpPower;
        private readonly byte[] _rawData = new byte[64];
        private readonly HidStream _stream;

        public AquaComputerAquastreamXT(HidDevice dev)
        {
            if (dev.TryOpen(out _stream))
            {
                do
                {
                    _rawData[0] = 0x4;
                    _stream.GetFeature(_rawData);
                }
                while (_rawData[0] != 0x4);

                Name = $"Aquastream XT {Variant}";
                FirmwareVersion = BitConverter.ToUInt16(_rawData, 50);

                _fanControl = new();
                Control control = new(_fanControl, 0, 100);
                _fanControl.Control = control;
            }
        }

        public ControlSensor FanControl => _fanControl;

        public double PumpPower => _pumpPower;

        public double WaterFlow => _pumpFlow;

        public AquastreamXTTempSensors Temperatures { get; } = new();

        public AquastreamXTVoltageSensors Voltages { get; } = new();

        public AquastreamXTMotorSpeedSensors MotorSpeeds { get; } = new();

        public AquastreamXTFrequencySensors Frequencies { get; } = new();

        public ushort FirmwareVersion { get; private set; }

        public string Name { get; }

        public bool IsWaterCoolingSystem => true;

        public CoolerType Type => CoolerType.AquaComputerAquastreamXT;

        //TODO: Check if valid
        public string Variant
        {
            get
            {
                MODE mode = (MODE)_rawData[33];

                if (mode.HasFlag(MODE.MODE_PUMP_ADV))
                    return "Ultra + Internal Flow Sensor";

                if (mode.HasFlag(MODE.MODE_FAN_CONTROLLER))
                    return "Ultra";

                if (mode.HasFlag(MODE.MODE_FAN_AMP))
                    return "Advanced";


                return "Standard";
            }
        }

        public void Close()
        {
            _stream.Close();
        }

        //TODO: Check tested and fix unknown variables
        public void Update()
        {
            _rawData[0] = 0x4;
            _stream.GetFeature(_rawData);

            if (_rawData[0] != 0x4)
                return;


            //var rawSensorsFan = BitConverter.ToUInt16(rawData, 1);                        //unknown - redundant?
            //var rawSensorsExt = BitConverter.ToUInt16(rawData, 3);                        //unknown - redundant?
            //var rawSensorsWater = BitConverter.ToUInt16(rawData, 5);                      //unknown - redundant?

            Voltages.ExternalFan = BitConverter.ToUInt16(_rawData, 7) / 61f; //External Fan Voltage: tested - OK
            Voltages.Pump = BitConverter.ToUInt16(_rawData, 9) / 61f; //Pump Voltage: tested - OK
            _pumpPower = Voltages.Pump * BitConverter.ToInt16(_rawData, 11) / 625f; //Pump Voltage * Pump Current: tested - OK

            Temperatures.ExternalFanVRM = BitConverter.ToUInt16(_rawData, 13) / 100f; //External Fan VRM Temperature: untested
            Temperatures.External = BitConverter.ToUInt16(_rawData, 15) / 100f; //External Temperature Sensor: untested
            Temperatures.InternalWater = BitConverter.ToUInt16(_rawData, 17) / 100f; //Internal Water Temperature Sensor: tested - OK

            Frequencies.PumpFrequency = (1f / BitConverter.ToInt16(_rawData, 19)) * 750000; //Pump Frequency: tested - OK
            MotorSpeeds.Pump = Frequencies.PumpFrequency * 60f; //Pump RPM: tested - OK
            Frequencies.PumpMaxFrequency = (1f / BitConverter.ToUInt16(_rawData, 21)) * 750000; //Pump Max Frequency: tested - OK

            _pumpFlow = BitConverter.ToUInt32(_rawData, 23); //Internal Pump Flow Sensor: unknown

            MotorSpeeds.ExternalFan = BitConverter.ToUInt32(_rawData, 27); //External Fan RPM: untested

            _fanControl.Value = 100f / byte.MaxValue * _rawData[31]; //External Fan Control: tested, External Fan Voltage scales by this value - OK
        }

        [Flags]
        private enum MODE : byte
        {
            MODE_PUMP_ADV = 1,
            MODE_FAN_AMP = 2,
            MODE_FAN_CONTROLLER = 4
        }
    }
}
