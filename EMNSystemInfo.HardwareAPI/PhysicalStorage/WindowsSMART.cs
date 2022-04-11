// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using EMNSystemInfo.HardwareAPI.NativeInterop;

namespace EMNSystemInfo.HardwareAPI.PhysicalStorage
{
    /// <summary>
    /// Drive health
    /// </summary>
    public enum DriveHealth
    {
        /// <summary>
        /// Drive health is unknown
        /// </summary>
        Unknown,

        /// <summary>
        /// An error occurred during health query
        /// </summary>
        Error,

        /// <summary>
        /// Drive health is good
        /// </summary>
        Good,

        /// <summary>
        /// Drive health is bad
        /// </summary>
        Bad
    }

    internal class WindowsSMART : ISmart
    {
        private readonly int _driveNumber;
        private readonly SafeHandle _handle;

        public WindowsSMART(int driveNumber)
        {
            _driveNumber = driveNumber;
            _handle = Kernel32.CreateFile(@"\\.\PhysicalDrive" + driveNumber, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
        }

        public bool IsValid => !_handle.IsInvalid && !_handle.IsClosed;

        public void Dispose()
        {
            Close();
        }

        public void Close()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool EnableSmart()
        {
            if (_handle.IsClosed)
                throw new ObjectDisposedException(nameof(WindowsSMART));

            var parameter = new Kernel32.SENDCMDINPARAMS
            {
                bDriveNumber = (byte)_driveNumber,
                irDriveRegs = { bFeaturesReg = Kernel32.SMART_FEATURES.ENABLE_SMART, bCylLowReg = Kernel32.SMART_LBA_MID, bCylHighReg = Kernel32.SMART_LBA_HI, bCommandReg = Kernel32.ATA_COMMAND.ATA_SMART}
            };

            return Kernel32.DeviceIoControl(_handle, Kernel32.DFP.DFP_SEND_DRIVE_COMMAND, ref parameter, Marshal.SizeOf(parameter),
                out Kernel32.SENDCMDOUTPARAMS _, Marshal.SizeOf<Kernel32.SENDCMDOUTPARAMS>(), out _, IntPtr.Zero);
        }

        public Kernel32.SMART_ATTRIBUTE[] ReadSmartData()
        {
            if (_handle.IsClosed)
                throw new ObjectDisposedException(nameof(WindowsSMART));

            var parameter = new Kernel32.SENDCMDINPARAMS
            {
                bDriveNumber = (byte)_driveNumber, irDriveRegs = {
                    bFeaturesReg = Kernel32.SMART_FEATURES.SMART_READ_DATA,
                    bCylLowReg = Kernel32.SMART_LBA_MID,
                    bCylHighReg = Kernel32.SMART_LBA_HI,
                    bCommandReg = Kernel32.ATA_COMMAND.ATA_SMART
                }
            };

            bool isValid = Kernel32.DeviceIoControl(_handle, Kernel32.DFP.DFP_RECEIVE_DRIVE_DATA, ref parameter, Marshal.SizeOf(parameter),
                out Kernel32.ATTRIBUTECMDOUTPARAMS result, Marshal.SizeOf<Kernel32.ATTRIBUTECMDOUTPARAMS>(), out _, IntPtr.Zero);

            return isValid ? result.Attributes : new Kernel32.SMART_ATTRIBUTE[0];
        }

        public Kernel32.SMART_THRESHOLD[] ReadSmartThresholds()
        {
            if (_handle.IsClosed)
                throw new ObjectDisposedException(nameof(WindowsSMART));

            var parameter = new Kernel32.SENDCMDINPARAMS
            {
                bDriveNumber = (byte)_driveNumber, irDriveRegs = {
                    bFeaturesReg = Kernel32.SMART_FEATURES.READ_THRESHOLDS,
                    bCylLowReg = Kernel32.SMART_LBA_MID,
                    bCylHighReg = Kernel32.SMART_LBA_HI,
                    bCommandReg = Kernel32.ATA_COMMAND.ATA_SMART
                }
            };

            bool isValid = Kernel32.DeviceIoControl(_handle, Kernel32.DFP.DFP_RECEIVE_DRIVE_DATA, ref parameter, Marshal.SizeOf(parameter),
                out Kernel32.THRESHOLDCMDOUTPARAMS result, Marshal.SizeOf<Kernel32.THRESHOLDCMDOUTPARAMS>(), out _, IntPtr.Zero);

            return isValid ? result.Thresholds : new Kernel32.SMART_THRESHOLD[0];
        }

        public bool ReadIdentifyInfo(out Kernel32.IDENTIFY_DATA identify)
        {
            if (_handle.IsClosed)
                throw new ObjectDisposedException(nameof(WindowsSMART));

            var parameter = new Kernel32.SENDCMDINPARAMS
            {
                bDriveNumber = (byte)_driveNumber,
                irDriveRegs = { bCommandReg = Kernel32.ATA_COMMAND.ATA_IDENTIFY_DEVICE }
            };

            bool valid = Kernel32.DeviceIoControl(_handle, Kernel32.DFP.DFP_RECEIVE_DRIVE_DATA, ref parameter, Marshal.SizeOf(parameter),
                out Kernel32.IDENTIFYCMDOUTPARAMS result, Marshal.SizeOf<Kernel32.IDENTIFYCMDOUTPARAMS>(), out _, IntPtr.Zero);

            if (!valid)
            {
                identify = default;
                return false;
            }

            identify = result.Identify;
            return true;
        }

        public bool ReadNameAndFirmwareRevision(out string name, out string firmwareRevision)
        {
            if (_handle.IsClosed)
                throw new ObjectDisposedException(nameof(WindowsSMART));

            var parameter = new Kernel32.SENDCMDINPARAMS
            {
                bDriveNumber = (byte)_driveNumber,
                irDriveRegs = { bCommandReg = Kernel32.ATA_COMMAND.ATA_IDENTIFY_DEVICE }
            };

            bool valid = Kernel32.DeviceIoControl(_handle, Kernel32.DFP.DFP_RECEIVE_DRIVE_DATA, ref parameter, Marshal.SizeOf(parameter),
                out Kernel32.IDENTIFYCMDOUTPARAMS result, Marshal.SizeOf<Kernel32.IDENTIFYCMDOUTPARAMS>(), out _, IntPtr.Zero);

            if (!valid)
            {
                name = null;
                firmwareRevision = null;
                return false;
            }

            name = GetString(result.Identify.ModelNumber);
            firmwareRevision = GetString(result.Identify.FirmwareRevision);
            return true;
        }

        /// <summary>
        /// Reads S.M.A.R.T. health status of the drive
        /// </summary>
        /// <returns><see cref="DriveHealth.Good"/>, if drive is healthy; <see cref="DriveHealth.Bad"/>, if unhealthy; <see cref="DriveHealth.Unknown"/>, if it cannot be read; <see cref="DriveHealth.Error"/>, if an error occurred during health query.</returns>
        public DriveHealth ReadSmartHealth()
        {
            if (_handle.IsClosed)
                throw new ObjectDisposedException(nameof(WindowsSMART));

            var parameter = new Kernel32.SENDCMDINPARAMS
            {
                bDriveNumber = (byte)_driveNumber,
                irDriveRegs = {
                    bFeaturesReg = Kernel32.SMART_FEATURES.RETURN_SMART_STATUS,
                    bCylLowReg = Kernel32.SMART_LBA_MID,
                    bCylHighReg = Kernel32.SMART_LBA_HI,
                    bCommandReg = Kernel32.ATA_COMMAND.ATA_SMART
                }
            };

            bool isValid = Kernel32.DeviceIoControl(_handle, Kernel32.DFP.DFP_SEND_DRIVE_COMMAND, ref parameter, Marshal.SizeOf(parameter),
                out Kernel32.STATUSCMDOUTPARAMS result, Marshal.SizeOf<Kernel32.STATUSCMDOUTPARAMS>(), out _, IntPtr.Zero);

            if (!isValid)
            {
                return DriveHealth.Error;
            }

            // reference: https://github.com/smartmontools/smartmontools/blob/master/smartmontools/atacmds.cpp
            if (Kernel32.SMART_LBA_HI == result.irDriveRegs.bCylHighReg && Kernel32.SMART_LBA_MID == result.irDriveRegs.bCylLowReg)
            {
                // high and mid registers are unchanged, which means that the drive is healthy
                return DriveHealth.Good;
            }
            else if (Kernel32.SMART_LBA_HI_EXCEEDED == result.irDriveRegs.bCylHighReg && Kernel32.SMART_LBA_MID_EXCEEDED == result.irDriveRegs.bCylLowReg)
            {
                // high and mid registers are exceeded, which means that the drive is unhealthy
                return DriveHealth.Bad;
            }
            else
            {
                // response is not clear
                return DriveHealth.Unknown;
            }
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_handle.IsClosed)
                    _handle.Close();
            }
        }

        private static string GetString(IReadOnlyList<byte> bytes)
        {
            char[] chars = new char[bytes.Count];
            for (int i = 0; i < bytes.Count; i += 2)
            {
                chars[i] = (char)bytes[i + 1];
                chars[i + 1] = (char)bytes[i];
            }
            return new string(chars).Trim(' ', '\0');
        }
    }
}
