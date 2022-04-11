// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using EMNSystemInfo.HardwareAPI.NativeInterop;

namespace EMNSystemInfo.HardwareAPI.PhysicalStorage
{
    [NamePrefix(""), RequireSmart(0xAB), RequireSmart(0xAC), RequireSmart(0xAD), RequireSmart(0xAE), RequireSmart(0xC4), RequireSmart(0xCA), RequireSmart(0xCE)]
    internal class SSDMicron : ATADrive
    {
        private const byte TEMPERATURE_ATTRIBUTE = 0xC2;

        private static readonly IReadOnlyList<SMARTAttribute> _smartAttributes = new List<SMARTAttribute>
        {
            new SMARTAttribute(0x01, SMARTNames.ReadErrorRate, RawToInt),
            new SMARTAttribute(0x05, SMARTNames.ReallocatedNANDBlockCount, RawToInt),
            new SMARTAttribute(POWERONHOURS_ATTRIBUTE, SMARTNames.PowerOnHours, RawToInt),
            new SMARTAttribute(POWERCYCLECOUNT_ATTRIBUTE, SMARTNames.PowerCycleCount, RawToInt),
            new SMARTAttribute(0xAA, SMARTNames.NewFailingBlockCount, RawToInt),
            new SMARTAttribute(0xAB, SMARTNames.ProgramFailCount, RawToInt),
            new SMARTAttribute(0xAC, SMARTNames.EraseFailCount, RawToInt),
            new SMARTAttribute(0xAD, SMARTNames.WearLevelingCount, RawToInt),
            new SMARTAttribute(0xAE, SMARTNames.UnexpectedPowerLossCount, RawToInt),
            new SMARTAttribute(0xB4, SMARTNames.UnusedReserveNANDBlocks, RawToInt),
            new SMARTAttribute(0xB5, SMARTNames.Non4KAlignedAccess, (raw, value, p) => 6e4f * ((raw[5] << 8) | raw[4])),
            new SMARTAttribute(0xB7, SMARTNames.SataDownshiftErrorCount, RawToInt),
            new SMARTAttribute(0xB8, SMARTNames.ErrorCorrectionCount, RawToInt),
            new SMARTAttribute(0xBB, SMARTNames.ReportedUncorrectableErrors, RawToInt),
            new SMARTAttribute(0xBC, SMARTNames.CommandTimeout, RawToInt),
            new SMARTAttribute(0xBD, SMARTNames.FactoryBadBlockCount, RawToInt),
            new SMARTAttribute(TEMPERATURE_ATTRIBUTE, SMARTNames.Temperature, RawToInt),
            new SMARTAttribute(0xC4, SMARTNames.ReallocationEventCount, RawToInt),
            new SMARTAttribute(0xC5, SMARTNames.CurrentPendingSectorCount),
            new SMARTAttribute(0xC6, SMARTNames.OffLineUncorrectableErrorCount, RawToInt),
            new SMARTAttribute(0xC7, SMARTNames.UltraDmaCrcErrorCount, RawToInt),
            new SMARTAttribute(0xCA, SMARTNames.RemainingLife, (raw, value, p) => 100 - RawToInt(raw, value, p)),
            new SMARTAttribute(0xCE, SMARTNames.WriteErrorRate, (raw, value, p) => 6e4f * ((raw[1] << 8) | raw[0])),
            new SMARTAttribute(0xD2, SMARTNames.SuccessfulRAINRecoveryCount, RawToInt),
            new SMARTAttribute(0xF6,
                               SMARTNames.TotalLbasWritten,
                               (r, v, p) => (((long)r[5] << 40) |
                                             ((long)r[4] << 32) |
                                             ((long)r[3] << 24) |
                                             ((long)r[2] << 16) |
                                             ((long)r[1] << 8) |
                                             r[0]) *
                                            (512.0f / 1024 / 1024 / 1024)),
            new SMARTAttribute(0xF7, SMARTNames.HostProgramNANDPagesCount, RawToInt),
            new SMARTAttribute(0xF8, SMARTNames.FTLProgramNANDPagesCount, RawToInt)
        };

        public SSDMicron(StorageInfo storageInfo, ISmart smart)
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
