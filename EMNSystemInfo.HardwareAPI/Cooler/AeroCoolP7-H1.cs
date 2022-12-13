// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Threading.Tasks;
using HidSharp;

namespace EMNSystemInfo.HardwareAPI.Cooler
{
    public sealed class AeroCoolP7H1 : ICooler
    {
        private const byte REPORT_ID = 0x0;
        private readonly HidDevice _device;

        private readonly double[] _fanRPMs = new double[5];
        private readonly double[] _speeds = new double[5];
        private readonly HidStream _stream;
        private bool _running;

        public AeroCoolP7H1(HidDevice dev)
        {
            _device = dev;
            HubNumber = _device.ProductID - 0x1000;
            Name = $"AeroCool P7-H1 #{HubNumber}";

            if (_device.TryOpen(out _stream))
            {
                _running = true;

                Task.Run(ReadStream);
            }
        }

        public string Name { get; }

        public bool IsWaterCoolingSystem => false;

        public CoolerType Type => CoolerType.AeroCoolP7H1;

        public double[] FanRPMs => _fanRPMs;

        public int HubNumber { get; }

        private void ReadStream()
        {
            byte[] inputReportBuffer = new byte[_device.GetMaxInputReportLength()];

            while (_running)
            {
                IAsyncResult ar = null;

                while (_running)
                {
                    ar ??= _stream.BeginRead(inputReportBuffer, 0, inputReportBuffer.Length, null, null);

                    if (ar.IsCompleted)
                    {
                        int byteCount = _stream.EndRead(ar);
                        ar = null;

                        if (byteCount == 16 && inputReportBuffer[0] == REPORT_ID)
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                int speed = inputReportBuffer[i * 3 + 2] * 256 + inputReportBuffer[i * 3 + 3];
                                _speeds[i] = speed;
                            }
                        }
                    }
                    else
                    {
                        ar.AsyncWaitHandle.WaitOne(1000);
                    }
                }
            }
        }

        public void Close()
        {
            _running = false;
            _stream.Close();
        }

        public void Update()
        {
            for (int i = 0; i < 5; i++)
            {
                _fanRPMs[i] = _speeds[i];
            }
        }
    }
}
