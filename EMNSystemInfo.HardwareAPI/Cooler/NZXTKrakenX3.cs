// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Linq;
using System.Threading;
using HidSharp;

namespace EMNSystemInfo.HardwareAPI.Cooler
{
    /**
     * Support for the Kraken X3 devices from NZXT
     */
    public sealed class NZXTKrakenX3 : ICooler
    {
        // Some fixed messages to send to the pump for basic monitoring and control
        private static readonly byte[] _getFirmwareInfo = { 0x10, 0x01 };
        private static readonly byte[] _initialize1 = { 0x70, 0x02, 0x01, 0xb8, 0x0b };
        private static readonly byte[] _initialize2 = { 0x70, 0x01 };
        private static readonly byte[][] _setPumpTargetMap = new byte[101][]; // Sacrifice memory to speed this up with a lookup instead of a copy operation

        private readonly ControlSensor _pump;
        private readonly Control _pumpControl;
        private double _pumpRpm;
        private readonly byte[] _rawData = new byte[64];
        private readonly HidStream _stream;
        private double _temperature;

        private volatile bool _controlling;

        static NZXTKrakenX3()
        {
            byte[] setPumpSpeedHeader = { 0x72, 0x01, 0x00, 0x00 };

            for (byte speed = 0; speed < _setPumpTargetMap.Length; speed++)
                _setPumpTargetMap[speed] = setPumpSpeedHeader.Concat(Enumerable.Repeat(speed, 40).Concat(new byte[20])).ToArray();
        }

        public NZXTKrakenX3(HidDevice dev)
        {
            if (dev.TryOpen(out _stream))
            {
                _stream.ReadTimeout = 5000; // The NZXT device returns with data that we need periodically without writing... 
                _stream.Write(_initialize1);
                _stream.Write(_initialize2);

                _stream.Write(_getFirmwareInfo);
                do
                {
                    _stream.Read(_rawData);
                    if (_rawData[0] == 0x11 && _rawData[1] == 0x01)
                    {
                        FirmwareVersion = $"{_rawData[0x11]}.{_rawData[0x12]}.{_rawData[0x13]}";
                    }
                }
                while (FirmwareVersion == null);

                Name = "NZXT Kraken X3";

                _pump = new();
                _pumpControl = new Control(_pump, 0, 100);
                _pump.Control = _pumpControl;
                _pumpControl.ControlModeChanged += SoftwareControlValueChanged;
                _pumpControl.SoftwareControlValueChanged += SoftwareControlValueChanged;
                SoftwareControlValueChanged(_pumpControl);

                ThreadPool.UnsafeQueueUserWorkItem(ContinuousRead, _rawData);
            }
        }

        public ControlSensor PumpControlSensor => _pump;

        public double PumpRPM => _pumpRpm;

        public double InternalWaterTemperature => _temperature;

        public string FirmwareVersion { get; private set; }

        public string Name { get; }

        public bool IsWaterCoolingSystem => true;

        public CoolerType Type => CoolerType.NZXTKrakenX3;

        private void SoftwareControlValueChanged(Control control)
        {
            if (control.ControlMode == ControlMode.Software)
            {
                double value = control.SoftwareValue;
                byte pumpSpeedIndex = (byte)(value > 100 ? 100 : (value < 0) ? 0 : value); // Clamp the value, anything out of range will fail

                _controlling = true;
                _stream.Write(_setPumpTargetMap[pumpSpeedIndex]);
                _pump.Value = value;
            }
            else if (control.ControlMode == ControlMode.Default)
            {
                // There isn't a "default" mode with this pump, but a safe setting is 40%
                _stream.Write(_setPumpTargetMap[40]);
            }
        }

        public void Close()
        {
            _stream?.Close();
        }

        private void ContinuousRead(object state)
        {
            byte[] buffer = new byte[_rawData.Length];
            while (_stream.CanRead)
            {
                try
                {
                    _stream.Read(buffer); // This is a blocking call, will wait for bytes to become available

                    lock (_rawData)
                    {
                        Array.Copy(buffer, _rawData, buffer.Length);
                    }
                }
                catch (TimeoutException)
                {
                    // Don't care, just make sure the stream is still open
                    Thread.Sleep(500);
                }
                catch (ObjectDisposedException)
                {
                    // Could be unplugged, or the app is stopping...
                    return;
                }
            }
        }

        public void Update()
        {
            // The NZXT Kraken X3 series sends updates periodically. We have to read it in a seperate thread, this call just reads that data.
            lock (_rawData)
            {
                if (_rawData[0] == 0x75 && _rawData[1] == 0x02)
                {
                    _temperature = _rawData[15] + _rawData[16] / 10.0f;
                    _pumpRpm = (_rawData[18] << 8) | _rawData[17];

                    // The following logic makes sure the pump is set to the controlling value. This pump sometimes sets itself to 0% when instructed to a value.
                    if (!_controlling)
                    {
                        _pump.Value = _rawData[19];
                    }
                    else if (_pump.Value != _rawData[19])
                    {
                        double value = _pump.Value.GetValueOrDefault();
                        byte pumpSpeedIndex = (byte)(value > 100 ? 100 : (value < 0) ? 0 : value); // Clamp the value, anything out of range will fail
                        _stream.Write(_setPumpTargetMap[pumpSpeedIndex]);
                    }
                    else
                    {
                        _controlling = false;
                    }
                }
            }
        }
    }
}
