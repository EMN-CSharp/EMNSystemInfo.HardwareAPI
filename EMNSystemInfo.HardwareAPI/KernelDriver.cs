// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using EMNSystemInfo.HardwareAPI.NativeInterop;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using Microsoft.Win32.SafeHandles;
using static EMNSystemInfo.HardwareAPI.NativeInterop.AdvAPI32;

namespace EMNSystemInfo.HardwareAPI
{
    internal class KernelDriver
    {
        private readonly string _driverId;
        private readonly string _serviceName;
        private readonly string _displayName;
        private SafeFileHandle _device;

        public KernelDriver(string serviceName, string displayName, string driverId)
        {
            _serviceName = serviceName;
            _displayName = displayName;
            _driverId = driverId;
        }

        public bool IsOpen
        {
            get { return _device != null; }
        }

        public bool Install(string path, out string errorMessage)
        {
            IntPtr manager = OpenSCManager(null, null, SC_MANAGER_ACCESS_MASK.SC_MANAGER_ALL_ACCESS);
            if (manager == IntPtr.Zero)
            {
                errorMessage = "OpenSCManager returned zero.";
                return false;
            }

            IntPtr service = CreateService(manager,
                                           _serviceName,
                                           _displayName,
                                           SERVICE_ACCESS_MASK.SERVICE_ALL_ACCESS,
                                           SERVICE_TYPE.SERVICE_KERNEL_DRIVER,
                                           SERVICE_START.SERVICE_DEMAND_START,
                                           SERVICE_ERROR.SERVICE_ERROR_NORMAL,
                                           path,
                                           null, null, null, null, null);

            if (service == IntPtr.Zero)
            {
                int error = Marshal.GetHRForLastWin32Error();
                if (error == Kernel32.ERROR_SERVICE_EXISTS)
                {
                    errorMessage = "Service already exists";
                    return false;
                }

                errorMessage = "CreateService returned the error: " + Marshal.GetExceptionForHR(error).Message;
                CloseServiceHandle(manager);
                return false;
            }

            if (!StartService(service, 0, null))
            {
                int error = Marshal.GetHRForLastWin32Error();
                if (error != Kernel32.ERROR_SERVICE_ALREADY_RUNNING)
                {
                    errorMessage = "StartService returned the error: " + Marshal.GetExceptionForHR(error).Message;
                    CloseServiceHandle(service);
                    CloseServiceHandle(manager);
                    return false;
                }
            }

            CloseServiceHandle(service);
            CloseServiceHandle(manager);

            try
            {
                // restrict the driver access to system (SY) and builtin admins (BA)
                // TODO: replace with a call to IoCreateDeviceSecure in the driver
                FileInfo fileInfo = new(@"\\.\" + _driverId);
                FileSecurity fileSecurity = fileInfo.GetAccessControl();
                fileSecurity.SetSecurityDescriptorSddlForm("O:BAG:SYD:(A;;FA;;;SY)(A;;FA;;;BA)");
                fileInfo.SetAccessControl(fileSecurity);
            }
            catch
            { }

            errorMessage = null;
            return true;
        }

        public bool Open()
        {
            IntPtr fileHandle = Kernel32.CreateFile(@"\\.\" + _driverId, 0xC0000000, FileShare.None, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
            _device = new SafeFileHandle(fileHandle, true);
            if (_device.IsInvalid)
            {
                _device.Close();
                _device.Dispose();
                _device = null;
            }

            return _device != null;
        }

        public bool DeviceIOControl(Kernel32.IOControlCode ioControlCode, object inBuffer)
        {
            if (_device == null)
                return false;


            return Kernel32.DeviceIoControl(_device, ioControlCode, inBuffer, inBuffer == null ? 0 : (uint)Marshal.SizeOf(inBuffer), null, 0, out uint _, IntPtr.Zero);
        }

        public bool DeviceIOControl<T>(Kernel32.IOControlCode ioControlCode, object inBuffer, ref T outBuffer)
        {
            if (_device == null)
                return false;


            object boxedOutBuffer = outBuffer;
            bool b = Kernel32.DeviceIoControl(_device,
                                              ioControlCode,
                                              inBuffer,
                                              inBuffer == null ? 0 : (uint)Marshal.SizeOf(inBuffer),
                                              boxedOutBuffer,
                                              (uint)Marshal.SizeOf(boxedOutBuffer),
                                              out uint _,
                                              IntPtr.Zero);

            outBuffer = (T)boxedOutBuffer;
            return b;
        }

        public bool DeviceIOControl<T>(Kernel32.IOControlCode ioControlCode, object inBuffer, ref T[] outBuffer)
        {
            if (_device == null)
                return false;


            object boxedOutBuffer = outBuffer;
            bool b = Kernel32.DeviceIoControl(_device,
                                              ioControlCode,
                                              inBuffer,
                                              inBuffer == null ? 0 : (uint)Marshal.SizeOf(inBuffer),
                                              boxedOutBuffer,
                                              (uint)(Marshal.SizeOf(typeof(T)) * outBuffer.Length),
                                              out uint _,
                                              IntPtr.Zero);

            outBuffer = (T[])boxedOutBuffer;
            return b;
        }

        public void Close()
        {
            if (_device != null)
            {
                _device.Close();
                _device.Dispose();
                _device = null;
            }
        }

        public bool Delete()
        {
            IntPtr manager = OpenSCManager(null, null, SC_MANAGER_ACCESS_MASK.SC_MANAGER_ALL_ACCESS);
            if (manager == IntPtr.Zero)
                return false;


            IntPtr service = OpenService(manager, _serviceName, SERVICE_ACCESS_MASK.SERVICE_ALL_ACCESS);
            if (service == IntPtr.Zero)
            {
                CloseServiceHandle(manager);
                return true;
            }

            SERVICE_STATUS status = new();
            ControlService(service, SERVICE_CONTROL.SERVICE_CONTROL_STOP, ref status);
            DeleteService(service);
            CloseServiceHandle(service);
            CloseServiceHandle(manager);

            return true;
        }
    }
}
