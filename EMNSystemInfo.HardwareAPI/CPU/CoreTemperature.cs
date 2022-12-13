// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace EMNSystemInfo.HardwareAPI.CPU
{
    /// <summary>
    /// Struct that represents a CPU core temperature
    /// </summary>
    public struct CoreTemperature
    {
        /// <summary>
        /// Gets the temperature value, in degrees Celsius (°C). This property is nullable.
        /// </summary>
        public double? Value { get; internal set; }

        /// <summary>
        /// Gets the <b>T</b>junction temperature value (TjMax), in degrees Celsius (°C), it is the maximum temperature supported by the package. Only used in Intel CPUs. 
        /// </summary>
        public double TjMax { get; internal set; }

        internal double TSlope { get; set; }

        internal double Offset { get; set; }
    }
}
