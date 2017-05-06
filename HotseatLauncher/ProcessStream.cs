using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HotseatLauncher
{
    class ProcessStream : IDisposable
    {
        #region InterOp

        [Flags]
        enum AllocationType
        {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        [Flags]
        enum MemoryProtection : uint
        {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, UInt32 nSize, out UInt32 lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        #endregion

        MemoryStream ms = new MemoryStream();

        Process proc;

        public ProcessStream(Process proc)
        {
            this.proc = proc;
        }

        public void Dispose()
        {
            ms.Dispose();
        }

        public uint Length { get { return (uint)this.ms.Length; } }

        public void Reset()
        {
            ms.Position = 0;
            ms.SetLength(0);
            relAddresses.Clear();
        }

        public void Write(byte value)
        {
            ms.WriteByte(value);
        }

        public void Write(params byte[] data)
        {
            ms.Write(data, 0, data.Length);
        }

        public void Write(byte[] data, int offset, int length)
        {
            ms.Write(data, offset, length);
        }

        public void WriteInt(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            ms.Write(bytes, 0, 4);
        }

        public void WriteNops(int count)
        {
            for (int i = 0; i < count; i++)
                ms.WriteByte(0x90);
        }

        struct ByteDist
        {
            public string Name;
            public int Position;
            public ByteDist(string name, int position)
            {
                this.Name = name;
                this.Position = position;
            }
        }

        List<ByteDist> byteDists = new List<ByteDist>();

        public void WriteByteDistance(string name)
        {
            byteDists.Add(new ByteDist(name, (int)ms.Position));
            ms.SetLength(ms.Length + 1);
            ms.Position += 1;
        }

        public void AddByteDistance(string name)
        {
            foreach (ByteDist bd in byteDists)
            {
                if (!string.Equals(bd.Name, name, StringComparison.OrdinalIgnoreCase))
                    continue;

                byte diff = (byte)(ms.Position - bd.Position - 1);
                ms.Position = bd.Position;
                ms.WriteByte(diff);
                ms.Position = ms.Length;
            }
        }

        struct RelativeAddress
        {
            public int TargetAddress;
            public int Position;
            public RelativeAddress(int targetAddress, int position)
            {
                this.TargetAddress = targetAddress;
                this.Position = position;
            }
        }

        List<RelativeAddress> relAddresses = new List<RelativeAddress>();

        public void WriteRelativeAddress(int targetAddress)
        {
            relAddresses.Add(new RelativeAddress(targetAddress, (int)ms.Position));

            ms.SetLength(ms.Length + 4);
            ms.Position += 4;
        }

        public void Inject(int address)
        {
            if (address == 0)
                throw new ArgumentOutOfRangeException("Address is zero!");

            foreach (RelativeAddress relAddr in relAddresses)
            {
                ms.Position = relAddr.Position;
                this.WriteInt(relAddr.TargetAddress - (address + relAddr.Position + 4));
            }
            ms.Position = ms.Length;

            uint written;
            if (!WriteProcessMemory(proc.Handle, new IntPtr(address), ms.ToArray(), this.Length, out written))
                throw new Exception("Process Write failed, Win32-Error: " + Marshal.GetLastWin32Error());

        }

        public int AllocInject()
        {
            IntPtr memAddr = VirtualAllocEx(proc.Handle, IntPtr.Zero, this.Length, AllocationType.Reserve | AllocationType.Commit, MemoryProtection.ReadWrite);
            if (memAddr == IntPtr.Zero)
                throw new Exception("Process Allocation failed, Win32-Error: " + Marshal.GetLastWin32Error());

            Inject(memAddr.ToInt32());

            return memAddr.ToInt32();
        }

        public int AllocString(string str, bool uni = false, int pointer = 0)
        {
            // pointer -> pointer -> null-terminated string

            // encode string to bytes
            Encoding enc = uni ? Encoding.Unicode : Encoding.ASCII;
            byte[] data = enc.GetBytes(str + '\0');

            // calculate space to allocate (pointer + pointer + string)
            int len = data.Length + 4 * pointer;
            if (uni)
                len += 6; //  (unicode adds 6 bytes for ref, len, alloced in front of the string)

            // allocate space
            IntPtr memAddr = VirtualAllocEx(proc.Handle, IntPtr.Zero, (uint)len, AllocationType.Reserve | AllocationType.Commit, MemoryProtection.ReadWrite);
            if (memAddr == IntPtr.Zero)
                throw new Exception("Process Allocation failed, Win32-Error: " + Marshal.GetLastWin32Error());

            // write it all together
            using (MemoryStream ms = new MemoryStream(len))
            {
                for (int i = 1; i <= pointer; i++)
                {
                    ms.Write(BitConverter.GetBytes(memAddr.ToInt32() + 4 * i), 0, 4); // pointer
                }

                if (uni)
                {
                    ms.Write(BitConverter.GetBytes((ushort)0), 0, 2); // ref
                    ms.Write(BitConverter.GetBytes((ushort)data.Length), 0, 2); // len
                    ms.Write(BitConverter.GetBytes((ushort)data.Length), 0, 2); // alloced
                }

                ms.Write(data, 0, data.Length); // string

                // Inject
                uint written;
                if (!WriteProcessMemory(proc.Handle, memAddr, ms.ToArray(), (uint)ms.Length, out written))
                {
                    ms.Dispose();
                    throw new Exception("Process Write failed, Win32-Error: " + Marshal.GetLastWin32Error());
                }

            }

            return memAddr.ToInt32();
        }
    }
}
