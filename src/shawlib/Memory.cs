using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ShawLib
{
    public static class Memory
    {
        static IntPtr hProc;

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(
             ProcessAccessFlags processAccess,
             bool bInheritHandle,
             int processId
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            [Out] byte[] lpBuffer,
            int dwSize,
            out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int nSize,
            out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress,
           UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        public static bool OpenProcess(Process proc)
        {
            return OpenProcess(proc.Id);
        }

        public static bool OpenProcess(int PID)
        {
            hProc = OpenProcess(ProcessAccessFlags.All, false, PID);
            return (hProc == IntPtr.Zero ? false : true);
        }

        public static bool CloseHandle()
        {
            if (hProc == IntPtr.Zero)
                return false;
            return CloseHandle(hProc);
        }

        public static T Read<T>(IntPtr address) where T : IConvertible
        {
            var type = typeof(T);
            if (type == typeof(string))
            {
                var buffer = new List<byte>();
                do
                {
                    var b = Read<byte>(address);
                    address += 0x1;
                }
                while (buffer[buffer.Count] != 0);
                return buffer.ToArray().To<T>();
            }
            else
            {
                var size = typeof(T).MemSize();
                var buffer = new byte[size];
                IntPtr read;
                ReadProcessMemory(hProc, address, buffer, size, out read);
                return buffer.To<T>();
            }
        }

        public static void Write(IntPtr address, IConvertible value)
        {
            var type = typeof(string);
            var bytes = value.GetBytes();
            if (type == typeof(string))
            {
                foreach (var b in bytes)
                {
                    Write(address, b);
                    address += 0x1;
                }
                Write(address, 0);
            }
            else
            {
                IntPtr written;
                WriteProcessMemory(hProc, address, bytes, bytes.Length, out written);
            }
        }

        public static uint RemoveProtection(IntPtr address, UIntPtr size)
        {
            uint protection = 0;
            VirtualProtectEx(hProc, address, size, 0x40, out protection);
            return protection;
        }

        public static void AddProtection(IntPtr address, UIntPtr size, uint protection)
        {
            VirtualProtectEx(hProc, address, size, protection, out protection);
        }
    }
}
