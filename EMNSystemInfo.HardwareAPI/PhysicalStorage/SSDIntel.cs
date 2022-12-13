// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using EMNSystemInfo.HardwareAPI.NativeInterop;
using System.Collections.Generic;

namespace EMNSystemInfo.HardwareAPI.PhysicalStorage
{
    [NamePrefix("INTEL SSD"), RequireSmart(0xE1), RequireSmart(0xE8), RequireSmart(0xE9)]
    internal class SSDIntel : ATADrive
    {
        private const byte TEMPERATURE_ATTRIBUTE = 0xBE;

        private static readonly IReadOnlyList<SMARTAttribute> _smartAttributes = new List<SMARTAttribute>
        {
            new SMARTAttribute(0x01, SMARTNames.ReadErrorRate),
            new SMARTAttribute(0x03, SMARTNames.SpinUpTime),
            new SMARTAttribute(0x04, SMARTNames.StartStopCount, RawToInt),
            new SMARTAttribute(0x05, SMARTNames.ReallocatedSectorsCount),
            new SMARTAttribute(POWERONHOURS_ATTRIBUTE, SMARTNames.PowerOnHours, RawToInt),
            new SMARTAttribute(POWERCYCLECOUNT_ATTRIBUTE, SMARTNames.PowerCycleCount, RawToInt),
            new SMARTAttribute(0xAA, SMARTNames.AvailableReservedSpace),
            new SMARTAttribute(0xAB, SMARTNames.ProgramFailCount),
            new SMARTAttribute(0xAC, SMARTNames.EraseFailCount),
            new SMARTAttribute(0xAE, SMARTNames.UnexpectedPowerLossCount, RawToInt),
            new SMARTAttribute(0xB7, SMARTNames.SataDownshiftErrorCount, RawToInt),
            new SMARTAttribute(0xB8, SMARTNames.EndToEndError),
            new SMARTAttribute(0xBB, SMARTNames.UncorrectableErrorCount, RawToInt),
            new SMARTAttribute(TEMPERATURE_ATTRIBUTE,
                               SMARTNames.Temperature,
                               (r, v, p) => r[0] + (p ?? 0)),
            new SMARTAttribute(0xC0, SMARTNames.UnsafeShutdownCount),
            new SMARTAttribute(0xC7, SMARTNames.CrcErrorCount, RawToInt),
            new SMARTAttribute(0xE1, SMARTNames.HostWrites, (r, v, p) => RawToInt(r, v, p) / 0x20),
            new SMARTAttribute(0xE8, SMARTNames.RemainingLife),
            new SMARTAttribute(0xE9, SMARTNames.MediaWearOutIndicator),
            new SMARTAttribute(0xF1, SMARTNames.HostWrites, (r, v, p) => RawToInt(r, v, p) / 0x20),
            new SMARTAttribute(0xF2, SMARTNames.HostReads, (r, v, p) => RawToInt(r, v, p) / 0x20)
        };

        public SSDIntel(StorageInfo storageInfo, ISmart smart)
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
