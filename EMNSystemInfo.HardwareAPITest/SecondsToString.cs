// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// All Rights Reserved.

using System;

namespace EMNSystemInfo.HardwareAPITest
{
    static class SecondsToString
    {
        public static string Convert(double seconds)
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
    }
}
