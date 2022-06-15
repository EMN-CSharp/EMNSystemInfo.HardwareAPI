// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// All Rights Reserved.

using System;
using EMNSystemInfo.HardwareAPI;
using System.Linq;

namespace EMNSystemInfo.HardwareAPITest
{
    public static class Extensions
    {
        public static string ToSecondUnitString(this double seconds)
        {
            double nanoseconds = seconds * 1000000000d;

            if (Math.Round(nanoseconds, 2) == 0d)
            {
                return "0 s";
            }
            else if (nanoseconds < 1000d)
            {
                return $"{Math.Round(nanoseconds, 2)} ns";
            }
            else if (nanoseconds > 999d && nanoseconds < 1000000d)
            {
                return $"{Math.Round(nanoseconds / 1000d, 2)} µs";
            }
            else if (nanoseconds > 999999d && nanoseconds < 1000000000d)
            {
                return $"{Math.Round(nanoseconds / 1000000d, 2)} ms";
            }
            else if (nanoseconds > 999999999d)
            {
                return $"{Math.Round(nanoseconds / 1000000000d, 2)} s";
            }

            return "";
        }

        public static string GetNodeName(this NodeEngineType nodeEngineType, string nodeEngTypeStrWhenOther)
        {
            string nodeName = "<Unknown>";
            switch (nodeEngineType)
            {
                case NodeEngineType.Other:
                    if (string.IsNullOrEmpty(nodeEngTypeStrWhenOther))
                    {
                        nodeName = "Other";
                    }
                    else
                    {
                        nodeName = nodeEngTypeStrWhenOther;
                    }
                    break;
                case NodeEngineType._3D:
                    nodeName = "3D";
                    break;
                case NodeEngineType.VideoDecode:
                    nodeName = "Video Decode";
                    break;
                case NodeEngineType.VideoEncode:
                    nodeName = "Video Encode";
                    break;
                case NodeEngineType.VideoProcessing:
                    nodeName = "Video Processing";
                    break;
                case NodeEngineType.SceneAssembly:
                    nodeName = "Scene Assembly";
                    break;
                case NodeEngineType.Copy:
                    nodeName = "Copy";
                    break;
                case NodeEngineType.Overlay:
                    nodeName = "Overlay";
                    break;
                case NodeEngineType.Crypto:
                    nodeName = "Cryptography";
                    break;
            }

            return nodeName;
        }

        public static string ToHexString(this byte[] buffer)
        {
            string[] hexStrArr = new string[buffer.Length];

            for (long i = 0; i < buffer.LongLength; i++)
            {
                hexStrArr[i] = buffer[i].ToString("X2");
            }

            return string.Join("", hexStrArr.Reverse());
        }

        public static string ToDataUnitString(this double bytes)
            => ToDataUnitString((ulong)bytes);

        public static string ToDataUnitString(this long bytes)
            => ToDataUnitString((ulong)bytes);

        public static string ToDataUnitString(this ulong bytes)
        {
            if (bytes < 1024)
            {
                return $"{bytes} B";
            }
            else if (bytes > 1023 && bytes < 1048576)
            {
                return $"{Math.Round(bytes / 1024d, 2)} KiB";
            }
            else if (bytes > 1048575 && bytes < 1073741824)
            {
                return $"{Math.Round(bytes / 1048576d, 2)} MiB";
            }
            else if (bytes > 1073741823 && bytes < 1099511627776)
            {
                return $"{Math.Round(bytes / 1073741824d, 2)} GiB";
            }
            else if (bytes > 1099511627775)
            {
                return $"{Math.Round(bytes / 1099511627776d, 2)} TiB";
            }

            return "N/A";
        }

        public static string ToDataSpeedUnitString(this double bytesPerSecond)
            => ToDataSpeedUnitString((ulong)bytesPerSecond);

        public static string ToDataSpeedUnitString(this ulong bytesPerSecond)
        {
            if (bytesPerSecond < 1000)
            {
                return $"{bytesPerSecond} B/s";
            }
            else if (bytesPerSecond > 999 && bytesPerSecond < 1000000)
            {
                return $"{Math.Round(bytesPerSecond / 1000d, 2)} kB/s";
            }
            else if (bytesPerSecond > 999999 && bytesPerSecond < 1000000000)
            {
                return $"{Math.Round(bytesPerSecond / 1000000d, 2)} MB/s";
            }
            else if (bytesPerSecond > 999999999 && bytesPerSecond < 1000000000000)
            {
                return $"{Math.Round(bytesPerSecond / 1000000000d, 2)} GB/s";
            }
            else if (bytesPerSecond > 999999999999)
            {
                return $"{Math.Round(bytesPerSecond / 1000000000000d, 2)} TB/s";
            }

            return "N/A";
        }
    }
}
