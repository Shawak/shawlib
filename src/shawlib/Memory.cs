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
            byte[] buffer;
            var type = typeof(T);
            if (type == typeof(string))
            {
                var list = new List<byte>();
                do
                {
                    var b = Read<byte>(address);
                    address += 0x1;
                }
                while (list[list.Count] != 0);
                buffer = list.ToArray();
            }
            else
            {
                var size = typeof(T).MemSize();
                buffer = new byte[size];
                IntPtr read;
                NativeMethods.ReadProcessMemory(hProc, address, buffer, size, out read);
                if ((int)read != size)
                    throw new Exception("could not write value, maybe it's protected?");
            }
            return buffer.To<T>();
        }

        public void Write(IntPtr address, IConvertible value)
        {
            var type = value.GetType();
            if (type == typeof(string))
                value += "\0";

            var bytes = value.GetBytes();
            IntPtr written;
            NativeMethods.WriteProcessMemory(hProc, address, bytes, bytes.Length, out written);
            if ((int)written != value.MemSize())
                throw new Exception("could not write value, maybe it's protected?");
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
