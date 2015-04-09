using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ShawLib
{
    public class Memory : IDisposable
    {
        IntPtr hProc;
        bool disposed;

        public Memory(Process proc)
            : this(proc.Id)
        { }

        public Memory(int procId)
        {
            hProc = NativeMethods.OpenProcess(ProcessAccessFlags.All, false, procId);
        }

        public T Read<T>(IntPtr address) where T : IConvertible
        {
            byte[] buffer;
            var type = typeof(T);
            if (type == typeof(string))
            {
                var list = new List<byte>();
                while (true)
                {
                    var b = Read<byte>(address);
                    if (b == 0)
                        break;

                    list.Add(b);
                    address += 0x1;
                }
                buffer = list.ToArray();
            }
            else
            {
                var size = type.MemSize();
                buffer = Read(address, size);
            }
            return buffer.To<T>();
        }

        public byte[] Read(IntPtr address, int size)
        {
            var buffer = new byte[size];
            IntPtr read;
            NativeMethods.ReadProcessMemory(hProc, address, buffer, size, out read);
            if ((int)read != size)
                throw new MemoryException("could not read value, maybe it's protected?");
            return buffer;
        }

        public void Write(IntPtr address, IConvertible value)
        {
            var type = value.GetType();
            if (type == typeof(string))
                value += "\0";

            var bytes = value.GetBytes();
            IntPtr written;
            NativeMethods.WriteProcessMemory(hProc, address, bytes, bytes.Length, out written);
            if ((int)written != bytes.Length)
                throw new MemoryException("could not write value, maybe it's protected?");
        }

        public IntPtr FindPattern(byte[] pattern, string mask = null)
        {
            if (mask == null)
                mask = new String('x', pattern.Length);
            else if (mask.Length != pattern.Length)
                throw new MemoryException("The pattern length does not match with the mask length");

            SystemInfo systemInfo;
            NativeMethods.GetSystemInfo(out systemInfo);
            var address = systemInfo.MinimumApplicationAddress;

            MemoryBasicInformation info;
            var sizeOf = (uint)Marshal.SizeOf(typeof(MemoryBasicInformation));
            while ((long)address < (long)systemInfo.MaximumApplicationAddress)
            {
                NativeMethods.VirtualQueryEx(hProc, (IntPtr)address, out info, sizeOf);

                // if the memory chunk is accessible
                if ((info.Protect == AllocationProtect.PAGE_READWRITE || info.Protect == AllocationProtect.PAGE_READONLY) &&
                    info.State == MemoryRegionState.Commit)
                {
                    var buffer = Read(info.BaseAddress, (int)info.RegionSize);
                    for (int bOffset = 0; bOffset < buffer.Length - mask.Length; bOffset++)
                        if (maskCheck(buffer, bOffset, mask, pattern))
                            return new IntPtr((long)address + bOffset);
                }

                address += (int)info.RegionSize;
            }

            return IntPtr.Zero;
        }

        bool maskCheck(byte[] buffer, int offset, string mask, byte[] pattern)
        {
            if (buffer.Length - offset < mask.Length)
                return false;

            for (int pOffset = 0; pOffset < pattern.Length; pOffset++)
                if (mask[pOffset] == '?')
                    continue;
                else if (pattern[pOffset] != buffer[offset + pOffset])
                    return false;

            return true;
        }

        public uint RemoveProtection(IntPtr address, UIntPtr size)
        {
            uint protection = 0;
            NativeMethods.VirtualProtectEx(hProc, address, size, 0x40, out protection);
            return protection;
        }

        public void AddProtection(IntPtr address, UIntPtr size, uint protection)
        {
            NativeMethods.VirtualProtectEx(hProc, address, size, protection, out protection);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                NativeMethods.CloseHandle(hProc);

            disposed = true;
        }
    }
}
