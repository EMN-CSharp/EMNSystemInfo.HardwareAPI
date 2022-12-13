// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;

namespace EMNSystemInfo.HardwareAPI
{
    /// <summary>
    /// Class that represents the library version info
    /// </summary>
    public static class VersionInfo
    {
        /// <summary>
        /// Gets a string literal representing the library version
        /// </summary>
        public const string Version = "0.3.0";

        /// <summary>
        /// Gets a <see cref="System.Version"/> instance based on <see cref="Version"/> string literal.
        /// </summary>
        public static Version GetVersion() => new(Version);

        /// <summary>
        /// Gets the library build date
        /// </summary>
        public static DateTime BuildDate => new(year: 2022, month: 12, day: 7);
    }
}
