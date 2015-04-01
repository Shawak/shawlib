using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ShawLib
{
    public class Memory : IDisposable
    {
        IntPtr hProc;
        bool disposed;

        public Memory(Process proc)
            : this(proc.Id)
        {
        }

        public Memory(int procId)
        {
            hProc = NativeMethods.OpenProcess(ProcessAccessFlags.All, false, procId);
        }

        public T Read<T>(IntPtr address) where T : IConvertible
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
                NativeMethods.ReadProcessMemory(hProc, address, buffer, (IntPtr)size, out read);
                return buffer.To<T>();
            }
        }

        public void Write(IntPtr address, IConvertible value)
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
                NativeMethods.WriteProcessMemory(hProc, address, bytes, (IntPtr)bytes.Length, out written);
            }
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
