// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using EMNSystemInfo.HardwareAPI.NativeInterop;

namespace EMNSystemInfo.HardwareAPI.CPU
{
    /// <summary>
    /// Abstract AMD CPU class. Don't use it, it is meant for internal use but it cannot be <see langword="internal"/>. 
    /// </summary>
    public abstract class AMDCPU : Processor
    {
        internal AMDCPU(int processorIndex, CPUID[][] cpuId) : base(processorIndex, cpuId)
        { }

        protected uint GetPciAddress(byte function, ushort deviceId)
        {
            // assemble the pci address
            uint address = Ring0.GetPciAddress(PCI_BUS, (byte)(PCI_BASE_DEVICE + Index), function);

            // verify that we have the correct bus, device and function
            if (!Ring0.ReadPciConfig(address, DEVICE_VENDOR_ID_REGISTER, out uint deviceVendor))
                return WinRing0.INVALID_PCI_ADDRESS;

            if (deviceVendor != (deviceId << 16 | AMD_VENDOR_ID))
                return WinRing0.INVALID_PCI_ADDRESS;


            return address;
        }

        private const ushort AMD_VENDOR_ID = 0x1022;
        private const byte DEVICE_VENDOR_ID_REGISTER = 0;
        private const byte PCI_BASE_DEVICE = 0x18;

        private const byte PCI_BUS = 0;
        // ReSharper restore InconsistentNaming
    }
}
