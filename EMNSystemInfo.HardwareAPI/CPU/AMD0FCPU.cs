// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using EMNSystemInfo.HardwareAPI.NativeInterop;
using System.Threading;

namespace EMNSystemInfo.HardwareAPI.CPU
{
    /// <summary>
    /// Class that represents an individual AMD CPU family 0Fh
    /// </summary>
    public sealed class AMD0FCPU : AMDCPU
    {
        private double _busClock;
        private readonly double[] _coreClocks;
        private readonly CoreTemperature[] _coreTemperatures;
        private readonly uint _miscellaneousControlAddress;

        private readonly byte _thermSenseCoreSelCPU0;
        private readonly byte _thermSenseCoreSelCPU1;

        /// <summary>
        /// Gets the bus clock speed, in megahertz (MHz).
        /// </summary>
        public double BusClock => _busClock;

        /// <summary>
        /// Gets the clock speed for all the cores, in megahertz (MHz). You can get the multiplier by dividing each value by the <see cref="BusClock"/> value.
        /// </summary>
        public double[] CoreClockSpeeds => _coreClocks;

        /// <summary>
        /// Gets an array of <see cref="CoreTemperature"/>s for each core.
        /// </summary>
        public CoreTemperature[] CoreTemperatures => _coreTemperatures;

        internal AMD0FCPU(int processorIndex, CPUID[][] cpuId) : base(processorIndex, cpuId)
        {
            Type = ProcessorType.AMD0FCPU;

            float offset = -49.0f;

            // AM2+ 65nm +21 offset
            uint model = cpuId[0][0].Model;
            if (model >= 0x69 && model != 0xc1 && model != 0x6c && model != 0x7c)
                offset += 21;

            if (model < 40)
            {
                // AMD Athlon 64 Processors
                _thermSenseCoreSelCPU0 = 0x0;
                _thermSenseCoreSelCPU1 = 0x4;
            }
            else
            {
                // AMD NPT Family 0Fh Revision F, G have the core selection swapped
                _thermSenseCoreSelCPU0 = 0x4;
                _thermSenseCoreSelCPU1 = 0x0;
            }

            // check if processor supports a digital thermal sensor
            if (cpuId[0][0].ExtData.GetLength(0) > 7 && (cpuId[0][0].ExtData[7, 3] & 1) != 0)
            {
                _coreTemperatures = new CoreTemperature[_coreCount];
                for (int i = 0; i < _coreCount; i++)
                {
                    _coreTemperatures[i] = new CoreTemperature { Offset = offset };
                }
            }
            else
            {
                _coreTemperatures = new CoreTemperature[0];
            }

            _miscellaneousControlAddress = GetPciAddress(MISCELLANEOUS_CONTROL_FUNCTION, MISCELLANEOUS_CONTROL_DEVICE_ID);
            _coreClocks = new double[_coreCount];

            Update();
        }

        /// <inheritdoc/>
        public override void Update()
        {
            base.Update();

            if (Ring0.WaitPciBusMutex(10))
            {
                if (_miscellaneousControlAddress != WinRing0.INVALID_PCI_ADDRESS)
                {
                    for (uint i = 0; i < _coreTemperatures.Length; i++)
                    {
                        if (Ring0.WritePciConfig(_miscellaneousControlAddress,
                                                 THERMTRIP_STATUS_REGISTER,
                                                 i > 0 ? _thermSenseCoreSelCPU1 : _thermSenseCoreSelCPU0))
                        {
                            if (Ring0.ReadPciConfig(_miscellaneousControlAddress, THERMTRIP_STATUS_REGISTER, out uint value))
                            {
                                _coreTemperatures[i] = new CoreTemperature { Value = ((value >> 16) & 0xFF) + _coreTemperatures[i].Offset, Offset = _coreTemperatures[i].Offset };
                            }
                        }
                    }
                }

                Ring0.ReleasePciBusMutex();
            }


            if (HasTimeStampCounter)
            {
                double newBusClock = 0;

                for (int i = 0; i < _coreClocks.Length; i++)
                {
                    Thread.Sleep(1);

                    if (Ring0.ReadMsr(FIDVID_STATUS, out uint eax, out uint _, _cpuId[i][0].Affinity))
                    {
                        // CurrFID can be found in eax bits 0-5, MaxFID in 16-21
                        // 8-13 hold StartFID, we don't use that here.
                        double curMp = 0.5 * ((eax & 0x3F) + 8);
                        double maxMp = 0.5 * ((eax >> 16 & 0x3F) + 8);
                        _coreClocks[i] = curMp * TimeStampCounterFrequency / maxMp;
                        newBusClock = TimeStampCounterFrequency / maxMp;
                    }
                    else
                    {
                        // Fail-safe value - if the code above fails, we'll use this instead
                        _coreClocks[i] = TimeStampCounterFrequency;
                    }
                }

                if (newBusClock > 0)
                {
                    _busClock = newBusClock;
                }
            }
        }

        // ReSharper disable InconsistentNaming
        private const uint FIDVID_STATUS = 0xC0010042;
        private const ushort MISCELLANEOUS_CONTROL_DEVICE_ID = 0x1103;
        private const byte MISCELLANEOUS_CONTROL_FUNCTION = 3;

        private const uint THERMTRIP_STATUS_REGISTER = 0xE4;
        // ReSharper restore InconsistentNaming
    }
}
