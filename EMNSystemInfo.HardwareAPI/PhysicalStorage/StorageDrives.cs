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
    public static class StorageDrives
    {
        public static Drive[] List { get; private set; } = Array.Empty<Drive>();

        public static void LoadDrives()
        {
            //https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-diskdrive
            using var diskDriveSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive") { Options = { Timeout = TimeSpan.FromSeconds(10) } };
            using ManagementObjectCollection diskDrives = diskDriveSearcher.Get();

            List<Drive> drives = new();
            foreach (ManagementBaseObject diskDrive in diskDrives)
            {
                string deviceId = (string)diskDrive.Properties["DeviceId"].Value; // is \\.\PhysicalDrive0..n
                uint idx = Convert.ToUInt32(diskDrive.Properties["Index"].Value);
                ulong diskSize = Convert.ToUInt64(diskDrive.Properties["Size"].Value);
                int scsi = Convert.ToInt32(diskDrive.Properties["SCSIPort"].Value);
                uint heads = Convert.ToUInt32(diskDrive.Properties["TotalHeads"].Value);
                ulong tracks = Convert.ToUInt64(diskDrive.Properties["TotalTracks"].Value);
                ulong sectors = Convert.ToUInt64(diskDrive.Properties["TotalSectors"].Value);

                if (deviceId != null)
                {
                    var instance = Drive.CreateInstance(deviceId, idx, diskSize, scsi, heads, tracks, sectors);
                    if (instance != null)
                    {
                        drives.Add(instance);
                    }
                }
            }

            List = drives.ToArray();
        }

        public static void DisposeDrives()
        {
            foreach (Drive storage in List)
                storage.Close();

            List = Array.Empty<Drive>();
        }
    }
}
