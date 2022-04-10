// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using EMNSystemInfo.HardwareAPI.NativeInterop;
using System.Collections.Generic;

namespace EMNSystemInfo.HardwareAPI.PhysicalStorage
{
    [NamePrefix(""), RequireSmart(0xAB), RequireSmart(0xB1)]
    internal class SSDSandforce : ATADrive
    {
        private const byte TEMPERATURE_ATTRIBUTE = 0xC2;

        private static readonly IReadOnlyList<SMARTAttribute> _smartAttributes = new List<SMARTAttribute>
        {
            new SMARTAttribute(0x01, SMARTNames.RawReadErrorRate),
            new SMARTAttribute(0x05, SMARTNames.RetiredBlockCount, RawToInt),
            new SMARTAttribute(POWERONHOURS_ATTRIBUTE, SMARTNames.PowerOnHours, RawToInt),
            new SMARTAttribute(POWERONHOURS_ATTRIBUTE, SMARTNames.PowerCycleCount, RawToInt),
            new SMARTAttribute(0xAB, SMARTNames.ProgramFailCount, RawToInt),
            new SMARTAttribute(0xAC, SMARTNames.EraseFailCount, RawToInt),
            new SMARTAttribute(0xAE, SMARTNames.UnexpectedPowerLossCount, RawToInt),
            new SMARTAttribute(0xB1, SMARTNames.WearRangeDelta, RawToInt),
            new SMARTAttribute(0xB5, SMARTNames.AlternativeProgramFailCount, RawToInt),
            new SMARTAttribute(0xB6, SMARTNames.AlternativeEraseFailCount, RawToInt),
            new SMARTAttribute(0xBB, SMARTNames.UncorrectableErrorCount, RawToInt),
            new SMARTAttribute(TEMPERATURE_ATTRIBUTE,
                               SMARTNames.Temperature,
                               (raw, value, p) => value + (p ?? 0)),
            new SMARTAttribute(0xC3, SMARTNames.UnrecoverableEcc),
            new SMARTAttribute(0xC4, SMARTNames.ReallocationEventCount, RawToInt),
            new SMARTAttribute(0xE7, SMARTNames.RemainingLife),
            new SMARTAttribute(0xE9, SMARTNames.ControllerWritesToNand, RawToInt),
            new SMARTAttribute(0xEA, SMARTNames.HostWritesToController, RawToInt),
            new SMARTAttribute(0xF1, SMARTNames.HostWrites, RawToInt),
            new SMARTAttribute(0xF2, SMARTNames.HostReads, RawToInt)
        };

        public SSDSandforce(StorageInfo storageInfo, ISmart smart)
            : base(storageInfo, smart, _smartAttributes)
        {
            Type = PhysicalDriveType.SSD;
        }

        internal override void UpdateAdditionalSensors(Kernel32.SMART_ATTRIBUTE[] values)
        {
            foreach (Kernel32.SMART_ATTRIBUTE attr in values)
            {
                if (attr.Id == POWERONHOURS_ATTRIBUTE)
                {
                    PowerOnTime = TimeSpan.FromHours(RawToInt(attr.RawValue, 0, null));
                }
                if (attr.Id == POWERCYCLECOUNT_ATTRIBUTE)
                {
                    PowerCycleCount = (ulong)RawToInt(attr.RawValue, 0, null);
                }
                if (attr.Id == TEMPERATURE_ATTRIBUTE)
                {
                    Temperature = attr.CurrentValue;
                }
            }
        }
    }
}
