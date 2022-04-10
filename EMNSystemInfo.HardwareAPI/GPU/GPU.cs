// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;

namespace EMNSystemInfo.HardwareAPI.GPU
{
    /// <summary>
    /// GPU type. These values are required to get the proper instance. For example. if your GPU type is an <see cref="NvidiaGPU"/>, you can convert your <see cref="GPU"/> instance into <see cref="HardwareAPI.GPU.NvidiaGPU"/> using type casting.
    /// </summary>
    public enum GPUType
    {
        Generic,

        /// <summary>
        /// AMD GPU. Convert your <see cref="GPU"/> instance into <see cref="HardwareAPI.GPU.AMDGPU"/>.
        /// </summary>
        AMDGPU,

        /// <summary>
        /// NVIDIA GPU. Convert your <see cref="GPU"/> instance into <see cref="HardwareAPI.GPU.NvidiaGPU"/>.
        /// </summary>
        NvidiaGPU,

        /// <summary>
        /// Intel integrated GPU. Convert your <see cref="GPU"/> instance into <see cref="HardwareAPI.GPU.IntelIntegratedGPU"/>.
        /// </summary>
        IntelIntegratedGPU
    }

    /// <summary>
    /// Class that represents an individual generic GPU
    /// </summary>
    public class GPU
    {
        protected string _d3dDeviceId;
        protected string _gpuName;
        protected ulong _gpuDedicatedMemoryUsage;
        private bool _arraysInitialized = false;
        protected NodeUsageSensor[] _gpuNodeUsage;
        protected DateTime[] _gpuNodeUsagePrevTick;
        protected long[] _gpuNodeUsagePrevValue;
        protected ulong _gpuSharedMemoryUsage;

        /// <summary>
        /// Gets the GPU name
        /// </summary>
        public string Name => _gpuName;

        /// <summary>
        /// Gets the dedicated memory usage, in bytes.
        /// </summary>
        public ulong DedicatedMemoryUsage => _gpuDedicatedMemoryUsage;

        /// <summary>
        /// Gets the usages of all the GPU nodes.
        /// </summary>
        public NodeUsageSensor[] NodeUsage => _gpuNodeUsage;

        /// <summary>
        /// Gets the shared memory usage, in bytes.
        /// </summary>
        public ulong SharedMemoryUsage => _gpuSharedMemoryUsage;

        /// <summary>
        /// Gets the GPU type.
        /// </summary>
        public GPUType Type { get; internal set; } = GPUType.Generic;

        public virtual void Update()
        {
            if (_d3dDeviceId != null && D3DDisplayDevice.GetDeviceInfoByIdentifier(_d3dDeviceId, out D3DDisplayDevice.D3DDeviceInfo deviceInfo))
            {
                if (!_arraysInitialized)
                {
                    _gpuNodeUsage = deviceInfo.Nodes;
                    _gpuNodeUsagePrevValue = new long[deviceInfo.Nodes.Length];
                    _gpuNodeUsagePrevTick = new DateTime[deviceInfo.Nodes.Length];
                    _arraysInitialized = true;
                }

                _gpuDedicatedMemoryUsage = deviceInfo.GpuDedicatedUsed;
                _gpuSharedMemoryUsage = deviceInfo.GpuSharedUsed;
                
                foreach (NodeUsageSensor node in _gpuNodeUsage)
                {
                    long runningTimeDiff = node._runningTime - _gpuNodeUsagePrevValue[node.Id];
                    long timeDiff = node.QueryTime.Ticks - _gpuNodeUsagePrevTick[node.Id].Ticks;

                    _gpuNodeUsage[node.Id].Value = 100f * runningTimeDiff / timeDiff;
                    _gpuNodeUsagePrevValue[node.Id] = node._runningTime;
                    _gpuNodeUsagePrevTick[node.Id] = node.QueryTime;
                }
            }
        }

        public virtual void Close() { }
    }
}
