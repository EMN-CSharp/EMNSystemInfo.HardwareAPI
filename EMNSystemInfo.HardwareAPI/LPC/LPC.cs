// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace EMNSystemInfo.HardwareAPI.LPC
{
    /// <summary>
    /// Abstract class that represents a base LPC chip
    /// </summary>
    public abstract class LPC
    {
        /// <summary>
        /// Gets the chip name
        /// </summary>
        public string ChipName { get; internal set; }

        /// <summary>
        /// Gets the LPC chip type
        /// </summary>
        public LPCType Type { get; internal set; }

        /// <summary>
        /// Updates the LPC sensors.
        /// </summary>
        public virtual void Update() { }

        /// <summary>
        /// Frees the resources used in this class. It is not necessary to call this method, <see cref="LPCChips.Dispose"/> does all the work.
        /// </summary>
        public virtual void Close() { }
    }
}
