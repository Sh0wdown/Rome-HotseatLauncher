using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HotseatLauncher
{
    class WatcherStream : Stream
    {
        FileWatcher watcher;
        bool wasEnabled;

        Stream stream;

        public WatcherStream(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("Stream is null!");

            this.stream = stream;
            this.watcher = null;
        }

        public WatcherStream(FileWatcher fileWatcher, FileMode fileMode, FileAccess fileAccess)
        {
            if (fileWatcher == null)
                throw new ArgumentNullException("FileWatcher is null!");

            this.watcher = fileWatcher;
            this.wasEnabled = fileWatcher.Enabled;

            this.stream = fileWatcher.Info.Open(fileMode, fileAccess, FileShare.None);
            watcher.Enabled = false;
            watcher.Postpone = true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                stream.Flush();
                if (watcher != null)
                {
                    watcher.Enabled = wasEnabled;
                    watcher.Postpone = false;
                }
                stream.Dispose();
            }
        }

        public void Write(CodeVersion version)
        {
            stream.WriteByte(version.ID);
        }

        public void Read(out CodeVersion version)
        {
            int id = stream.ReadByte();
            version = CodeVersion.Versions.FirstOrDefault(v => v.ID == id);
        }

        public void Write(Installation installation)
        {
            Write(installation?.FilePath);
        }

        public void Read(out Installation installation)
        {
            string filePath;
            Read(out filePath);
            Settings.TryGetInstallation(filePath, out installation);
        }

        public void Write(bool value)
        {
            stream.WriteByte((byte)(value ? 1 : 0));
        }

        public void Read(out bool value)
        {
            value = stream.ReadByte() > 0;
        }

        public void Read(out Difficulty difficulty)
        {
            byte value;
            Read(out value);
            difficulty = value;
        }

        public void Write(byte value)
        {
            stream.WriteByte(value);
        }

        public void Read(out byte value)
        {
            int read = stream.ReadByte();
            if (read < 0)
                throw new EndOfStreamException();

            value = (byte)read;
        }

        public void Write(int value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, 4);
        }

        public void Read(out int value)
        {
            byte[] bytes = new byte[4];
            stream.Read(bytes, 0, 4);

            value = BitConverter.ToInt32(bytes, 0);
        }

        public void Write(ushort value)
        {
            stream.Write(BitConverter.GetBytes(value), 0, 2);
        }

        public void Read(out ushort value)
        {
            byte[] bytes = new byte[2];
            stream.Read(bytes, 0, 2);

            value = BitConverter.ToUInt16(bytes, 0);
        }

        public void Write(string str)
        {
            byte[] bytes = str == null ? new byte[0] : Encoding.UTF8.GetBytes(str);

            Write(bytes.Length);
            stream.Write(bytes, 0, bytes.Length);
        }

        public void Read(out string str)
        {
            int len;
            Read(out len);

            byte[] bytes = new byte[len];
            stream.Read(bytes, 0, len);
            str = Encoding.UTF8.GetString(bytes);
        }

        #region Stream class properties & methods

        public override bool CanRead { get { return stream.CanRead; } }
        public override bool CanSeek { get { return stream.CanSeek; } }
        public override bool CanWrite { get { return stream.CanWrite; } }
        public override long Length { get { return stream.Length; } }
        public override long Position { get { return stream.Position; } set { stream.Position = value; } }

        public override void WriteByte(byte value)
        {
            stream.WriteByte(value);
        }

        public override int ReadByte()
        {
            return stream.ReadByte();
        }

        public override void Flush()
        {
            stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            stream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return stream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            stream.Write(buffer, offset, count);
        }

        #endregion
    }
}
