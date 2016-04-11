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

        static int SimpleBoyerMooreSearch(byte[] haystack, byte[] needle)
        {
            var lookup = new int[256];
            for (int i = 0; i < lookup.Length; i++)
                lookup[i] = needle.Length;

            for (int i = 0; i < needle.Length; i++)
                lookup[needle[i]] = needle.Length - i - 1;

            var index = needle.Length - 1;
            var lastByte = needle[needle.Length - 1];
            while (index < haystack.Length)
            {
                var checkByte = haystack[index];
                if (haystack[index] == lastByte)
                {
                    var found = true;
                    for (int j = needle.Length - 2; j >= 0; j--)
                        if (haystack[index - needle.Length + j + 1] != needle[j])
                        {
                            found = false;
                            break;
                        }

                    if (found)
                        return index - needle.Length + 1;
                    else
                        index++;
                }
                else
                {
                    index += lookup[checkByte];
                }
            }
            return -1;
        }

        public IntPtr Search(byte[] pattern, string mask = null, bool includeReadonly = false)
        {
            if (mask != null && mask.Length != pattern.Length)
                throw new MemoryException("The pattern length does not match with the mask length");

            SystemInfo systemInfo;
            NativeMethods.GetSystemInfo(out systemInfo);
            var maxAddress = (long)systemInfo.MaximumApplicationAddress;
            var address = systemInfo.MinimumApplicationAddress;

            MemoryBasicInformation info;
            var sizeOf = (uint)Marshal.SizeOf(typeof(MemoryBasicInformation));
            while ((long)address < maxAddress)
            {
                NativeMethods.VirtualQueryEx(hProc, address, out info, sizeOf);

                if ((info.Protect == AllocationProtect.PAGE_READWRITE || (includeReadonly && info.Protect == AllocationProtect.PAGE_READONLY)) &&
                    info.State == MemoryRegionState.Commit)
                {
                    var buffer = Read(info.BaseAddress, (int)info.RegionSize);
                    if (mask == null)
                    {
                        var offset = SimpleBoyerMooreSearch(buffer, pattern);
                        if (offset != -1)
                            return (IntPtr)((long)address + offset);
                    }
                    else
                    {
                        for (int offset = 0; offset < buffer.Length - pattern.Length; offset++)
                            if (checkMask(buffer, offset, pattern, mask))
                                return new IntPtr((long)address + offset);
                    }
                }

                address += (int)info.RegionSize;
            }

            return IntPtr.Zero;
        }

        bool checkMask(byte[] bytes, int offset, byte[] pattern, string mask)
        {
            if (bytes.Length - offset < pattern.Length)
                return false;

            for (int pOffset = 0; pOffset < pattern.Length; pOffset++)
                if (mask[pOffset] == '?')
                    continue;
                else if (pattern[pOffset] != bytes[offset + pOffset])
                    return false;

            return true;
        }

        public uint RemoveProtection(IntPtr address, int size)
        {
            uint protection = 0;
            NativeMethods.VirtualProtectEx(hProc, address, (UIntPtr)size, 0x40, out protection);
            return protection;
        }

        public void AddProtection(IntPtr address, int size, uint protection)
        {
            NativeMethods.VirtualProtectEx(hProc, address, (UIntPtr)size, protection, out protection);
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
