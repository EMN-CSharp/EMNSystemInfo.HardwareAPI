// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using EMNSystemInfo.HardwareAPI.NativeInterop;
using System.Collections.Generic;

namespace EMNSystemInfo.HardwareAPI.PhysicalStorage
{
    [NamePrefix(""), RequireSmart(0x01), RequireSmart(0x09), RequireSmart(0x0C), RequireSmart(0xD1), RequireSmart(0xCE), RequireSmart(0xCF)]
    internal class SSDIndilinx : ATADrive
    {
        private static readonly IReadOnlyList<SMARTAttribute> _smartAttributes = new List<SMARTAttribute>
        {
            new SMARTAttribute(0x01, SMARTNames.ReadErrorRate),
            new SMARTAttribute(POWERONHOURS_ATTRIBUTE, SMARTNames.PowerOnHours),
            new SMARTAttribute(POWERCYCLECOUNT_ATTRIBUTE, SMARTNames.PowerCycleCount),
            new SMARTAttribute(0xB8, SMARTNames.InitialBadBlockCount),
            new SMARTAttribute(0xC3, SMARTNames.ProgramFailure),
            new SMARTAttribute(0xC4, SMARTNames.EraseFailure),
            new SMARTAttribute(0xC5, SMARTNames.ReadFailure),
            new SMARTAttribute(0xC6, SMARTNames.SectorsRead),
            new SMARTAttribute(0xC7, SMARTNames.SectorsWritten),
            new SMARTAttribute(0xC8, SMARTNames.ReadCommands),
            new SMARTAttribute(0xC9, SMARTNames.WriteCommands),
            new SMARTAttribute(0xCA, SMARTNames.BitErrors),
            new SMARTAttribute(0xCB, SMARTNames.CorrectedErrors),
            new SMARTAttribute(0xCC, SMARTNames.BadBlockFullFlag),
            new SMARTAttribute(0xCD, SMARTNames.MaxCellCycles),
            new SMARTAttribute(0xCE, SMARTNames.MinErase),
            new SMARTAttribute(0xCF, SMARTNames.MaxErase),
            new SMARTAttribute(0xD0, SMARTNames.AverageEraseCount),
            new SMARTAttribute(0xD1, SMARTNames.RemainingLife),
            new SMARTAttribute(0xD2, SMARTNames.UnknownUnique),
            new SMARTAttribute(0xD3, SMARTNames.SataErrorCountCrc),
            new SMARTAttribute(0xD4, SMARTNames.SataErrorCountHandshake)
        };

        public SSDIndilinx(StorageInfo storageInfo, ISmart smart)
            : base(storageInfo, smart, _smartAttributes)
        {
            Type = PhysicalDriveType.SSD;
        }

        internal override void UpdateAdditionalSensors(Kernel32.SMART_ATTRIBUTE[] values)
        {
            DefaultUpdateAdditionalSensors(values, POWERONHOURS_ATTRIBUTE, POWERCYCLECOUNT_ATTRIBUTE, null);
        }
    }
}
