// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using static EMNSystemInfo.HardwareAPI.NativeInterop.NTDLL;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace EMNSystemInfo.HardwareAPI.CPU
{
    internal class CPULoad
    {
        private long[] _idleTimes;
        private readonly double[] _threadLoads;
        private double _totalLoad;
        private long[] _totalTimes;

        public CPULoad(CPUID[][] cpuid)
        {
            _threadLoads = new double[cpuid.Sum(x => x.Length)];
            _totalLoad = 0;
            try
            {
                GetTimes(out _idleTimes, out _totalTimes);
            }
            catch (Exception)
            {
                _idleTimes = null;
                _totalTimes = null;
            }

            if (_idleTimes != null)
                IsAvailable = true;
        }

        public bool IsAvailable { get; }

        private static bool GetTimes(out long[] idle, out long[] total)
        {
            SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION[] information = new SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION[64];
            int size = Marshal.SizeOf(typeof(SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION));

            idle = null;
            total = null;

            if (NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemProcessorPerformanceInformation,
                                         information,
                                         information.Length * size,
                                         out IntPtr returnLength) != 0)
            {
                return false;
            }

            idle = new long[(int)returnLength / size];
            total = new long[(int)returnLength / size];

            for (int i = 0; i < idle.Length; i++)
            {
                idle[i] = information[i].IdleTime;
                total[i] = information[i].KernelTime + information[i].UserTime;
            }

            return true;
        }

        public double GetTotalLoad()
        {
            return _totalLoad;
        }

        public double GetThreadLoad(int thread)
        {
            return _threadLoads[thread];
        }

        public void Update()
        {
            if (_idleTimes == null)
                return;

            if (!GetTimes(out long[] newIdleTimes, out long[] newTotalTimes))
                return;


            for (int i = 0; i < Math.Min(newTotalTimes.Length, _totalTimes.Length); i++)
            {
                if (newTotalTimes[i] - _totalTimes[i] < 100000)
                    return;
            }

            if (newIdleTimes == null)
                return;

            float total = 0;
            int count = 0;
            for (int i = 0; i < _threadLoads.Length && i < _idleTimes.Length && i < newIdleTimes.Length; i++)
            {
                float idle = (newIdleTimes[i] - _idleTimes[i]) / (float)(newTotalTimes[i] - _totalTimes[i]);
                _threadLoads[i] = 100f * (1.0f - Math.Min(idle, 1.0f));
                total += idle;
                count++;
            }

            if (count > 0)
            {
                total = 1.0f - total / count;
                total = total < 0 ? 0 : total;
            }
            else
            {
                total = 0;
            }

            _totalLoad = total * 100;
            _totalTimes = newTotalTimes;
            _idleTimes = newIdleTimes;
        }
    }
}
