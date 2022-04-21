// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using static EMNSystemInfo.HardwareAPI.NativeInterop.Kernel32;

namespace EMNSystemInfo.HardwareAPI.PhysicalStorage
{
    /// <summary>
    /// Main event handler for drive events (insert, remove)
    /// </summary>
    /// <param name="drive">The drive that's been inserted or removed</param>
    public delegate void DriveEventHandler(Drive drive);

    /// <summary>
    /// Class that represents a list of physical drives connected to the system.<br/>
    /// Administrator privileges are not required, but, if the library is not executing with admin privileges, only generic storage info can be accessed, regardless of the drive type
    /// </summary>
    public static class StorageDrives
    {
        private static ManagementEventWatcher _drvInsertedEvent;
        private static ManagementEventWatcher _drvRemovedEvent;
        private static List<Drive> _drives = new();

        /// <summary>
        /// Drives list
        /// </summary>
        public static Drive[] List => _drives.ToArray();

        /// <summary>
        /// Gets a value that represents if the drives are loaded on the <see cref="List"/> property. Returns <see langword="true"/> if drives are loaded, otherwise, <see langword="false"/>.
        /// </summary>
        public static bool DrivesAreLoaded { get; private set; } = false;

        /// <summary>
        /// Loads all the connected drives into the <see cref="List"/> property and starts the listeners for drive events.
        /// </summary>
        public static void LoadDrives()
        {
            if (!DrivesAreLoaded)
            {
                _drvInsertedEvent = new ManagementEventWatcher("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_DiskDrive'");
                _drvInsertedEvent.EventArrived += _drvInsertedEvent_EventArrived;
                _drvInsertedEvent.Start();

                _drvRemovedEvent = new ManagementEventWatcher("SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_DiskDrive'");
                _drvRemovedEvent.EventArrived += _drvRemovedEvent_EventArrived;
                _drvRemovedEvent.Start();

                //https://docs.microsoft.com/en-us/windows/win32/cimwin32prov/win32-diskdrive
                using var diskDriveSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive") { Options = { Timeout = TimeSpan.FromSeconds(10) } };
                using ManagementObjectCollection diskDrives = diskDriveSearcher.Get();

                foreach (ManagementBaseObject diskDrive in diskDrives)
                {
                    // Use WMIStorageInfo when Win32 info can't be accessed because of privileges
                    WMIStorageInfo wmiInfo = new(diskDrive);
                    if (wmiInfo.DeviceId != null)
                    {
                        var instance = Drive.CreateInstance(wmiInfo);
                        if (instance != null)
                        {
                            _drives.Add(instance);
                        }
                    }
                }

                DrivesAreLoaded = true;
            }
        }

        /// <summary>
        /// Frees the resources used by <see cref="Drive"/> instances and closes the listeners for drive events.
        /// </summary>
        public static void DisposeDrives()
        {
            if (DrivesAreLoaded)
            {
                _drvInsertedEvent?.Stop();
                if (_drvInsertedEvent != null)
                    _drvInsertedEvent.EventArrived -= _drvInsertedEvent_EventArrived;
                _drvInsertedEvent?.Dispose();

                _drvRemovedEvent?.Stop();
                if (_drvRemovedEvent != null)
                    _drvRemovedEvent.EventArrived -= _drvRemovedEvent_EventArrived;
                _drvRemovedEvent?.Dispose();

                foreach (Drive storage in List)
                    storage.Close();

                _drives.Clear();

                DrivesAreLoaded = false;
            }
        }

        private static void _drvInsertedEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject driveObject = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            WMIStorageInfo wmiInfo = new(driveObject);

            if (wmiInfo.DeviceId != null)
            {
                Drive newDrive = Drive.CreateInstance(wmiInfo);
                if (newDrive != null)
                {
                    _drives.Add(newDrive);
                    DriveInserted?.Invoke(newDrive);
                }
            }
        }

        private static void _drvRemovedEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject driveObject = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            WMIStorageInfo wmiInfo = new(driveObject);

            Drive removedDrive = (from d in _drives
                                  where d.Index == wmiInfo.Index
                                  select d).FirstOrDefault();

            if (removedDrive != default)
            {
                _drives.Remove(removedDrive);
                DriveRemoved?.Invoke(removedDrive);
            }
        }

        /// <summary>
        /// Drive inserted event. This event is <strong>asynchronous</strong>.
        /// </summary>
        public static event DriveEventHandler DriveInserted;

        /// <summary>
        /// Drive removed event. This event is <strong>asynchronous</strong>.
        /// </summary>
        public static event DriveEventHandler DriveRemoved;

        internal class WMIStorageInfo : StorageInfo
        {
            public WMIStorageInfo(ManagementBaseObject diskDrive)
            {
                // PnP device IDs of virtual drives are like this:
                // SCSI\DISK&VEN_MSFT&PROD_VIRTUAL_DISK\2&1F4ADFFE&0&000002
                string pnpDevId = ((string)(diskDrive.Properties["PnPDeviceId"].Value ?? "")).Trim();
                BusType = pnpDevId.Contains("PROD_VIRTUAL_DISK") ? STORAGE_BUS_TYPE.BusTypeVirtual : STORAGE_BUS_TYPE.BusTypeUnknown;
                
                Product = ((string)(diskDrive.Properties["Caption"].Value ?? "")).Trim();
                Revision = ((string)(diskDrive.Properties["FirmwareRevision"].Value ?? "")).Trim();
                Serial = ((string)(diskDrive.Properties["SerialNumber"].Value ?? "")).Trim();
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
