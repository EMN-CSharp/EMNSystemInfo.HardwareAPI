// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Management;

namespace EMNSystemInfo.HardwareAPI.PhysicalStorage
{
    /// <summary>
    /// Class that represents a list of physical drives connected to the system. Administrator privileges are required.
    /// </summary>
    public static class StorageDrives
    {
        /// <summary>
        /// Drives list
        /// </summary>
        public static Drive[] List { get; private set; } = Array.Empty<Drive>();

        /// <summary>
        /// Loads all the connected drives into the <see cref="List"/> property.
        /// </summary>
        public static void LoadDrives()
        {
            //https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-diskdrive
            using var diskDriveSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive") { Options = { Timeout = TimeSpan.FromSeconds(10) } };
            using ManagementObjectCollection diskDrives = diskDriveSearcher.Get();

            List<Drive> drives = new();
            foreach (ManagementBaseObject diskDrive in diskDrives)
            {
                // Use WMIStorageInfo when Win32 info can't be accessed because of privileges
                WMIStorageInfo wmiInfo = new(diskDrive);

                if (wmiInfo.DeviceId != null)
                {
                    var instance = Drive.CreateInstance(wmiInfo);
                    if (instance != null)
                    {
                        drives.Add(instance);
                    }
                }
            }

            List = drives.ToArray();
        }

        /// <summary>
        /// Frees the resources used by <see cref="Drive"/> instances.
        /// </summary>
        public static void DisposeDrives()
        {
            foreach (Drive storage in List)
                storage.Close();

            List = Array.Empty<Drive>();
        }

        internal class WMIStorageInfo : StorageInfo
        {
            public WMIStorageInfo(ManagementBaseObject diskDrive)
            {
                BusType = NativeInterop.Kernel32.STORAGE_BUS_TYPE.BusTypeUnknown;
                Product = ((string)diskDrive.Properties["Caption"].Value).Trim();
                Serial = ((string)diskDrive.Properties["SerialNumber"].Value).Trim();
                Revision = ((string)diskDrive.Properties["FirmwareRevision"].Value).Trim();
                DeviceId = (string)diskDrive.Properties["DeviceId"].Value; // is \\.\PhysicalDrive0..n
                Index = (int)Convert.ToUInt32(diskDrive.Properties["Index"].Value);
                DiskSize = Convert.ToUInt64(diskDrive.Properties["Size"].Value);
                int scsiPort = Convert.ToInt32(diskDrive.Properties["SCSIPort"].Value);
                Scsi = $@"\\.\SCSI{scsiPort}:";
                DriveGeometry.Cylinders = Convert.ToUInt64(diskDrive.Properties["TotalCylinders"].Value);
                DriveGeometry.Heads = Convert.ToUInt32(diskDrive.Properties["TotalHeads"].Value);
                DriveGeometry.Tracks = Convert.ToUInt64(diskDrive.Properties["TotalTracks"].Value);
                DriveGeometry.Sectors = Convert.ToUInt64(diskDrive.Properties["TotalSectors"].Value);
                DriveGeometry.TracksPerCylinder = Convert.ToUInt32(diskDrive.Properties["TracksPerCylinder"].Value);
                DriveGeometry.SectorsPerTrack = Convert.ToUInt32(diskDrive.Properties["SectorsPerTrack"].Value);
                DriveGeometry.BytesPerSector = Convert.ToUInt32(diskDrive.Properties["BytesPerSector"].Value);
            }
        }
    }
}
