// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael M�ller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;

namespace EMNSystemInfo.HardwareAPI.CPU
{
    /// <summary>
    /// Processors information. It is required to initialize the library.
    /// </summary>
    public static class Processors
    {
        private static CPUID[][][] _threads;
        private static bool _processorsAreLoaded = false;

        /// <summary>
        /// Gets a value that represents if the installed processors are loaded on the <see cref="List"/> property. Returns <see langword="true"/> if processors are loaded, otherwise, <see langword="false"/>.
        /// </summary>
        public static bool ProcessorsAreLoaded => _processorsAreLoaded;

        /// <summary>
        /// Processors list
        /// </summary>
        public static Processor[] List { get; private set; } = Array.Empty<Processor>();

        /// <summary>
        /// Loads all the installed processors into the <see cref="List"/> property.
        /// </summary>
        /// <returns><see langword="false"/> if the processors were loaded before or the library is not initialized. Otherwise, <see langword="true"/>.</returns>
        public static bool Load()
        {
            if (!LibrarySettings.IsInitialized || _processorsAreLoaded)
            {
                return false;
            }
            else
            {
                List<Processor> processors = new();

                CPUID[][] processorThreads = GetProcessorThreads();
                _threads = new CPUID[processorThreads.Length][][];

                int index = 0;
                foreach (CPUID[] threads in processorThreads)
                {
                    if (threads.Length == 0)
                        continue;


                    CPUID[][] coreThreads = GroupThreadsByCore(threads);
                    _threads[index] = coreThreads;

                    if (LibrarySettings.UserIsAdmin)
                    {
                        switch (threads[0].Vendor)
                        {
                            case ProcessorVendor.Intel:
                                processors.Add(new IntelCPU(index, coreThreads));
                                break;
                            case ProcessorVendor.AMD:
                                switch (threads[0].Family)
                                {
                                    case 0x0F:
                                        processors.Add(new AMD0FCPU(index, coreThreads));
                                        break;
                                    case 0x10:
                                    case 0x11:
                                    case 0x12:
                                    case 0x14:
                                    case 0x15:
                                    case 0x16:
                                        processors.Add(new AMD10CPU(index, coreThreads));
                                        break;
                                    case 0x17:
                                    case 0x19:
                                        processors.Add(new AMD17CPU(index, coreThreads));
                                        break;
                                    default:
                                        processors.Add(new Processor(index, coreThreads));
                                        break;
                                }

                                break;
                            default:
                                processors.Add(new Processor(index, coreThreads));
                                break;
                        }
                    }
                    else
                    {
                        processors.Add(new Processor(index, coreThreads));
                    }

                    index++;
                }

                List = processors.ToArray();
                _processorsAreLoaded = true;
                return true;
            }
        }

        /// <summary>
        /// Deletes all processors from the <see cref="List"/> property.
        /// </summary>
        public static void Dispose()
        {
            _processorsAreLoaded = false;
            List = Array.Empty<Processor>();
        }

        private static CPUID[][] GetProcessorThreads()
        {
            List<CPUID> threads = new List<CPUID>();
            
            for (int i = 0; i < ThreadAffinity.ProcessorGroupCount; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    try
                    {
                        if (!ThreadAffinity.IsValid(GroupAffinity.Single((ushort)i, j)))
                            continue;


                        var cpuid = CPUID.Get(i, j);
                        if (cpuid != null)
                            threads.Add(cpuid);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // All cores found.
                        break;
                    }
                }
            }


            SortedDictionary<uint, List<CPUID>> processors = new SortedDictionary<uint, List<CPUID>>();
            foreach (CPUID thread in threads)
            {
                processors.TryGetValue(thread.ProcessorId, out List<CPUID> list);
                if (list == null)
                {
                    list = new List<CPUID>();
                    processors.Add(thread.ProcessorId, list);
                }

                list.Add(thread);
            }

            CPUID[][] processorThreads = new CPUID[processors.Count][];
            int index = 0;
            foreach (List<CPUID> list in processors.Values)
            {
                processorThreads[index] = list.ToArray();
                index++;
            }

            return processorThreads;
        }

        private static CPUID[][] GroupThreadsByCore(IEnumerable<CPUID> threads)
        {
            SortedDictionary<uint, List<CPUID>> cores = new SortedDictionary<uint, List<CPUID>>();
            foreach (CPUID thread in threads)
            {
                cores.TryGetValue(thread.CoreId, out List<CPUID> coreList);
                if (coreList == null)
                {
                    coreList = new List<CPUID>();
                    cores.Add(thread.CoreId, coreList);
                }

                coreList.Add(thread);
            }

            CPUID[][] coreThreads = new CPUID[cores.Count][];
            int index = 0;
            foreach (List<CPUID> list in cores.Values)
            {
                coreThreads[index] = list.ToArray();
                index++;
            }

            return coreThreads;
        }
    }
}
