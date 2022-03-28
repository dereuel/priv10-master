// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include <krabs/kernel_providers.hpp>

namespace Microsoft { namespace O365 { namespace Security { namespace ETW { namespace Kernel {


#define CREATE_CONVENIENCE_KERNEL_PROVIDER(__name__, __value__, __guid__)     \
    public ref class __name__ : public KernelProvider {                       \
    public:                                                                   \
        __name__()                                                            \
        : KernelProvider(__value__, __guid__)                                 \
        {}                                                                    \
    };

    /// <summary>Converts a GUID to a Guid</summary>
    Guid FromGuid(const GUID &guid)
    {
        return Guid(guid.Data1, guid.Data2, guid.Data3,
            guid.Data4[0], guid.Data4[1],
            guid.Data4[2], guid.Data4[3],
            guid.Data4[4], guid.Data4[5],
            guid.Data4[6], guid.Data4[7]);
    }

    /// <summary>A provider that enables ALPC events.</summary>
    CREATE_CONVENIENCE_KERNEL_PROVIDER(
        AlpcProvider,
        EVENT_TRACE_FLAG_ALPC,
        FromGuid(krabs::guids::alpc));

    /// <summary>A provider that enables debug print events.</summary>
    CREATE_CONVENIENCE_KERNEL_PROVIDER(
        DebugPrintProvider,
        EVENT_TRACE_FLAG_DBGPRINT,
        FromGuid(krabs::guids::debug));

    /// <summary>A provider that enables disk io events (like flush).</summary>
    CREATE_CONVENIENCE_KERNEL_PROVIDER(
        DiskIoProvider,
        EVENT_TRACE_FLAG_DISK_IO,
        FromGuid(krabs::guids::disk_io));

    /// <summary>A provider that enables beginning of disk io events.</summary>
    CREATE_CONVENIENCE_KERNEL_PROVIDER(
        DiskInitIoProvider,
        EVENT_TRACE_FLAG_DISK_IO_INIT,
        FromGuid(krabs::guids::disk_io));

    /// <summary>A provider that enables thread dispatch events.</summary>
    CREATE_CONVENIENCE_KERNEL_PROVIDER(
        ThreadDispatchProvider,
        EVENT_TRACE_FLAG_DISPATCHER,
        FromGuid(krabs::guids::thread));

    /// <summary>A provider that enables image load events.</summary>
    CREATE_CONVENIENCE_KERNEL_PROVIDER(
        ImageLoadProvider,
        EVENT_TRACE_FLAG_IMAGE_LOAD,
        FromGuid(krabs::guids::image_load));

    /// <summary>A provider that enables memory hard fault events.</summary>
    CREATE_CONVENIENCE_KERNEL_PROVIDER(
        MemoryHardFaultProvider,
        EVENT_TRACE_FLAG_MEMORY_HARD_FAULTS,
        FromGuid(krabs::guids::page_fault));

    /// <summary>A provider that enables memory page fault events.</summary>
    CREATE_CONVENIENCE_KERNEL_PROVIDER(
        MemoryPageFaultProvider,
        EVENT_TRACE_FLAG_MEMORY_PAGE_FAULTS,
        FromGuid(krabs::guids::page_fault));

    /// <summary>A provider that enables network tcp/ip events.</summary>
    CREATE_CONVENIENCE_KERNEL_PROVIDER(
        NetworkTcpipProvider,
        EVENT_TRACE_FLAG_NETWORK_TCPIP,
        FromGuid(krabs::guids::tcp_ip));

    /// <summary>A provider that enables network udp/ip events.</summary>
    CREATE_CONVENIENCE_KERNEL_PROVIDER(
        NetworkUdpipProvider,
        EVENT_TRACE_FLAG_NETWORK_TCPIP,
        FromGuid(krabs::guids::udp_ip));

    /// <summary>A provider that enables process events.</summary>
    CREATE_CONVENIENCE_KERNEL_PROVIDER(
        ProcessProvider,
        EVENT_TRACE_FLAG_PROCESS,
        FromGuid(krabs::guids::process));

    /// <summary>A provider that enables process counter events.</summary>
    CREATE_CONVENIENCE_KERNEL_PROVIDER(
        ProcessCounterProvider,
        EVENT_TRACE_FLAG_PROCESS_COUNTERS,
        FromGuid(krabs::guids::process));

    /// <summary>A provider that enables profiling events.</summary>
    CREATE_CONVENIENCE_KERNEL_PROVIDER(
        ProfileProvider,
        EVENT_TRACE_FLAG_PROFILE,
        FromGuid(krabs::guids::perf_info));

    /// <summary>A provider that enables registry events.</summary>
    CREATE_CONVENIENCE_KERNEL_PROVIDER(
        RegistryProvider,
        EVENT_TRACE_FLAG_REGISTRY,
        FromGuid(krabs::guids::registry));

    /// <summary>A provider that enables split IO events.</summary>
    CREATE_CONVENIENCE_KERNEL_PROVIDER(
        SplitIoProvider,
        EVENT_TRACE_FLAG_SPLIT_IO,
        FromGuid(krabs::guids::split_io));

    /// <summary>A provider that enables system call events.</summary>
    CREATE_CONVENIENCE_KERNEL_PROVIDER(
        SystemCallProvider,
        EVENT_TRACE_FLAG_SYSTEMCALL,
        FromGuid(krabs::guids::system_trace));

    /// <summary>A provider that enables thread start and stop events.</summary>
    CREATE_CONVENIENCE_KERNEL_PROVIDER(
        ThreadProvider,
        EVENT_TRACE_FLAG_THREAD,
        FromGuid(krabs::guids::thread));

#undef CREATE_CONVENIENCE_KERNEL_PROVIDER

} } } } }