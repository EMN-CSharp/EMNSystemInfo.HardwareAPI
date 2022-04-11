// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.
// Ported from: https://github.com/processhacker/processhacker/blob/master/plugins/ExtendedTools/gpumon.c

using EMNSystemInfo.HardwareAPI.NativeInterop;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace EMNSystemInfo.HardwareAPI
{
    public enum NodeEngineType
    {
        Other = 0,
        _3D = 1,
        VideoDecode = 2,
        VideoEncode = 3,
        VideoProcessing = 4,
        SceneAssembly = 5,
        Copy = 6,
        Overlay = 7,
        Crypto
    }

    public struct NodeUsageSensor
    {
        internal long _runningTime;

        public TimeSpan RunningTime => TimeSpan.FromTicks(_runningTime);
        public ulong Id { get; internal set; }
        public NodeEngineType NodeEngineType { get; internal set; }
        public string NodeEngineTypeString { get; internal set; }
        internal DateTime QueryTime { get; set; }
        public double Value { get; internal set; }
    }

    internal static class D3DDisplayDevice
    {
        public static string[] GetDeviceIdentifiers()
        {
            if (CfgMgr32.CM_Get_Device_Interface_List_Size(out uint size, ref CfgMgr32.GUID_DISPLAY_DEVICE_ARRIVAL, null, CfgMgr32.CM_GET_DEVICE_INTERFACE_LIST_PRESENT) != CfgMgr32.CR_SUCCESS)
                return null;


            char[] data = new char[size];
            if (CfgMgr32.CM_Get_Device_Interface_List(ref CfgMgr32.GUID_DISPLAY_DEVICE_ARRIVAL, null, data, (uint)data.Length, CfgMgr32.CM_GET_DEVICE_INTERFACE_LIST_PRESENT) == CfgMgr32.CR_SUCCESS)
                return new string(data).Split('\0').Where(m => !string.IsNullOrEmpty(m)).ToArray();


            return null;
        }

        public static string GetActualDeviceIdentifier(string deviceIdentifier)
        {
            string identifier = deviceIdentifier;

            // For example:
            // \\?\ROOT#BasicRender#0000#{1ca05180-a699-450a-9a0c-de4fbe3ddd89}  -->  ROOT\BasicRender\0000
            // \\?\PCI#VEN_1002&DEV_731F&SUBSYS_57051682&REV_C4#6&e539058&0&00000019#{1ca05180-a699-450a-9a0c-de4fbe3ddd89}  -->  PCI\VEN_1002&DEV_731F&SUBSYS_57051682&REV_C4\6&e539058&0&00000019

            if (identifier.StartsWith(@"\\?\"))
                identifier = identifier.Substring(4);

            if (identifier.Length > 0 && identifier[identifier.Length - 1] == '}')
            {
                int lastIndex = identifier.LastIndexOf('{');
                if (lastIndex > 0)
                    identifier = identifier.Substring(0, lastIndex - 1);
            }

            identifier = identifier.Replace('#', '\\');

            return identifier;
        }

        public static bool GetDeviceInfoByIdentifier(string deviceIdentifier, out D3DDeviceInfo deviceInfo)
        {
            deviceInfo = new D3DDeviceInfo();

            OpenAdapterFromDeviceName(out uint status, deviceIdentifier, out D3DKMTH.D3DKMT_OPENADAPTERFROMDEVICENAME adapter);
            if (status != WinNT.STATUS_SUCCESS)
                return false;



            GetAdapterType(out status, adapter, out D3DKMTH.D3DKMT_ADAPTERTYPE adapterType);
            if (status != WinNT.STATUS_SUCCESS)
                return false;

            if (!adapterType.Value.HasFlag(D3DKMTH.D3DKMT_ADAPTERTYPE_FLAGS.SoftwareDevice))
                return false;

            deviceInfo.Integrated = !adapterType.Value.HasFlag(D3DKMTH.D3DKMT_ADAPTERTYPE_FLAGS.HybridIntegrated);

            GetQueryStatisticsAdapterInformation(out status, adapter, out D3DKMTH.D3DKMT_QUERYSTATISTICS_ADAPTER_INFORMATION adapterInformation);
            if (status != WinNT.STATUS_SUCCESS)
                return false;


            uint segmentCount = adapterInformation.NbSegments;
            uint nodeCount = adapterInformation.NodeCount;

            deviceInfo.Nodes = new NodeUsageSensor[nodeCount];

            var queryTime = DateTime.Now;

            for (uint nodeId = 0; nodeId < nodeCount; nodeId++)
            {
                GetNodeMetaData(out status, adapter, nodeId, out D3DKMTH.D3DKMT_NODEMETADATA nodeMetaData);
                if (status != WinNT.STATUS_SUCCESS)
                    return false;


                GetQueryStatisticsNode(out status, adapter, nodeId, out D3DKMTH.D3DKMT_QUERYSTATISTICS_NODE_INFORMATION nodeInformation);
                if (status != WinNT.STATUS_SUCCESS)
                    return false;

                deviceInfo.Nodes[nodeId] = new NodeUsageSensor
                {
                    Id = nodeId,
                    NodeEngineType = (NodeEngineType)(int)nodeMetaData.NodeData.EngineType,
                    NodeEngineTypeString = nodeMetaData.NodeData.FriendlyName,
                    _runningTime = nodeInformation.GlobalInformation.RunningTime.QuadPart,
                    QueryTime = queryTime
                };
            }

            GetSegmentSize(out status, adapter, out D3DKMTH.D3DKMT_SEGMENTSIZEINFO segmentSizeInfo);
            if (status != WinNT.STATUS_SUCCESS)
                return false;

            deviceInfo.GpuSharedLimit = segmentSizeInfo.SharedSystemMemorySize;
            deviceInfo.GpuDedicatedLimit = segmentSizeInfo.DedicatedSystemMemorySize;

            for (uint segmentId = 0; segmentId < segmentCount; segmentId++)
            {
                GetQueryStatisticsSegment(out status, adapter, segmentId, out D3DKMTH.D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION segmentInformation);
                if (status != WinNT.STATUS_SUCCESS)
                    return false;


                ulong bytesResident = segmentInformation.BytesResident;
                ulong bytesCommitted = segmentInformation.BytesCommitted;

                uint aperture = segmentInformation.Aperture;

                if (aperture == 1)
                {
                    deviceInfo.GpuSharedUsed += bytesResident;
                    deviceInfo.GpuSharedMax += bytesCommitted;
                }
                else
                {
                    deviceInfo.GpuDedicatedUsed += bytesResident;
                    deviceInfo.GpuDedicatedMax += bytesCommitted;
                }
            }

            CloseAdapter(out status, adapter);
            return status == WinNT.STATUS_SUCCESS;
        }

        private static void GetSegmentSize
        (
            out uint status,
            D3DKMTH.D3DKMT_OPENADAPTERFROMDEVICENAME adapter,
            out D3DKMTH.D3DKMT_SEGMENTSIZEINFO sizeInformation)
        {
            IntPtr segmentSizePtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(D3DKMTH.D3DKMT_SEGMENTSIZEINFO)));
            sizeInformation = new D3DKMTH.D3DKMT_SEGMENTSIZEINFO();
            Marshal.StructureToPtr(sizeInformation, segmentSizePtr, true);

            var queryAdapterInfo = new D3DKMTH.D3DKMT_QUERYADAPTERINFO
            {
                hAdapter = adapter.hAdapter,
                Type = D3DKMTH.KMTQUERYADAPTERINFOTYPE.KMTQAITYPE_GETSEGMENTSIZE,
                pPrivateDriverData = segmentSizePtr,
                PrivateDriverDataSize = Marshal.SizeOf(typeof(D3DKMTH.D3DKMT_SEGMENTSIZEINFO))
            };

            status = GDI32.D3DKMTQueryAdapterInfo(ref queryAdapterInfo);
            sizeInformation = Marshal.PtrToStructure<D3DKMTH.D3DKMT_SEGMENTSIZEINFO>(segmentSizePtr);
            Marshal.FreeHGlobal(segmentSizePtr);
        }

        private static void GetNodeMetaData(out uint status, D3DKMTH.D3DKMT_OPENADAPTERFROMDEVICENAME adapter, uint nodeId, out D3DKMTH.D3DKMT_NODEMETADATA nodeMetaDataResult)
        {
            IntPtr nodeMetaDataPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(D3DKMTH.D3DKMT_NODEMETADATA)));
            nodeMetaDataResult = new D3DKMTH.D3DKMT_NODEMETADATA { NodeOrdinalAndAdapterIndex = nodeId };
            Marshal.StructureToPtr(nodeMetaDataResult, nodeMetaDataPtr, true);

            var queryAdapterInfo = new D3DKMTH.D3DKMT_QUERYADAPTERINFO
            {
                hAdapter = adapter.hAdapter,
                Type = D3DKMTH.KMTQUERYADAPTERINFOTYPE.KMTQAITYPE_NODEMETADATA,
                pPrivateDriverData = nodeMetaDataPtr,
                PrivateDriverDataSize = Marshal.SizeOf(typeof(D3DKMTH.D3DKMT_NODEMETADATA))
            };

            status = GDI32.D3DKMTQueryAdapterInfo(ref queryAdapterInfo);
            nodeMetaDataResult = Marshal.PtrToStructure<D3DKMTH.D3DKMT_NODEMETADATA>(nodeMetaDataPtr);
            Marshal.FreeHGlobal(nodeMetaDataPtr);
        }

        private static void GetQueryStatisticsNode(out uint status, D3DKMTH.D3DKMT_OPENADAPTERFROMDEVICENAME adapter, uint nodeId, out D3DKMTH.D3DKMT_QUERYSTATISTICS_NODE_INFORMATION nodeInformation)
        {
            var queryElement = new D3DKMTH.D3DKMT_QUERYSTATISTICS_QUERY_ELEMENT { QueryNode = { NodeId = nodeId } };
            
            var queryStatistics = new D3DKMTH.D3DKMT_QUERYSTATISTICS
            {
                AdapterLuid = adapter.AdapterLuid, Type = D3DKMTH.D3DKMT_QUERYSTATISTICS_TYPE.D3DKMT_QUERYSTATISTICS_NODE, QueryElement = queryElement
            };

            status = GDI32.D3DKMTQueryStatistics(ref queryStatistics);

            nodeInformation = queryStatistics.QueryResult.NodeInformation;
        }

        private static void GetQueryStatisticsSegment
        (
            out uint status,
            D3DKMTH.D3DKMT_OPENADAPTERFROMDEVICENAME adapter,
            uint segmentId,
            out D3DKMTH.D3DKMT_QUERYSTATISTICS_SEGMENT_INFORMATION segmentInformation)
        {
            var queryElement = new D3DKMTH.D3DKMT_QUERYSTATISTICS_QUERY_ELEMENT { QuerySegment = { SegmentId = segmentId } };

            var queryStatistics = new D3DKMTH.D3DKMT_QUERYSTATISTICS
            {
                AdapterLuid = adapter.AdapterLuid, Type = D3DKMTH.D3DKMT_QUERYSTATISTICS_TYPE.D3DKMT_QUERYSTATISTICS_SEGMENT, QueryElement = queryElement
            };

            status = GDI32.D3DKMTQueryStatistics(ref queryStatistics);

            segmentInformation = queryStatistics.QueryResult.SegmentInformation;
        }

        private static void GetQueryStatisticsAdapterInformation
        (
            out uint status,
            D3DKMTH.D3DKMT_OPENADAPTERFROMDEVICENAME adapter,
            out D3DKMTH.D3DKMT_QUERYSTATISTICS_ADAPTER_INFORMATION adapterInformation)
        {
            var queryStatistics = new D3DKMTH.D3DKMT_QUERYSTATISTICS { AdapterLuid = adapter.AdapterLuid, Type = D3DKMTH.D3DKMT_QUERYSTATISTICS_TYPE.D3DKMT_QUERYSTATISTICS_ADAPTER, };

            status = GDI32.D3DKMTQueryStatistics(ref queryStatistics);

            adapterInformation = queryStatistics.QueryResult.AdapterInformation;
        }

        private static void GetAdapterType(out uint status, D3DKMTH.D3DKMT_OPENADAPTERFROMDEVICENAME adapter, out D3DKMTH.D3DKMT_ADAPTERTYPE adapterTypeResult)
        {
            IntPtr adapterTypePtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(D3DKMTH.D3DKMT_ADAPTERTYPE)));
            var queryAdapterInfo = new D3DKMTH.D3DKMT_QUERYADAPTERINFO
            {
                hAdapter = adapter.hAdapter,
                Type = D3DKMTH.KMTQUERYADAPTERINFOTYPE.KMTQAITYPE_ADAPTERTYPE,
                pPrivateDriverData = adapterTypePtr,
                PrivateDriverDataSize = Marshal.SizeOf(typeof(D3DKMTH.D3DKMT_ADAPTERTYPE))
            };

            status = GDI32.D3DKMTQueryAdapterInfo(ref queryAdapterInfo);
            adapterTypeResult = Marshal.PtrToStructure<D3DKMTH.D3DKMT_ADAPTERTYPE>(adapterTypePtr);
            Marshal.FreeHGlobal(adapterTypePtr);
        }

        private static void OpenAdapterFromDeviceName(out uint status, string displayDeviceName, out D3DKMTH.D3DKMT_OPENADAPTERFROMDEVICENAME adapter)
        {
            adapter = new D3DKMTH.D3DKMT_OPENADAPTERFROMDEVICENAME { pDeviceName = displayDeviceName };
            status = GDI32.D3DKMTOpenAdapterFromDeviceName(ref adapter);
        }

        private static void CloseAdapter(out uint status, D3DKMTH.D3DKMT_OPENADAPTERFROMDEVICENAME adapter)
        {
            var closeAdapter = new D3DKMTH.D3DKMT_CLOSEADAPTER { hAdapter = adapter.hAdapter };
            status = GDI32.D3DKMTCloseAdapter(ref closeAdapter);
        }

        public struct D3DDeviceInfo
        {
            public ulong GpuSharedLimit;
            public ulong GpuDedicatedLimit;

            public ulong GpuSharedUsed;
            public ulong GpuDedicatedUsed;

            public ulong GpuSharedMax;
            public ulong GpuDedicatedMax;

            public NodeUsageSensor[] Nodes;
            public bool Integrated;
}
    }
}
