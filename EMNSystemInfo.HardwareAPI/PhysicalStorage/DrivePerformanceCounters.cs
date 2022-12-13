// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Diagnostics;
using System.Linq;

namespace EMNSystemInfo.HardwareAPI.PhysicalStorage
{
    internal class DrivePerformanceCounters : IDisposable
    {
        public DrivePerformanceCounters(int driveIndex)
        {
            string[] performanceCounterInstances = new PerformanceCounterCategory("PhysicalDisk").GetInstanceNames();
            string driveInstance = (from inst in performanceCounterInstances
                                    where inst.StartsWith(driveIndex.ToString())
                                    select inst).First();

            try
            {
                _driveTimePercentageCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", driveInstance);
                _driveTimePercentageCounter.NextValue();
            }
            catch (Exception)
            { }

            try
            {
                _driveReadTimePercentageCounter = new PerformanceCounter("PhysicalDisk", "% Disk Read Time", driveInstance);
                _driveReadTimePercentageCounter.NextValue();
            }
            catch (Exception)
            { }

            try
            {
                _driveWriteTimePercentageCounter = new PerformanceCounter("PhysicalDisk", "% Disk Write Time", driveInstance);
                _driveWriteTimePercentageCounter.NextValue();
            }
            catch (Exception)
            { }

            try
            {
                _driveIdleTimePercentageCounter = new PerformanceCounter("PhysicalDisk", "% Idle Time", driveInstance);
                _driveIdleTimePercentageCounter.NextValue();
            }
            catch (Exception)
            { }

            try
            {
                _driveReadSpeedCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", driveInstance);
                _driveReadSpeedCounter.NextValue();
            }
            catch (Exception)
            { }

            try
            {
                _driveWriteSpeedCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", driveInstance);
                _driveWriteSpeedCounter.NextValue();
            }
            catch (Exception)
            { }

            try
            {
                _driveAvgResponseTimePerRead = new PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Read", driveInstance);
                _driveAvgResponseTimePerRead.NextValue();
            }
            catch (Exception)
            { }

            try
            {
                _driveAvgResponseTimePerWrite = new PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Write", driveInstance);
                _driveAvgResponseTimePerWrite.NextValue();
            }
            catch (Exception)
            { }

            try
            {
                _driveAvgResponseTimePerTransfer = new PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Transfer", driveInstance);
                _driveAvgResponseTimePerTransfer.NextValue();
            }
            catch (Exception)
            { }
        }

        private readonly PerformanceCounter _driveTimePercentageCounter;
        private readonly PerformanceCounter _driveReadTimePercentageCounter;
        private readonly PerformanceCounter _driveWriteTimePercentageCounter;
        private readonly PerformanceCounter _driveIdleTimePercentageCounter;
        private readonly PerformanceCounter _driveReadSpeedCounter;
        private readonly PerformanceCounter _driveWriteSpeedCounter;
        private readonly PerformanceCounter _driveAvgResponseTimePerRead;
        private readonly PerformanceCounter _driveAvgResponseTimePerWrite;
        private readonly PerformanceCounter _driveAvgResponseTimePerTransfer;

        public void Dispose()
        {
            _driveTimePercentageCounter?.Dispose();
            _driveReadTimePercentageCounter?.Dispose();
            _driveWriteTimePercentageCounter?.Dispose();
            _driveIdleTimePercentageCounter?.Dispose();
            _driveReadSpeedCounter?.Dispose();
            _driveWriteSpeedCounter?.Dispose();
            _driveAvgResponseTimePerRead?.Dispose();
            _driveAvgResponseTimePerWrite?.Dispose();
            _driveAvgResponseTimePerTransfer?.Dispose();
        }

        public double? DriveTime
        {
            get
            {
                try
                {
                    double? value = _driveTimePercentageCounter?.NextValue();
                    if (value > 100d)
                    {
                        return 100d;
                    }
                    return value;
                }
                catch (Exception)
                { }

                return null;
            }
        }

        public double? ReadTime
        {
            get
            {
                try
                {
                    double? value = _driveReadTimePercentageCounter?.NextValue();
                    if (value > 100d)
                    {
                        return 100d;
                    }
                    return value;
                }
                catch (Exception)
                { }

                return null;
            }
        }

        public double? WriteTime
        {
            get
            {
                try
                {
                    double? value = _driveWriteTimePercentageCounter?.NextValue();
                    if (value > 100d)
                    {
                        return 100d;
                    }
                    return value;
                }
                catch (Exception)
                { }

                return null;
            }
        }

        public double? IdleTime
        {
            get
            {
                try
                {
                    double? value = _driveIdleTimePercentageCounter.NextValue();
                    if (value > 100d)
                    {
                        return 100d;
                    }
                    return value;
                }
                catch (Exception)
                { }

                return null;
            }
        }

        public double? ReadSpeed
        {
            get
            {
                try
                {
                    return _driveReadSpeedCounter?.NextValue();
                }
                catch (Exception)
                { }

                return null;
            }
        }

        public double? WriteSpeed
        {
            get
            {
                try
                {
                    return _driveWriteSpeedCounter?.NextValue();
                }
                catch (Exception)
                { }

                return null;
            }
        }

        public double? AverageResponseTimePerRead
        {
            get
            {
                try
                {
                    return _driveAvgResponseTimePerRead?.NextValue();
                }
                catch (Exception)
                { }

                return null;
            }
        }

        public double? AverageResponseTimePerWrite
        {
            get
            {
                try
                {
                    return _driveAvgResponseTimePerWrite?.NextValue();
                }
                catch (Exception)
                { }

                return null;
            }
        }

        public double? AverageResponseTimePerTransfer
        {
            get
            {
                try
                {
                    return _driveAvgResponseTimePerTransfer?.NextValue();
                }
                catch (Exception)
                { }

                return null;
            }
        }
    }
}
