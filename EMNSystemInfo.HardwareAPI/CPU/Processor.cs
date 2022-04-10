// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Diagnostics;
using System.Linq;

namespace EMNSystemInfo.HardwareAPI.CPU
{
    public enum ProcessorVendor
    {
        Unknown,
        Intel,
        AMD
    }

    /// <summary>
    /// Processor type. These values are required to get the proper instance. For example. if your processor type is an <see cref="IntelCPU"/>, you can convert your <see cref="Processor"/> instance into <see cref="CPU.IntelCPU"/> using type casting.
    /// </summary>
    public enum ProcessorType
    {
        Generic,

        /// <summary>
        /// Intel CPU. Convert your <see cref="Processor"/> instance into <see cref="CPU.IntelCPU"/>.
        /// </summary>
        IntelCPU,

        /// <summary>
        /// AMD CPU family 0Fh. Convert your <see cref="Processor"/> instance into <see cref="CPU.AMD0FCPU"/>.
        /// </summary>
        AMD0FCPU,

        /// <summary>
        /// AMD CPU family 10h-16h. Convert your <see cref="Processor"/> instance into <see cref="CPU.AMD10CPU"/>.
        /// </summary>
        AMD10CPU,

        /// <summary>
        /// AMD CPU family 17h-19h. Convert your <see cref="Processor"/> instance into <see cref="CPU.AMD17CPU"/>.
        /// </summary>
        AMD17CPU
    }

    /// <summary>
    /// Struct that represents a thread load.
    /// </summary>
    public struct ThreadLoad
    {
        /// <summary>
        /// Core number
        /// </summary>
        public int Core { get; internal set; }

        /// <summary>
        /// Thread number. This property is nullable.
        /// </summary>
        public int? Thread { get; internal set; }

        /// <summary>
        /// Load value
        /// </summary>
        public double Value { get; internal set; }
    }

    /// <summary>
    /// Class that represents an individual generic processor.
    /// </summary>
    public class Processor
    {
        protected readonly int _coreCount;
        protected readonly int _threadCount;
        internal readonly CPUID[][] _cpuId;
        protected readonly uint _family;
        protected readonly uint _model;
        protected readonly uint _packageType;
        protected readonly uint _stepping;

        private readonly CPULoad _cpuLoad;
        private readonly double _estimatedTimeStampCounterFrequency;
        private readonly ProcessorVendor _vendor;

        /// <summary>
        /// Gets the processor brand string. Example: Intel(R) Core(TM) i3-1005G1 CPU @ 1.20GHz
        /// </summary>
        public string BrandString => _cpuId[0][0].BrandString;

        /// <summary>
        /// Gets the processor name. It is a "summary" of the <see cref="BrandString"/> value. Example: Intel Core i3-1005G1
        /// </summary>
        public string Name => _cpuId[0][0].Name;

        /// <summary>
        /// Gets the processor vendor.
        /// </summary>
        public ProcessorVendor Vendor => _vendor;

        /// <summary>
        /// Gets the processor type.
        /// </summary>
        public ProcessorType Type { get; internal set; } = ProcessorType.Generic;

        /// <summary>
        /// Gets the CPU total load
        /// </summary>
        public double TotalLoad
        {
            get
            {
                if (_cpuLoad.IsAvailable)
                {
                    _cpuLoad.Update();
                    return _cpuLoad.GetTotalLoad();
                }

                return 0;
            }
        }

        /// <summary>
        /// Gets an array of <see cref="ThreadLoad"/>s, each element representing a core and, if available, a thread.
        /// </summary>
        public ThreadLoad[] ThreadLoads
        {
            get
            {
                if (_cpuLoad.IsAvailable)
                {
                    ThreadLoad[] threadLoads = new ThreadLoad[_threadCount];
                    for (int coreIdx = 0; coreIdx < _cpuId.Length; coreIdx++)
                    {
                        for (int threadIdx = 0; threadIdx < _cpuId[coreIdx].Length; threadIdx++)
                        {
                            int thread = _cpuId[coreIdx][threadIdx].Thread;
                            if (thread < threadLoads.Length)
                            {
                                threadLoads[thread].Core = coreIdx;
                                // Some cores may have 2 threads while others have only one (e.g. P-cores vs E-cores on Intel 12th gen).
                                threadLoads[thread].Thread = _cpuId[coreIdx].Length > 1 ? threadIdx : null;
                                threadLoads[thread].Value = _cpuLoad.GetThreadLoad(thread);
                            }
                        }
                    }

                    return threadLoads;
                }

                return Array.Empty<ThreadLoad>();
            }
        }

