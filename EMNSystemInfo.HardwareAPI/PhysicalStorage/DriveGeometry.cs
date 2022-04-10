// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

namespace EMNSystemInfo.HardwareAPI.PhysicalStorage
{
    public class DriveGeometry
    {
        public ulong? Cylinders { get; internal set; }

        public ulong? Heads { get; internal set; }

        public ulong? Tracks { get; internal set; }

        public ulong? Sectors { get; internal set; }

        public ulong? TracksPerCylinder { get; internal set; }

        public ulong? SectorsPerTrack { get; internal set; }

        public ulong? BytesPerSector { get; internal set; }
    }
}
