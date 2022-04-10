// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using EMNSystemInfo.HardwareAPI.NativeInterop;
using System.Collections.Generic;

namespace EMNSystemInfo.HardwareAPI.PhysicalStorage
{
    [NamePrefix("PLEXTOR")]
    internal class SSDPlextor : ATADrive
    {
        private static readonly IReadOnlyList<SMARTAttribute> _smartAttributes = new List<SMARTAttribute>
        {
            new SMARTAttribute(POWERONHOURS_ATTRIBUTE, SMARTNames.PowerOnHours, RawToInt),
            new SMARTAttribute(POWERCYCLECOUNT_ATTRIBUTE, SMARTNames.PowerCycleCount, RawToInt),
            new SMARTAttribute(0xF1, SMARTNames.HostWrites, RawToGb),
            new SMARTAttribute(0xF2, SMARTNames.HostReads, RawToGb)
        };

        public SSDPlextor(StorageInfo storageInfo, ISmart smart)
            : base(storageInfo, smart, _smartAttributes)
        {
            Type = PhysicalDriveType.SSD;
        }

        private static double RawToGb(byte[] rawValue, byte value, double? parameter)
        {
            return RawToInt(rawValue, value, parameter) / 32;
        }

        internal override void UpdateAdditionalSensors(Kernel32.SMART_ATTRIBUTE[] values)
        {
            DefaultUpdateAdditionalSensors(values, POWERONHOURS_ATTRIBUTE, POWERCYCLECOUNT_ATTRIBUTE, null);
        }
    }
}