        internal Processor(int processorIndex, CPUID[][] cpuId)
        {
            _cpuId = cpuId;
            _vendor = cpuId[0][0].Vendor;
            _family = cpuId[0][0].Family;
            _model = cpuId[0][0].Model;
            _stepping = cpuId[0][0].Stepping;
            _packageType = cpuId[0][0].PkgType;

            Index = processorIndex;
            _coreCount = cpuId.Length;
            _threadCount = cpuId.Sum(x => x.Length);

            // Check if processor has MSRs.
            HasModelSpecificRegisters = cpuId[0][0].Data.GetLength(0) > 1 && (cpuId[0][0].Data[1, 3] & 0x20) != 0;

            // Check if processor has a TSC.
            HasTimeStampCounter = cpuId[0][0].Data.GetLength(0) > 1 && (cpuId[0][0].Data[1, 3] & 0x10) != 0;

            _cpuLoad = new CPULoad(cpuId);

            if (HasTimeStampCounter)
            {
                GroupAffinity previousAffinity = ThreadAffinity.Set(cpuId[0][0].Affinity);
                EstimateTimeStampCounterFrequency(out _estimatedTimeStampCounterFrequency, out _);
                ThreadAffinity.Set(previousAffinity);
            }
            else
            {
                _estimatedTimeStampCounterFrequency = 0;
            }

            TimeStampCounterFrequency = _estimatedTimeStampCounterFrequency;
        }

        /// <summary>
        /// Gets the CPUID.
        /// </summary>
        internal CPUID[][] CpuId => _cpuId;

        /// <summary>
        /// Gets if the CPU has MSRs (Model-Specific Registers) (<c>rdmsr</c> and <c>wrmsr</c> instructions)
        /// </summary>
        public bool HasModelSpecificRegisters { get; }

        /// <summary>
        /// Gets if the CPU has a TSC (Time Stamp Counter)
        /// </summary>
        public bool HasTimeStampCounter { get; }

        /// <summary>
        /// Gets the CPU index.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets the CPU TSC frequency, in megahertz (MHz).
        /// </summary>
        public double TimeStampCounterFrequency { get; private set; }

        private void EstimateTimeStampCounterFrequency(out double frequency, out double error)
        {
            // preload the function
            EstimateTimeStampCounterFrequency(0, out double f, out double e);
            EstimateTimeStampCounterFrequency(0, out f, out e);

            // estimate the frequency
            error = double.MaxValue;
            frequency = 0;
            for (int i = 0; i < 5; i++)
            {
                EstimateTimeStampCounterFrequency(0.025, out f, out e);
                if (e < error)
                {
                    error = e;
                    frequency = f;
                }

                if (error < 1e-4)
                    break;
            }
        }

        private static void EstimateTimeStampCounterFrequency(double timeWindow, out double frequency, out double error)
        {
            long ticks = (long)(timeWindow * Stopwatch.Frequency);

            long timeBegin = Stopwatch.GetTimestamp() + (long)Math.Ceiling(0.001 * ticks);
            long timeEnd = timeBegin + ticks;

            while (Stopwatch.GetTimestamp() < timeBegin)
            { }

            ulong countBegin = OpCode.Rdtsc();
            long afterBegin = Stopwatch.GetTimestamp();

            while (Stopwatch.GetTimestamp() < timeEnd)
            { }

            ulong countEnd = OpCode.Rdtsc();
            long afterEnd = Stopwatch.GetTimestamp();

            double delta = timeEnd - timeBegin;
            frequency = 1e-6 * ((double)(countEnd - countBegin) * Stopwatch.Frequency) / delta;

            double beginError = (afterBegin - timeBegin) / delta;
            double endError = (afterEnd - timeEnd) / delta;
            error = beginError + endError;
        }
    }
}
