// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using EMNSystemInfo.HardwareAPI.NativeInterop;

namespace EMNSystemInfo.HardwareAPI.Cooler
{
    public struct TBalancerFanSensor
    {
        public double Value { get; internal set; }

        public int MaxFanSpeed { get; internal set; }
    }

    public sealed class TBalancer : ICooler
    {
        /// <summary>
        /// The impulse rate of the flow meter in pulses per litre.
        /// </summary>
        public const int FlowMeterImpulseRate = 509;

        internal const byte EndFlag = 254;
        internal const byte StartFlag = 100;

        private readonly MethodDelegate _alternativeRequest;
        private readonly double?[] _analogTemperatures = new double?[4];
        private readonly double?[] _controls = new double?[4];
        private readonly double?[] _digitalTemperatures = new double?[8];
        private readonly TBalancerFanSensor[] _fans = new TBalancerFanSensor[4];
        private readonly int?[] _miniNgControls = new int?[4];
        private readonly double?[] _miniNgFans = new double?[4];
        private readonly double?[] _miniNgTemperatures = new double?[4];
        private readonly int _portIndex;
        private readonly byte _protocolVersion;
        private readonly double?[] _sensorHubFlows = new double?[2];
        private readonly double?[] _sensorHubTemperatures = new double?[6];
        private readonly byte[] _data = new byte[285];
        private Ftd2xx.FT_HANDLE _handle;
        private byte[] _alternativeData = Array.Empty<byte>();
        private byte[] _primaryData = Array.Empty<byte>();

        internal TBalancer(int portIndex, byte protocolVersion)
        {
            Name = "T-Balancer bigNG";

            _portIndex = portIndex;
            _protocolVersion = protocolVersion;

            _alternativeRequest = DelayedAlternativeRequest;

            Open();
            Update();
        }

        public double?[] AnalogTemperatures => _analogTemperatures;

        public double?[] DigitalTemperatures => _digitalTemperatures;

        public double?[] Controls => _controls;

        public int?[] MiniNGControls => _miniNgControls;

        public TBalancerFanSensor[] Fans => _fans;

        public double?[] MiniNGFans => _miniNgFans;

        public double?[] MiniNGTemperatures => _miniNgTemperatures;

        public double?[] SensorHubFlows => _sensorHubFlows;

        public double?[] SensorHubTemperatures => _sensorHubTemperatures;

        public int PortIndex => _portIndex;

        public byte ProtocolVersion => _protocolVersion;

        public string Name { get; }

        public bool IsWaterCoolingSystem => true;

        public CoolerType Type => CoolerType.TBalancer;

        private void ReadMiniNg(int number)
        {
            int offset = 1 + (number * 65);

            if (_data[offset + 61] != EndFlag)
                return;

            for (int i = 0; i < 2; i++)
            {
                if (_data[offset + 7 + i] > 0)
                {
                    _miniNgTemperatures[(number * 2) + i] = 0.5 * _data[offset + 7 + i];
                }
                else
                {
                    _miniNgTemperatures[(number * 2) + i] = null;
                }
            }

            for (int i = 0; i < 2; i++)
            {
                _miniNgFans[(number * 2) + i] = 20d * _data[offset + 43 + (2 * i)];
            }

            for (int i = 0; i < 2; i++)
            {
                _miniNgControls[(number * 2) + i] = _data[offset + 15 + i];
            }
        }

        private void ReadData()
        {
            Ftd2xx.Read(_handle, _data);
            if (_data[0] != StartFlag)
            {
                Ftd2xx.FT_Purge(_handle, Ftd2xx.FT_PURGE.FT_PURGE_RX);
                return;
            }

            if (_data[1] is 255 or 88)
            {
                // bigNG

                if (_data[274] != _protocolVersion)
                    return;

                if (_primaryData.Length == 0)
                    _primaryData = new byte[_data.Length];

                _data.CopyTo(_primaryData, 0);

                for (int i = 0; i < _digitalTemperatures.Length; i++)
                {
                    if (_data[238 + i] > 0)
                    {
                        _digitalTemperatures[i] = 0.5 * _data[238 + i];
                    }
                    else
                    {
                        _digitalTemperatures[i] = null;
                    }
                }

                for (int i = 0; i < _analogTemperatures.Length; i++)
                {
                    if (_data[260 + i] > 0)
                    {
                        _analogTemperatures[i] = 0.5 * _data[260 + i];
                    }
                    else
                    {
                        _analogTemperatures[i] = null;
                    }
                }

                for (int i = 0; i < _sensorHubTemperatures.Length; i++)
                {
                    if (_data[246 + i] > 0)
                    {
                        _sensorHubTemperatures[i] = 0.5f * _data[246 + i];
                    }
                    else
                    {
                        _sensorHubTemperatures[i] = null;
                    }
                }

                for (int i = 0; i < _sensorHubFlows.Length; i++)
                {
                    if (_data[231 + i] > 0 && _data[234] > 0)
                    {
                        double pulsesPerSecond = (_data[231 + i] * 4d) / _data[234];
                        _sensorHubFlows[i] = pulsesPerSecond * 3600 / FlowMeterImpulseRate;
                    }
                    else
                    {
                        _sensorHubFlows[i] = null;
                    }
                }

                for (int i = 0; i < _fans.Length; i++)
                {
                    double maxFanRPM = 11.5 * ((_data[149 + (2 * i)] << 8) | _data[148 + (2 * i)]);

                    double value;
                    if ((_data[136] & (1 << i)) == 0) // PWM mode
                        value = 0.02 * _data[137 + i];
                    else // Analog mode
                        value = 0.01 * _data[141 + i];

                    _fans[i].Value = maxFanRPM * value;

                    _controls[i] = 100 * value;
                }
            }
            else if (_data[1] == 253)
            {
                // miniNG #1
                if (_alternativeData.Length == 0)
                    _alternativeData = new byte[_data.Length];

                _data.CopyTo(_alternativeData, 0);

                ReadMiniNg(0);
                if (_data[66] == 253) // miniNG #2
                    ReadMiniNg(1);
            }
        }

        private void DelayedAlternativeRequest()
        {
            System.Threading.Thread.Sleep(500);
            Ftd2xx.Write(_handle, new byte[] { 0x37 });
        }

        internal void Open()
        {
            Ftd2xx.FT_Open(_portIndex, out _handle);
            Ftd2xx.FT_SetBaudRate(_handle, 19200);
            Ftd2xx.FT_SetDataCharacteristics(_handle, 8, 1, 0);
            Ftd2xx.FT_SetFlowControl(_handle, Ftd2xx.FT_FLOW_CONTROL.FT_FLOW_RTS_CTS, 0x11, 0x13);
            Ftd2xx.FT_SetTimeouts(_handle, 1000, 1000);
            Ftd2xx.FT_Purge(_handle, Ftd2xx.FT_PURGE.FT_PURGE_ALL);
        }

        public void Update()
        {
            while (Ftd2xx.BytesToRead(_handle) >= 285)
                ReadData();

            if (Ftd2xx.BytesToRead(_handle) == 1)
                Ftd2xx.ReadByte(_handle);

            Ftd2xx.Write(_handle, new byte[] { 0x38 });
            _alternativeRequest.BeginInvoke(null, null);
        }

        public void Close()
        {
            Ftd2xx.FT_Close(_handle);
        }

        private delegate void MethodDelegate();
    }
}
