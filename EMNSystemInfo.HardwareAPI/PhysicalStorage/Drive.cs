// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using EMNSystemInfo.HardwareAPI.NativeInterop;

namespace EMNSystemInfo.HardwareAPI.PhysicalStorage
{
    /// <summary>
    /// Physical drive type
    /// </summary>
    public enum PhysicalDriveType
    {
        Generic,

        /// <summary>
        /// Hard disk or generic drive
        /// </summary>
        HDD,

        /// <summary>
        /// SSD drive
        /// </summary>
        SSD,

        /// <summary>
        /// NVM Express (NVMe) drive. Convert your <see cref="Drive"/> instance into <see cref="NVMeDrive"/>
        /// </summary>
        NVMe
    }

    /// <summary>
    /// Class that represents an individual generic drive.
    /// </summary>
    public class Drive
    {
        private DrivePerformanceCounters _drivePCs;
        private readonly StorageInfo _storageInfo;
        private readonly TimeSpan _updateInterval = TimeSpan.FromMilliseconds(60);

        private DateTime _lastUpdate = DateTime.MinValue;
        private double _usageSensor;

        /// <summary>
        /// Gets the drive name
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets the drive serial number
        /// </summary>
        public string SerialNumber { get; protected set; }

        /// <summary>
        /// Gets the drive capacity, in bytes.
        /// </summary>
        public ulong Capacity => _storageInfo.DiskSize;

        /// <summary>
        /// Gets the drive firmware revision
        /// </summary>
        public string FirmwareRevision { get; protected set; }

        /// <summary>
        /// Gets the drive geometry information
        /// </summary>
        public DriveGeometry Geometry { get; protected set; }

        /// <summary>
        /// Gets if this drive is an ATA drive. If it's <see langword="true"/>, you can convert your <see cref="Drive"/> instance into <see cref="ATADrive"/>
        /// </summary>
        public bool IsATADrive { get; protected set; }

        /// <summary>
        /// Gets if this drive is removable. This property is nullable
        /// </summary>
        public bool? IsRemovable => _storageInfo.Removable;

        /// <summary>
        /// Gets the logical drives of this drive.
        /// </summary>
        public DriveInfo[] LogicalDrives { get; }

        /// <summary>
        /// Gets the device identifier
        /// </summary>
        public string DeviceId => _storageInfo.DeviceId;

        /// <summary>
        /// Gets the drive index
        /// </summary>
        public int Index => _storageInfo.Index;

        /// <summary>
        /// Gets the drive type.
        /// </summary>
        public PhysicalDriveType Type { get; internal set; } = PhysicalDriveType.Generic;

        /// <summary>
        /// Gets the used capacity percentage of the drive. Does not include hidden partitions.
        /// </summary>
        public double UsedCapacityPercentage => _usageSensor;

        /// <summary>
        /// Gets the total activity percentage of the drive. This property is nullable.
        /// </summary>
        public double? TotalActivityPercentage { get; internal set; }

        /// <summary>
        /// Gets the total read activity percentage of the drive. This property is nullable.
        /// </summary>
        public double? TotalReadActivityPercentage { get; internal set; }

        /// <summary>
        /// Gets the total write activity percentage of the drive. This property is nullable.
        /// </summary>
        public double? TotalWriteActivityPercentage { get; internal set; }

        /// <summary>
        /// Gets the idle time percentage of the drive. This property is nullable.
        /// </summary>
        public double? IdleTimePercentage { get; internal set; }

        /// <summary>
        /// Gets the drive read speed, in bytes per second (B/s). This property is nullable.
        /// </summary>
        public double? ReadSpeed { get; internal set; }

        /// <summary>
        /// Gets the drive write speed, in bytes per second (B/s). This property is nullable.
        /// </summary>
        public double? WriteSpeed { get; internal set; }

        /// <summary>
        /// Gets the average response time per transfer of the drive, in seconds (s). This property is nullable.
        /// </summary>
        public double? AverageResponseTimePerTransfer { get; internal set; }

        /// <summary>
        /// Gets the average response time per read of the drive, in seconds (s). This property is nullable.
        /// </summary>
        public double? AverageResponseTimePerRead { get; internal set; }

        /// <summary>
        /// Gets the average response time per write of the drive, in seconds (s). This property is nullable.
        /// </summary>
        public double? AverageResponseTimePerWrite { get; internal set; }

