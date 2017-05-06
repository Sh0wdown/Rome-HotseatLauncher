using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace HotseatLauncher
{
    static class Extensions
    {
        public static bool CompareTo(this byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
                return false;

            for (int i = 0; i < array1.Length; i++)
                if (array1[i] != array2[i])
                    return false;

            return true;
        }

        public static IEnumerable<T> ToEnumerable<T>(this T element)
        {
            yield return element;
        }

        #region IO

        public static string ReadRomeLine(this StreamReader sr)
        {
            string line = sr.ReadLine();
            if (line != null)
            {
                // remove comments
                int commentIndex = line.IndexOf(';');
                if (commentIndex >= 0)
                    line = line.Remove(commentIndex);

                line = line.Trim();
            }
            return line;
        }

        public static bool EOS(this BinaryReader reader)
        {
            return reader.BaseStream.Position >= reader.BaseStream.Length;
        }

        public static bool IsLocked(this FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        #endregion

        #region WPF Controls
        
        public static void AddItem<T>(this Selector selector, T element)
        {
            selector.Items.Add(element);
        }

        public static void AddItems<T>(this Selector selector, IEnumerable<T> elements)
        {
            ItemCollection items = selector.Items;
            foreach (T item in elements)
                items.Add(item);
        }

        public static void RemoveItem<T>(this Selector selector, T element)
        {
            selector.Items.Remove(element);
        }

        public static void RemoveItems<T>(this Selector selector, IEnumerable<T> elements)
        {
            ItemCollection items = selector.Items;
            foreach (T item in elements)
                items.Remove(item);
        }

        public static void RemoveAll<T>(this Selector selector, Predicate<T> predicate)
        {
            ItemCollection items = selector.Items;
            List<T> list = new List<T>(items.Cast<T>().Where(o => predicate(o)));
            list.ForEach(o => items.Remove(o));
        }

        public static void SortItems<T>(this Selector selector, Func<T, object> KeySelector)
        {
            object selected = selector.SelectedItem;

            ItemCollection items = selector.Items;
            List<T> list = new List<T>(items.Cast<T>());
            items.Clear();
            foreach (T item in list.OrderBy(KeySelector))
                items.Add(item);

            selector.SelectedItem = selected;
        }
        
        public static void AddItemSorted<T>(this Selector selector, T element, Func<T, object> SortKey)
        {
            selector.Items.Add(element);
            selector.SortItems(SortKey);
        }

        public static void AddItemsSorted<T>(this Selector selector, IEnumerable<T> elements, Func<T, object> SortKey)
        {
            ItemCollection items = selector.Items;
            foreach (T item in elements)
                items.Add(item);
            selector.SortItems(SortKey);
        }

        #endregion

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

        #endregion

        public static void Write(this Process process, int address, params byte[] data)
        {
            uint written;
            if (!WriteProcessMemory(process.Handle, new IntPtr(address), data, (uint)data.Length, out written))
                throw new Exception("Process Write failed, Win32-Error: " + Marshal.GetLastWin32Error());
        }

        public static void WriteInt(this Process process, int address, int value)
        {
            process.Write(address, BitConverter.GetBytes(value));
        }
    }
}
