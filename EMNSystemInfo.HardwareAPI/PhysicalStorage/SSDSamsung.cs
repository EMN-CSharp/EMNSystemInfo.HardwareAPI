// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using EMNSystemInfo.HardwareAPI.NativeInterop;
using System.Collections.Generic;

namespace EMNSystemInfo.HardwareAPI.PhysicalStorage
{
    [NamePrefix(""), RequireSmart(0xB1), RequireSmart(0xB3), RequireSmart(0xB5), RequireSmart(0xB6), RequireSmart(0xB7), RequireSmart(0xBB), RequireSmart(0xC3), RequireSmart(0xC7)]
    internal class SSDSamsung : ATADrive
    {
        private const byte TEMPERATURE_ATTRIBUTE = 0xBE;

        private static readonly IReadOnlyList<SMARTAttribute> _smartAttributes = new List<SMARTAttribute>
        {
            new SMARTAttribute(0x05, SMARTNames.ReallocatedSectorsCount),
            new SMARTAttribute(POWERONHOURS_ATTRIBUTE, SMARTNames.PowerOnHours, RawToInt),
            new SMARTAttribute(POWERCYCLECOUNT_ATTRIBUTE, SMARTNames.PowerCycleCount, RawToInt),
            new SMARTAttribute(0xAF, SMARTNames.ProgramFailCountChip, RawToInt),
            new SMARTAttribute(0xB0, SMARTNames.EraseFailCountChip, RawToInt),
            new SMARTAttribute(0xB1, SMARTNames.WearLevelingCount, RawToInt),
            new SMARTAttribute(0xB2, SMARTNames.UsedReservedBlockCountChip, RawToInt),
            new SMARTAttribute(0xB3, SMARTNames.UsedReservedBlockCountTotal, RawToInt),

            // Unused Reserved Block Count (Total)
            new SMARTAttribute(0xB4, SMARTNames.RemainingLife),
            new SMARTAttribute(0xB5, SMARTNames.ProgramFailCountTotal, RawToInt),
            new SMARTAttribute(0xB6, SMARTNames.EraseFailCountTotal, RawToInt),
            new SMARTAttribute(0xB7, SMARTNames.RuntimeBadBlockTotal, RawToInt),
            new SMARTAttribute(0xBB, SMARTNames.UncorrectableErrorCount, RawToInt),
            new SMARTAttribute(TEMPERATURE_ATTRIBUTE,
                               SMARTNames.Temperature,
                               (r, v, p) => r[0] + (p ?? 0)),
            new SMARTAttribute(0xC2, SMARTNames.AirflowTemperature),
            new SMARTAttribute(0xC3, SMARTNames.EccRate),
            new SMARTAttribute(0xC6, SMARTNames.OffLineUncorrectableErrorCount, RawToInt),
            new SMARTAttribute(0xC7, SMARTNames.CrcErrorCount, RawToInt),
            new SMARTAttribute(0xC9, SMARTNames.SupercapStatus),
            new SMARTAttribute(0xCA, SMARTNames.ExceptionModeStatus),
            new SMARTAttribute(0xEB, SMARTNames.PowerRecoveryCount),
            new SMARTAttribute(0xF1,
                               SMARTNames.TotalLbasWritten,
                               (r, v, p) => (((long)r[5] << 40) | ((long)r[4] << 32) | ((long)r[3] << 24) | ((long)r[2] << 16) | ((long)r[1] << 8) | r[0]) * (512.0f / 1024 / 1024 / 1024))
        };

        public SSDSamsung(StorageInfo storageInfo, ISmart smart)
            : base(storageInfo, smart, _smartAttributes)
        {
            Type = PhysicalDriveType.SSD;
        }

        internal override void UpdateAdditionalSensors(Kernel32.SMART_ATTRIBUTE[] values)
        {
            DefaultUpdateAdditionalSensors(values, POWERONHOURS_ATTRIBUTE, POWERCYCLECOUNT_ATTRIBUTE, TEMPERATURE_ATTRIBUTE);
        }
    }
}
