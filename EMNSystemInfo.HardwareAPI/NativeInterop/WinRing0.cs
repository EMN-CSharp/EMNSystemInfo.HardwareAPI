﻿namespace EMNSystemInfo.HardwareAPI.NativeInterop
{
    internal static class WinRing0
    {
        public const uint INVALID_PCI_ADDRESS = 0xFFFFFFFF;

        private const uint OLS_TYPE = 40000;

        public static readonly Kernel32.IOControlCode
            IOCTL_OLS_GET_REFCOUNT = new Kernel32.IOControlCode(OLS_TYPE, 0x801, Kernel32.IOControlCode.Access.Any);

        public static readonly Kernel32.IOControlCode
            IOCTL_OLS_READ_MSR = new Kernel32.IOControlCode(OLS_TYPE, 0x821, Kernel32.IOControlCode.Access.Any);

        public static readonly Kernel32.IOControlCode
            IOCTL_OLS_WRITE_MSR = new Kernel32.IOControlCode(OLS_TYPE, 0x822, Kernel32.IOControlCode.Access.Any);

        public static readonly Kernel32.IOControlCode
            IOCTL_OLS_READ_IO_PORT_BYTE = new Kernel32.IOControlCode(OLS_TYPE, 0x833, Kernel32.IOControlCode.Access.Read);

        public static readonly Kernel32.IOControlCode
            IOCTL_OLS_WRITE_IO_PORT_BYTE = new Kernel32.IOControlCode(OLS_TYPE, 0x836, Kernel32.IOControlCode.Access.Write);

        public static readonly Kernel32.IOControlCode
            IOCTL_OLS_READ_PCI_CONFIG = new Kernel32.IOControlCode(OLS_TYPE, 0x851, Kernel32.IOControlCode.Access.Read);

        public static readonly Kernel32.IOControlCode
            IOCTL_OLS_WRITE_PCI_CONFIG = new Kernel32.IOControlCode(OLS_TYPE, 0x852, Kernel32.IOControlCode.Access.Write);

        public static readonly Kernel32.IOControlCode
            IOCTL_OLS_READ_MEMORY = new Kernel32.IOControlCode(OLS_TYPE, 0x841, Kernel32.IOControlCode.Access.Read);
    }
}