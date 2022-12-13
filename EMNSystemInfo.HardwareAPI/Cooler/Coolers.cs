// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Security;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using HidSharp;
using EMNSystemInfo.HardwareAPI.NativeInterop;
using HidSharp.Reports;
using System.Runtime;

namespace EMNSystemInfo.HardwareAPI.Cooler
{
    public static class Coolers
    {
        /// <summary>
        /// Gets a value that represents if the cooler devices are loaded on the <see cref="List"/> property. Returns <see langword="true"/> if coolers are loaded, otherwise, <see langword="false"/>.
        /// </summary>
        public static bool CoolersAreLoaded { get; private set; } = false;

        public static ICooler[] List { get; internal set; } = Array.Empty<ICooler>();

        public static void Load()
        {
            if (!CoolersAreLoaded)
            {
                List<ICooler> coolers = new();

                #region AeroCool P7-H1 cooler listing

                foreach (HidDevice dev in DeviceList.Local.GetHidDevices(0x2E97))
                {
                    int hubno = dev.ProductID - 0x1000;
                    if (dev.DevicePath.Contains("mi_02") && (hubno >= 1) && (hubno <= 8))
                    {
                        var device = new AeroCoolP7H1(dev);
                        coolers.Add(device);
                    }
                }

                #endregion

                #region AquaComputer coolers listing

                foreach (HidDevice dev in DeviceList.Local.GetHidDevices(0x0c70))
                {
                    switch (dev.ProductID)
                    {
                        case 0xF00E:
                        {
                            var device = new AquaComputerD5Next(dev);
                            coolers.Add(device);
                            break;
                        }
                        case 0xF0b6:
                        {
                            var device = new AquaComputerAquastreamXT(dev);
                            coolers.Add(device);
                            break;
                        }
                        case 0xF003:
                        {
                            var device = new AquaComputerMPS(dev);
                            coolers.Add(device);
                            break;
                        }
                        case 0xF011:
                        {
                            var device = new AquaComputerOcto(dev);
                            coolers.Add(device);
                            break;
                        }
                    }
                }

                #endregion

                #region Heatmaster cooler listing

                string[] portNames = GetRegistryPortNames();
                for (int i = 0; i < portNames.Length; i++)
                {
                    bool isValid = false;
                    try
                    {
                        using (SerialPort serialPort = new SerialPort(portNames[i], 38400, Parity.None, 8, StopBits.One))
                        {
                            serialPort.NewLine = ((char)0x0D).ToString();
                            try
                            {
                                serialPort.Open();
                            }
                            catch (UnauthorizedAccessException)
                            {
                            }

                            if (serialPort.IsOpen)
                            {
                                serialPort.DiscardInBuffer();
                                serialPort.DiscardOutBuffer();
                                serialPort.Write(new byte[] { 0xAA }, 0, 1);

                                int j = 0;
                                while (serialPort.BytesToRead == 0 && j < 10)
                                {
                                    Thread.Sleep(20);
                                    j++;
                                }

                                if (serialPort.BytesToRead > 0)
                                {
                                    bool flag = false;
                                    while (serialPort.BytesToRead > 0 && !flag)
                                    {
                                        flag |= serialPort.ReadByte() == 0xAA;
                                    }

                                    if (flag)
                                    {
                                        serialPort.WriteLine("[0:0]RH");
                                        try
                                        {
                                            int k = 0;
                                            int revision = 0;
                                            while (k < 5)
                                            {
                                                string line = ReadLine(serialPort, 100);
                                                if (line.StartsWith("-[0:0]RH:", StringComparison.Ordinal))
                                                {
                                                    revision = int.Parse(line.Substring(9), CultureInfo.InvariantCulture);
                                                    break;
                                                }

                                                k++;
                                            }

                                            isValid = revision == 770;
                                        }
                                        catch (TimeoutException)
                                        {
                                        }
                                    }
                                    else
                                    {
                                    }
                                }
                                else
                                {
                                }

                                serialPort.DiscardInBuffer();
                            }
                            else
                            {
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }

                    if (isValid)
                    {
                        coolers.Add(new Heatmaster(portNames[i]));
                    }
                }

                #endregion

                #region NZXT Kraken X3 cooler listing

                foreach (HidDevice dev in DeviceList.Local.GetHidDevices(0x1e71))
                {
                    switch (dev.ProductID)
                    {
                        case 0x2007:
                        {
                            var device = new NZXTKrakenX3(dev);
                            coolers.Add(device);
                            break;
                        }
                    }
                }

                #endregion

                #region TBalancer cooler listing
                ListTBalancerCoolers(coolers);
                #endregion

                List = coolers.ToArray();

                CoolersAreLoaded = true;
            }
        }

        private static void ListTBalancerCoolers(List<ICooler> coolers)
        {
            uint numDevices;

            try
            {
                if (!Ftd2xx.DllExists())
                {
                    return;
                }

                if (Ftd2xx.FT_CreateDeviceInfoList(out numDevices) != Ftd2xx.FT_STATUS.FT_OK)
                {
                    return;
                }
            }
            catch (Exception e) when (e is DllNotFoundException or ArgumentNullException or EntryPointNotFoundException or BadImageFormatException)
            {
                return;
            }

            Ftd2xx.FT_DEVICE_INFO_NODE[] info = new Ftd2xx.FT_DEVICE_INFO_NODE[numDevices];
            if (Ftd2xx.FT_GetDeviceInfoList(info, ref numDevices) != Ftd2xx.FT_STATUS.FT_OK)
            {
                return;
            }

            // make sure numDevices is not larger than the info array
            if (numDevices > info.Length)
                numDevices = (uint)info.Length;

            for (int i = 0; i < numDevices; i++)
            {
                // the T-Balancer always uses an FT232BM
                if (info[i].Type != Ftd2xx.FT_DEVICE.FT_DEVICE_232BM)
                {
                    continue;
                }

                Ftd2xx.FT_STATUS status = Ftd2xx.FT_Open(i, out Ftd2xx.FT_HANDLE handle);
                if (status != Ftd2xx.FT_STATUS.FT_OK)
                {
                    continue;
                }

                Ftd2xx.FT_SetBaudRate(handle, 19200);
                Ftd2xx.FT_SetDataCharacteristics(handle, 8, 1, 0);
                Ftd2xx.FT_SetFlowControl(handle, Ftd2xx.FT_FLOW_CONTROL.FT_FLOW_RTS_CTS, 0x11, 0x13);
                Ftd2xx.FT_SetTimeouts(handle, 1000, 1000);
                Ftd2xx.FT_Purge(handle, Ftd2xx.FT_PURGE.FT_PURGE_ALL);

                status = Ftd2xx.Write(handle, new byte[] { 0x38 });
                if (status != Ftd2xx.FT_STATUS.FT_OK)
                {
                    Ftd2xx.FT_Close(handle);
                    continue;
                }

                bool isValid = false;
                byte protocolVersion = 0;

                int j = 0;
                while (Ftd2xx.BytesToRead(handle) == 0 && j < 2)
                {
                    Thread.Sleep(100);
                    j++;
                }

                if (Ftd2xx.BytesToRead(handle) > 0)
                {
                    if (Ftd2xx.ReadByte(handle) == TBalancer.StartFlag)
                    {
                        while (Ftd2xx.BytesToRead(handle) < 284 && j < 5)
                        {
                            Thread.Sleep(100);
                            j++;
                        }

                        int length = Ftd2xx.BytesToRead(handle);
                        if (length >= 284)
                        {
                            byte[] data = new byte[285];
                            data[0] = TBalancer.StartFlag;
                            for (int k = 1; k < data.Length; k++)
                                data[k] = Ftd2xx.ReadByte(handle);

                            // check protocol version 2X (protocols seen: 2C, 2A, 28)
                            isValid = (data[274] & 0xF0) == 0x20;
                            protocolVersion = data[274];
                        }
                    }
                }

                Ftd2xx.FT_Purge(handle, Ftd2xx.FT_PURGE.FT_PURGE_ALL);
                Ftd2xx.FT_Close(handle);

                if (isValid)
                {
                    coolers.Add(new TBalancer(i, protocolVersion));
                }
            }
        }

        private static string ReadLine(SerialPort port, int timeout)
        {
            int i = 0;
            StringBuilder builder = new StringBuilder();
            while (i < timeout)
            {
                while (port.BytesToRead > 0)
                {
                    byte b = (byte)port.ReadByte();
                    switch (b)
                    {
                        case 0xAA: return ((char)b).ToString();
                        case 0x0D: return builder.ToString();
                        default:
                            builder.Append((char)b);
                            break;
                    }
                }

                i++;
                Thread.Sleep(1);
            }

            throw new TimeoutException();
        }

        private static string[] GetRegistryPortNames()
        {
            List<string> result = new();
            string[] paths = { string.Empty, "&MI_00" };
            try
            {
                foreach (string path in paths)
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB\VID_10C4&PID_EA60" + path);
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            RegistryKey subKey = key.OpenSubKey(subKeyName + "\\" + "Device Parameters");
                            if (subKey?.GetValue("PortName") is string name && !result.Contains(name))
                                result.Add(name);
                        }
                    }
                }
            }
            catch (SecurityException)
            { }

            return result.ToArray();
        }

        public static void Dispose()
        {
            if (CoolersAreLoaded)
            {
                foreach (ICooler cooler in List)
                {
                    cooler.Close();
                }
                List = Array.Empty<ICooler>();

                CoolersAreLoaded = false;
            }
        }
    }
}
