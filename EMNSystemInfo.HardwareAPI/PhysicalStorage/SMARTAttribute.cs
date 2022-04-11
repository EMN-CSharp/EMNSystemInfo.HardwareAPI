// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using EMNSystemInfo.HardwareAPI.NativeInterop;

namespace EMNSystemInfo.HardwareAPI.PhysicalStorage
{
    public class SMARTAttribute
    {
        private readonly RawValueConversion _rawValueConversion;

        public delegate double RawValueConversion(byte[] rawValue, byte value, double? parameter);

        /// <summary>
        /// Initializes a new instance of the <see cref="SMARTAttribute" /> class.
        /// </summary>
        /// <param name="id">The SMART id of the attribute.</param>
        /// <param name="name">The name of the attribute.</param>
        internal SMARTAttribute(byte id, string name) : this(id, name, ATADrive.RawToInt, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SMARTAttribute" /> class.
        /// </summary>
        /// <param name="id">The SMART id of the attribute.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <param name="rawValueConversion">
        /// A delegate for converting the raw byte
        /// array into a value (or null to use the attribute value).
        /// </param>
        internal SMARTAttribute(byte id, string name, RawValueConversion rawValueConversion) : this(id, name, rawValueConversion, null) { }

        internal SMARTAttribute(byte id, string name, RawValueConversion rawValueConversion, double? parameter)
        {
            Id = id;
            Name = name;
            Parameter = parameter;
            _rawValueConversion = rawValueConversion;
        }

        public bool HasRawValueConversion => _rawValueConversion != null;

        /// <summary>
        /// Gets the SMART identifier.
        /// </summary>
        public byte Id { get; }

        public string Name { get; }

        public double? Parameter { get; }

        internal double ConvertValue(Kernel32.SMART_ATTRIBUTE value, double? parameter = null)
        {
            if (_rawValueConversion == null)
                return value.CurrentValue;
            return _rawValueConversion(value.RawValue, value.CurrentValue, parameter);
        }
    }
}
