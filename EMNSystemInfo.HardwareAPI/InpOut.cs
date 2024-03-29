﻿// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using static EMNSystemInfo.HardwareAPI.NativeInterop.InpOut;
using static EMNSystemInfo.HardwareAPI.NativeInterop.Kernel32;

namespace EMNSystemInfo.HardwareAPI
{
    internal static class InpOut
    {
        private static string _filePath;
        private static IntPtr _libraryHandle;
        private static MapPhysToLinDelegate _mapPhysToLin;
        private static UnmapPhysicalMemoryDelegate _unmapPhysicalMemory;

        public static bool IsOpen { get; private set; }

        public static bool Open()
        {
            if (IsOpen)
                return true;


            _filePath = GetFilePath();
            if (_filePath != null && (File.Exists(_filePath) || Extract(_filePath)))
            {
                _libraryHandle = LoadLibrary(_filePath);
                if (_libraryHandle != IntPtr.Zero)
                {
                    IntPtr mapPhysToLinAddress = GetProcAddress(_libraryHandle, "MapPhysToLin");
                    IntPtr unmapPhysicalMemoryAddress = GetProcAddress(_libraryHandle, "UnmapPhysicalMemory");

                    if (mapPhysToLinAddress != IntPtr.Zero)
                        _mapPhysToLin = Marshal.GetDelegateForFunctionPointer<MapPhysToLinDelegate>(mapPhysToLinAddress);

                    if (unmapPhysicalMemoryAddress != IntPtr.Zero)
                        _unmapPhysicalMemory = Marshal.GetDelegateForFunctionPointer<UnmapPhysicalMemoryDelegate>(unmapPhysicalMemoryAddress);

                    IsOpen = true;
                }
            }

            if (!IsOpen)
                DeleteDll();

            return IsOpen;
        }

        public static void Close()
        {
            if (_libraryHandle != IntPtr.Zero)
            {
                FreeLibrary(_libraryHandle);
                DeleteDll();

                _libraryHandle = IntPtr.Zero;
            }

            IsOpen = false;
        }

        public static byte[] ReadMemory(IntPtr baseAddress, uint size)
        {
            if (_mapPhysToLin != null && _unmapPhysicalMemory != null)
            {
                IntPtr pdwLinAddr = _mapPhysToLin(baseAddress, size, out IntPtr pPhysicalMemoryHandle);
                if (pdwLinAddr != IntPtr.Zero)
                {
                    byte[] bytes = new byte[size];
                    Marshal.Copy(pdwLinAddr, bytes, 0, bytes.Length);
                    _unmapPhysicalMemory(pPhysicalMemoryHandle, pdwLinAddr);

                    return bytes;
                }
            }

            return null;
        }

        private static void DeleteDll()
        {
            try
            {
                // try to delete the DLL
                if (_filePath != null && File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }

                _filePath = null;
            }
            catch
            { }
        }

        private static string GetFilePath()
        {
            string filePath;

            try
            {
                filePath = Path.GetTempFileName();
                if (!string.IsNullOrEmpty(filePath))
                    return Path.ChangeExtension(filePath, ".dll");
            }
            catch (IOException)
            { }


            const string fileName = "inpout.dll";

            try
            {
                ProcessModule processModule = Process.GetCurrentProcess().MainModule;
                if (!string.IsNullOrEmpty(processModule?.FileName))
                    return Path.Combine(Path.GetDirectoryName(processModule.FileName) ?? string.Empty, fileName);
            }
            catch
            {
                // Continue with the other options.
            }

            filePath = GetPathFromAssembly(Assembly.GetExecutingAssembly());
            if (!string.IsNullOrEmpty(filePath))
                return Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, fileName);


            filePath = GetPathFromAssembly(typeof(Ring0).Assembly);
            if (!string.IsNullOrEmpty(filePath))
                return Path.Combine(Path.GetDirectoryName(filePath) ?? string.Empty, fileName);


            return null;


            static string GetPathFromAssembly(Assembly assembly)
            {
                try
                {
                    string location = assembly?.Location;
                    return !string.IsNullOrEmpty(location) ? location : null;
                }
                catch
                {
                    return null;
                }
            }
        }

        private static bool Extract(string filePath)
        {
            string resourceName = $"EMNSystemInfo.HardwareAPI.Resources.{(Environment.Is64BitOperatingSystem ? "inpoutx64.dll" : "inpout32.dll")}";

            Assembly assembly = typeof(Ring0).Assembly;

            string[] names = assembly.GetManifestResourceNames();
            byte[] buffer = null;

            for (int i = 0; i < names.Length; i++)
            {
                if (names[i].Replace('\\', '.') == resourceName)
                {
                    using Stream stream = assembly.GetManifestResourceStream(names[i]);

                    if (stream != null)
                    {
                        buffer = new byte[stream.Length];
                        stream.Read(buffer, 0, buffer.Length);
                    }
                }
            }

            if (buffer == null)
                return false;


            try
            {
                using FileStream target = new(filePath, FileMode.Create);

                target.Write(buffer, 0, buffer.Length);
                target.Flush();
            }
            catch
            {
                // for example there is not enough space on the disk
                return false;
            }

            // make sure the file is actually written to the file system
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (stopwatch.ElapsedMilliseconds < 2000)
            {
                try
                {
                    if (File.Exists(filePath) && new FileInfo(filePath).Length == buffer.Length)
                        return true;
                }
                catch
                { }

                Thread.Sleep(1);
            }

            // file still has not the right size, something is wrong
            return false;
        }
    }
}
