// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

namespace EMNSystemInfo.HardwareAPI.LPC.EC
{
    internal class EmbeddedControllerSource
    {
        public EmbeddedControllerSource(ECSensorType type, ushort register, byte size = 1, float factor = 1.0f, int blank = int.MaxValue)
        {
            Register = register;
            Size = size;
            Type = type;
            Factor = factor;
            Blank = blank;
        }

        public ushort Register { get; }
        public byte Size { get; }
        public float Factor { get; }

        public int Blank { get; }

        public EmbeddedControllerReader Reader { get; }

        public ECSensorType Type { get; }
    }
}
