using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace HotseatLauncher
{
    struct MD5Hash
    {
        byte[] hash;

        public MD5Hash(string fileName)
        {
            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            using (MD5 md5 = new MD5CryptoServiceProvider())
                this.hash = md5.ComputeHash(fs);
        }

        public MD5Hash(Stream stream)
        {
            using (MD5 md5 = new MD5CryptoServiceProvider())
                this.hash = md5.ComputeHash(stream);
        }

        public MD5Hash(byte[] hash)
        {
            if (hash.Length != 16)
                throw new ArgumentException("Hash length is not 16!");

            this.hash = new byte[16];
            Array.Copy(hash, this.hash, 16);
        }

        public static MD5Hash Read(Stream stream)
        {
            byte[] hash = new byte[16];
            stream.Read(hash, 0, 16);
            return new MD5Hash(hash);
        }

        public void Write(Stream stream)
        {
            stream.Write(hash ?? new byte[16], 0, 16);
        }

        public byte this[int index]
        {
            get { return hash == null ? (byte)0 : hash[index]; }
        }

        public static bool operator ==(MD5Hash a, MD5Hash b)
        {
            if (a.hash == b.hash)
                return true;

            if (a.hash == null || b.hash == null)
                return false;

            if (a.hash.Length != b.hash.Length)
                return false;

            for (int i = 0; i < a.hash.Length; i++)
                if (a[i] != b[i])
                    return false;

            return true;
        }

        public static bool operator !=(MD5Hash a, MD5Hash b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return obj is MD5Hash ? this == (MD5Hash)obj : false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var result = 0;
                foreach (byte b in hash)
                    result = (result * 31) ^ b;
                return result;
            }
        }
    }
}