        internal Drive(StorageInfo storageInfo)
        {
            _storageInfo = storageInfo;
            Geometry = storageInfo.DriveGeometry;
            Name = storageInfo.Name;
            SerialNumber = storageInfo.Serial;
            FirmwareRevision = storageInfo.Revision;

            try
            {
                _drivePCs = new DrivePerformanceCounters(storageInfo.Index);
            }
            catch (Exception)
            {
                /*
                 * For some reason (most likely the drive is not registered in perf counters)
                 * we can't get an instance of DrivePerformanceCounters. Ignore the
                 * exception, because we'll try to get an instance when Update() is called.
                 */
            }

            string[] logicalDrives = WindowsStorage.GetLogicalDrives(storageInfo.Index);
            var driveInfoList = new List<DriveInfo>(logicalDrives.Length);

            foreach (string logicalDrive in logicalDrives)
            {
                try
                {
                    var di = new DriveInfo(logicalDrive);
                    if (di.TotalSize > 0)
                        driveInfoList.Add(new DriveInfo(logicalDrive));
                }
                catch (ArgumentException)
                { }
                catch (IOException)
                { }
                catch (UnauthorizedAccessException)
                { }
            }

            LogicalDrives = driveInfoList.ToArray();
        }

        internal static Drive CreateInstance(StorageDrives.WMIStorageInfo wmiInfo)
        {
            StorageInfo info = WindowsStorage.GetStorageInfo(wmiInfo.DeviceId, (uint)wmiInfo.Index);

            // Win32 storage info is not available, try with WMI storage info
            if (info == null)
            {
                info = wmiInfo;
            }
            else // Set some info WindowsStorage didn't provide
            {
                info.DeviceId = wmiInfo.DeviceId;
                info.DiskSize = wmiInfo.DiskSize;
                info.DriveGeometry.Heads = wmiInfo.DriveGeometry.Heads;
                info.DriveGeometry.Tracks = wmiInfo.DriveGeometry.Tracks;
                info.DriveGeometry.Sectors = wmiInfo.DriveGeometry.Sectors;
            }
            if (info == null)
                return null;

            if (info.BusType is Kernel32.STORAGE_BUS_TYPE.BusTypeVirtual or Kernel32.STORAGE_BUS_TYPE.BusTypeFileBackedVirtual)
                return null;

            // Fallback, when it is not possible to read out with the NVMe implementation,
            // Try it with the SATA S.M.A.R.T. implementation
            if (info.BusType == Kernel32.STORAGE_BUS_TYPE.BusTypeNvme)
            {
                Drive x = NVMeDrive.CreateInstance(info);
                if (x != null)
                    return x;
            }

            if (info.BusType == Kernel32.STORAGE_BUS_TYPE.BusTypeAta ||
                info.BusType == Kernel32.STORAGE_BUS_TYPE.BusTypeSata ||
                info.BusType == Kernel32.STORAGE_BUS_TYPE.BusTypeNvme ||
                /*
                 * It may seem strange to accept RAID drives, but for some reason
                 * my system recognizes my main drive (HDD) bus type as RAID, when
                 * actually it's a SATA drive.
                */
                info.BusType == Kernel32.STORAGE_BUS_TYPE.BusTypeRAID)
            {
                return ATADrive.CreateInstance(info);
            }


            return new Drive(info);
        }

        protected virtual void UpdateSensors()
        { }

        public virtual void Update()
        {
            //read out with updateInterval
            TimeSpan tDiff = DateTime.UtcNow - _lastUpdate;
            if (tDiff > _updateInterval)
            {
                if (_drivePCs == null)
                {
                    try
                    {
                        _drivePCs = new DrivePerformanceCounters(_storageInfo.Index);
                    }
                    catch (Exception)
                    {
                        // Try the next time...
                    }
                }

                TotalActivityPercentage = _drivePCs?.DriveTime;
                TotalReadActivityPercentage = _drivePCs?.ReadTime;
                TotalWriteActivityPercentage = _drivePCs?.WriteTime;
                IdleTimePercentage = _drivePCs?.IdleTime;
                ReadSpeed = _drivePCs?.ReadSpeed;
                WriteSpeed = _drivePCs?.WriteSpeed;
                AverageResponseTimePerTransfer = _drivePCs?.AverageResponseTimePerTransfer;
                AverageResponseTimePerRead = _drivePCs?.AverageResponseTimePerRead;
                AverageResponseTimePerWrite = _drivePCs?.AverageResponseTimePerWrite;

                _lastUpdate = DateTime.UtcNow;

                UpdateSensors();

                long totalSize = 0;
                long totalFreeSpace = 0;

                for (int i = 0; i < LogicalDrives.Length; i++)
                {
                    if (!LogicalDrives[i].IsReady)
                        continue;

                    try
                    {
                        totalSize += LogicalDrives[i].TotalSize;
                        totalFreeSpace += LogicalDrives[i].TotalFreeSpace;
                    }
                    catch (IOException)
                    { }
                    catch (UnauthorizedAccessException)
                    { }
                }

                if (totalSize > 0)
                    _usageSensor = 100.0f - (100.0f * totalFreeSpace) / totalSize;
            }
        }

        public virtual void Close()
        {
            _drivePCs?.Dispose();
        }
    }
}
