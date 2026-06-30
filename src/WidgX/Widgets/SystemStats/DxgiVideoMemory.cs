using System;
using System.Runtime.InteropServices;

namespace WidgX.Widgets.SystemStats;

/// <summary>
/// Reads the total dedicated VRAM of the primary discrete adapter via DXGI
/// (IDXGIAdapter1::GetDesc1). No elevation required; returns null on failure.
/// System-wide VRAM *usage* comes from a performance counter instead, because
/// DXGI's QueryVideoMemoryInfo only reports the calling process's usage.
/// </summary>
public class DxgiVideoMemory
{
    public ulong? GetTotalDedicatedBytes()
    {
        IDXGIFactory1? factory = null;
        var factoryPtr = IntPtr.Zero;
        try
        {
            var factoryIid = typeof(IDXGIFactory1).GUID;
            if (CreateDXGIFactory1(ref factoryIid, out factoryPtr) != 0 || factoryPtr == IntPtr.Zero)
            {
                return null;
            }

            factory = (IDXGIFactory1)Marshal.GetObjectForIUnknown(factoryPtr);

            ulong best = 0;
            for (uint i = 0; ; i++)
            {
                if (factory.EnumAdapters1(i, out var adapterPtr) != 0 || adapterPtr == IntPtr.Zero)
                {
                    break;
                }

                var adapterIid = typeof(IDXGIAdapter1).GUID;
                var qi = Marshal.QueryInterface(adapterPtr, ref adapterIid, out var typedPtr);
                Marshal.Release(adapterPtr);
                if (qi != 0 || typedPtr == IntPtr.Zero) continue;

                var adapter = (IDXGIAdapter1)Marshal.GetObjectForIUnknown(typedPtr);
                Marshal.Release(typedPtr);
                try
                {
                    if (adapter.GetDesc1(out var desc) != 0) continue;
                    if ((desc.Flags & DXGI_ADAPTER_FLAG_SOFTWARE) != 0) continue;

                    var total = desc.DedicatedVideoMemory.ToUInt64();
                    if (total > best) best = total;
                }
                finally
                {
                    Marshal.ReleaseComObject(adapter);
                }
            }

            return best > 0 ? best : null;
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            if (factory != null) Marshal.ReleaseComObject(factory);
            if (factoryPtr != IntPtr.Zero) Marshal.Release(factoryPtr);
        }
    }

    private const uint DXGI_ADAPTER_FLAG_SOFTWARE = 2;

    [DllImport("dxgi.dll")]
    private static extern int CreateDXGIFactory1(ref Guid riid, out IntPtr ppFactory);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DXGI_ADAPTER_DESC1
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Description;
        public uint VendorId;
        public uint DeviceId;
        public uint SubSysId;
        public uint Revision;
        public UIntPtr DedicatedVideoMemory;
        public UIntPtr DedicatedSystemMemory;
        public UIntPtr SharedSystemMemory;
        public long AdapterLuid;
        public uint Flags;
    }

    // Unused vtable slots are declared as placeholders so the method we call
    // lands at the correct vtable index.
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("770aae78-f26f-4dba-a829-253c83d1b387")]
    private interface IDXGIFactory1
    {
        void _SetPrivateData();
        void _SetPrivateDataInterface();
        void _GetPrivateData();
        void _GetParent();
        void _EnumAdapters();
        void _MakeWindowAssociation();
        void _GetWindowAssociation();
        void _CreateSwapChain();
        void _CreateSoftwareAdapter();
        [PreserveSig] int EnumAdapters1(uint adapter, out IntPtr ppAdapter);
        [PreserveSig] int IsCurrent();
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("29038f61-3839-4626-91fd-086879011a05")]
    private interface IDXGIAdapter1
    {
        void _SetPrivateData();
        void _SetPrivateDataInterface();
        void _GetPrivateData();
        void _GetParent();
        void _EnumOutputs();
        void _GetDesc();
        void _CheckInterfaceSupport();
        [PreserveSig] int GetDesc1(out DXGI_ADAPTER_DESC1 desc);
    }
}
