// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;

namespace EMNSystemInfo.HardwareAPI.GPU
{
    /// <summary>
    /// GPU type. These values are required to get the proper instance. For example, if your GPU type is an <see cref="NvidiaGPU"/>, you can convert your <see cref="GPU"/> instance into <see cref="HardwareAPI.GPU.NvidiaGPU"/> using type casting.
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
        protected private string _d3dDeviceId;
        protected private string _gpuName;
        protected private ulong _gpuDedicatedMemoryUsage;
        protected private ulong _gpuDedicatedMemoryLimit;
        protected private NodeUsageSensor[] _gpuNodeUsage;
        protected private DateTime[] _gpuNodeUsagePrevTick;
        protected private long[] _gpuNodeUsagePrevValue;
        protected private ulong _gpuSharedMemoryUsage;
        protected private ulong _gpuSharedMemoryLimit;

        /// <summary>
        /// Gets the GPU name
        /// </summary>
        public string Name => _gpuName;

        /// <summary>
        /// Gets the dedicated memory usage, in bytes.
        /// </summary>
        public ulong DedicatedMemoryUsage => _gpuDedicatedMemoryUsage;

        /// <summary>
        /// Gets the dedicated memory limit, in bytes.
        /// </summary>
        public ulong DedicatedMemoryLimit => _gpuDedicatedMemoryLimit;

        /// <summary>
        /// Gets the usages of all the GPU nodes.
        /// </summary>
        public NodeUsageSensor[] NodeUsage => _gpuNodeUsage;

        /// <summary>
        /// Gets the shared memory usage, in bytes.
        /// </summary>
        public ulong SharedMemoryUsage => _gpuSharedMemoryUsage;

        /// <summary>
        /// Gets the shared memory limit, in bytes.
        /// </summary>
        public ulong SharedMemoryLimit => _gpuSharedMemoryLimit;

        /// <summary>
        /// Gets the GPU type.
        /// </summary>
        public GPUType Type { get; internal set; } = GPUType.Generic;

        protected private void Initialize(string d3dDevId)
        {
            _d3dDeviceId = d3dDevId;

            if (_d3dDeviceId != null && D3DDisplayDevice.GetDeviceInfoByIdentifier(_d3dDeviceId, out D3DDisplayDevice.D3DDeviceInfo deviceInfo))
            {
                _gpuNodeUsage = new NodeUsageSensor[deviceInfo.Nodes.Length];
                _gpuNodeUsagePrevValue = new long[deviceInfo.Nodes.Length];
                _gpuNodeUsagePrevTick = new DateTime[deviceInfo.Nodes.Length];

                foreach (NodeUsageSensor node in deviceInfo.Nodes)
                {
                    _gpuNodeUsagePrevTick[node.Id] = node.QueryTime;
                    _gpuNodeUsagePrevValue[node.Id] = node._runningTime;
                }
            }
        }

        /// <summary>
        /// Updates all the GPU properties.
        /// </summary>
        public virtual void Update()
        {
            if (_d3dDeviceId != null && D3DDisplayDevice.GetDeviceInfoByIdentifier(_d3dDeviceId, out D3DDisplayDevice.D3DDeviceInfo deviceInfo))
            {
                _gpuDedicatedMemoryUsage = deviceInfo.GpuDedicatedUsed;
                _gpuDedicatedMemoryLimit = deviceInfo.GpuDedicatedLimit;

                _gpuSharedMemoryUsage = deviceInfo.GpuSharedUsed;
                _gpuSharedMemoryLimit = deviceInfo.GpuSharedLimit;
                
                for (int i = 0; i < deviceInfo.Nodes.Length; i++)
                {
                    NodeUsageSensor node = deviceInfo.Nodes[i];

                    long runningTimeDiff = node._runningTime - _gpuNodeUsagePrevValue[node.Id];
                    long timeDiff = node.QueryTime.Ticks - _gpuNodeUsagePrevTick[node.Id].Ticks;

                    node.Value = 100f * runningTimeDiff / timeDiff;
                    _gpuNodeUsagePrevValue[node.Id] = node._runningTime;
                    _gpuNodeUsagePrevTick[node.Id] = node.QueryTime;

                    _gpuNodeUsage[i] = node;
                }
            }
        }

        public virtual void Close() { }
    }
}
