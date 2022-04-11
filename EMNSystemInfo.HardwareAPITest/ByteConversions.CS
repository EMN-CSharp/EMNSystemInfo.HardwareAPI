// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// All Rights Reserved.

using static System.Math;

namespace EMNSystemInfo.HardwareAPITest
{
    public enum Unit
    {
        Byte,
        BinaryByte,
        ByteBase2,
        BytesPerSecond,
        BitsPerSecond
    }

    public static class ByteConversions
    {
        public static string ConvertBytesToString(ulong bytes, Unit unit)
        {
            switch (unit)
            {
                case Unit.Byte:
                    if (bytes < 1000)
                    {
                        return $"{bytes} B";
                    }
                    else if (bytes > 999 && bytes < 1000000)
                    {
                        return $"{Round(bytes / 1000.00, 2)} kB";
                    }
                    else if (bytes > 999999 && bytes < 1000000000)
                    {
                        return $"{Round(bytes / 1000000.00, 2)} MB";
                    }
                    else if (bytes > 999999999 && bytes < 1000000000000)
                    {
                        return $"{Round(bytes / 1000000000.00, 2)} GB";
                    }
                    else if (bytes > 999999999999)
                    {
                        return $"{Round(bytes / 1000000000000.00, 2)} TB";
                    }
                    break;
                case Unit.BytesPerSecond:
                    if (bytes < 1000)
                    {
                        return $"{bytes} B/s";
                    }
                    else if (bytes > 999 && bytes < 1000000)
                    {
                        return $"{Round(bytes / 1000.00, 2)} kB/s";
                    }
                    else if (bytes > 999999 && bytes < 1000000000)
                    {
                        return $"{Round(bytes / 1000000.00, 2)} MB/s";
                    }
                    else if (bytes > 999999999 && bytes < 1000000000000)
                    {
                        return $"{Round(bytes / 1000000000.00, 2)} GB/s";
                    }
                    else if (bytes > 999999999999)
                    {
                        return $"{Round(bytes / 1000000000000.00, 2)} TB/s";
                    }
                    break;
                case Unit.BitsPerSecond:
                    if (bytes < 1000)
                    {
                        return $"{bytes} bps";
                    }
                    else if (bytes > 999 && bytes < 1000000)
                    {
                        return $"{Round(bytes / 1000.00, 2)} kbps";
                    }
                    else if (bytes > 999999 && bytes < 1000000000)
                    {
                        return $"{Round(bytes / 1000000.00, 2)} Mbps";
                    }
                    else if (bytes > 999999999 && bytes < 1000000000000)
                    {
                        return $"{Round(bytes / 1000000000.00, 2)} Gbps";
                    }
                    else if (bytes > 999999999999)
                    {
                        return $"{Round(bytes / 1000000000000.00, 2)} Tbps";
                    }
                    break;
                case Unit.BinaryByte:
                    if (bytes < 1024)
                    {
                        return $"{bytes} B";
                    }
                    else if (bytes > 1023 && bytes < 1048576)
                    {
                        return $"{Round(bytes / 1024.00, 2)} KiB";
                    }
                    else if (bytes > 1048575 && bytes < 1073741824)
                    {
                        return $"{Round(bytes / 1048576.00, 2)} MiB";
                    }
                    else if (bytes > 1073741823 && bytes < 1099511627776)
                    {
                        return $"{Round(bytes / 1073741824.00, 2)} GiB";
                    }
                    else if (bytes > 1099511627775)
                    {
                        return $"{Round(bytes / 1099511627776.00, 2)} TiB";
                    }
                    break;
                case Unit.ByteBase2:
                    if (bytes < 1024)
                    {
                        return $"{bytes} B";
                    }
                    else if (bytes > 1023 && bytes < 1048576)
                    {
                        return $"{Round(bytes / 1024.00, 2)} kB";
                    }
                    else if (bytes > 1048575 && bytes < 1073741824)
                    {
                        return $"{Round(bytes / 1048576.00, 2)} MB";
                    }
                    else if (bytes > 1073741823 && bytes < 1099511627776)
                    {
                        return $"{Round(bytes / 1073741824.00, 2)} GB";
                    }
                    else if (bytes > 1099511627775)
                    {
                        return $"{Round(bytes / 1099511627776.00, 2)} TB";
                    }
                    break;
            }
            return "";
        }
    }
}
