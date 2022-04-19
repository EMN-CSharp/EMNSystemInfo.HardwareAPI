// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using EMNSystemInfo.HardwareAPI.CPU;
using EMNSystemInfo.HardwareAPI.NativeInterop;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EMNSystemInfo.HardwareAPI.GPU
{
    /// <summary>
    /// Class that represents a list of GPUs installed on the system.
    /// </summary>
    public static class GPUs
    {
        private static bool _gpusAreLoaded = false;
        private static ATIADLxx.ADLStatus _status;

        /// <summary>
        /// GPUs list
        /// </summary>
        public static GPU[] List { get; private set; } = Array.Empty<GPU>();

        /// <summary>
        /// Loads the GPUs supported by this library into the <see cref="List"/> property.
        /// </summary>
        public static void LoadGPUs()
        {
            if (!_gpusAreLoaded)
            {
                List<GPU> gpus = new();

                #region NVIDIA GPU listing

                if (NVAPI.IsAvailable)
                {
                    NVAPI.NvPhysicalGpuHandle[] handles = new NVAPI.NvPhysicalGpuHandle[NVAPI.MAX_PHYSICAL_GPUS];
                    NVAPI.NvStatus status = NVAPI.NvAPI_EnumPhysicalGPUs(handles, out int count);

                    IDictionary<NVAPI.NvPhysicalGpuHandle, NVAPI.NvDisplayHandle> displayHandles = new Dictionary<NVAPI.NvPhysicalGpuHandle, NVAPI.NvDisplayHandle>();
                    if (NVAPI.NvAPI_EnumNvidiaDisplayHandle != null && NVAPI.NvAPI_GetPhysicalGPUsFromDisplay != null)
                    {
                        status = NVAPI.NvStatus.OK;
                        int i = 0;
                        while (status == NVAPI.NvStatus.OK)
                        {
                            NVAPI.NvDisplayHandle displayHandle = new();
                            status = NVAPI.NvAPI_EnumNvidiaDisplayHandle(i, ref displayHandle);
                            i++;

                            if (status == NVAPI.NvStatus.OK)
                            {
                                NVAPI.NvPhysicalGpuHandle[] handlesFromDisplay = new NVAPI.NvPhysicalGpuHandle[NVAPI.MAX_PHYSICAL_GPUS];
                                if (NVAPI.NvAPI_GetPhysicalGPUsFromDisplay(displayHandle, handlesFromDisplay, out uint countFromDisplay) == NVAPI.NvStatus.OK)
                                {
                                    for (int j = 0; j < countFromDisplay; j++)
                                    {
                                        if (!displayHandles.ContainsKey(handlesFromDisplay[j]))
                                            displayHandles.Add(handlesFromDisplay[j], displayHandle);
                                    }
                                }
                            }
                        }
                    }

                    for (int i = 0; i < count; i++)
                    {
                        displayHandles.TryGetValue(handles[i], out NVAPI.NvDisplayHandle displayHandle);
                        gpus.Add(new NvidiaGPU(i, handles[i], displayHandle));
                    }
                }

                #endregion

                #region AMD GPU listing

                try
                {
                    _status = ATIADLxx.ADL_Main_Control_Create(1);

                    if (_status == ATIADLxx.ADLStatus.ADL_OK)
                    {
                        int numberOfAdapters = 0;
                        ATIADLxx.ADL_Adapter_NumberOfAdapters_Get(ref numberOfAdapters);

                        if (numberOfAdapters > 0)
                        {
                            List<AMDGPU> potentialHardware = new();

                            ATIADLxx.ADLAdapterInfo[] adapterInfo = new ATIADLxx.ADLAdapterInfo[numberOfAdapters];
                            if (ATIADLxx.ADL_Adapter_AdapterInfo_Get(adapterInfo) == ATIADLxx.ADLStatus.ADL_OK)
                            {
                                for (int i = 0; i < numberOfAdapters; i++)
                                {
                                    ATIADLxx.ADL_Adapter_Active_Get(adapterInfo[i].AdapterIndex, out int isActive);
                                    ATIADLxx.ADL_Adapter_ID_Get(adapterInfo[i].AdapterIndex, out int adapterId);

                                    if (!string.IsNullOrEmpty(adapterInfo[i].UDID) && adapterInfo[i].VendorID == ATIADLxx.ATI_VENDOR_ID)
                                        potentialHardware.Add(new AMDGPU(adapterInfo[i]));
                                }
                            }

                            IEnumerable<IGrouping<string, AMDGPU>> amdGpus = potentialHardware.GroupBy(x => $"{x.BusNumber}-{x.DeviceNumber}");
                            foreach (var amdGpu in amdGpus)
                            {
                                if (amdGpu.FirstOrDefault() != default)
                                {
                                    gpus.Add(amdGpu.FirstOrDefault());
                                }
                            }
                        }
                    }
                }
                catch (DllNotFoundException)
                { }

                #endregion

                #region Intel integrated GPU listing

                if (!Processors.ProcessorsAreLoaded)
                {
                    Processors.LoadProcessors();
                }
                IList<IntelCPU> intelCPUs = (from iCPU in Processors.List
                                             where iCPU.Type == ProcessorType.IntelCPU
                                             select (IntelCPU)iCPU).ToList();
                if (intelCPUs.Count > 0)
                {
                    string[] ids = D3DDisplayDevice.GetDeviceIdentifiers();
                    for (int i = 0; i < ids.Length; i++)
                    {
                        string deviceId = ids[i];
                        bool isIntel = deviceId.IndexOf("VEN_8086", StringComparison.Ordinal) != -1;

                        if (isIntel)
                        {
                            if (D3DDisplayDevice.GetDeviceInfoByIdentifier(deviceId, out D3DDisplayDevice.D3DDeviceInfo deviceInfo))
                            {
                                if (deviceInfo.Integrated)
                                {
                                    // It may seem strange to only use the first cpu here, but in-case we have a multi cpu system with integrated graphics (does that exist?),
                                    // we would pick up the multiple device identifiers above and would add one instance for each CPU.
                                    gpus.Add(new IntelIntegratedGPU(intelCPUs[0], deviceId));
                                }
                            }
                        }
                    }
                }

                #endregion

                List = gpus.ToArray();
                _gpusAreLoaded = true;
            }
        }

        /// <summary>
        /// Frees the resources used by <see cref="GPU"/> instances.
        /// </summary>
        public static void DisposeGPUs()
        {
            if (_gpusAreLoaded)
            {
                try
                {
                    if (_status == ATIADLxx.ADLStatus.ADL_OK)
                        ATIADLxx.ADL_Main_Control_Destroy();
                }
                catch (Exception)
                { }

                foreach (GPU gpu in List)
                {
                    gpu.Close();
                }
                List = Array.Empty<GPU>();
            }
        }
    }
}
