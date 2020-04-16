using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleChat
{
    public enum UdpTypes : byte
    {   
        Disconnected = 0,
        Connected = 1
    };
    public class UdpMessage
    {
        private byte Type = 2;
        private byte NameLength = 0;
        private byte[] Name = { };

 
        public UdpMessage(byte type, string name)
        {
            if ((type == 0 || type == 1))
            {
                Name = Encoding.Unicode.GetBytes(name);
                if (Name.Length < 256)
                {
                    NameLength = (byte)Name.Length;
                    Type = type;
                }
            }
        }

        public UdpMessage(byte[] bytes)
        {
            if ((bytes[0] == 0 || bytes[0] == 1) && bytes[1] == bytes.Length - 2)
            {
                Array.Resize(ref Name, bytes.Length - 2);
                Array.Copy(bytes, 2, Name, 0, bytes.Length - 2);
                if (Name.Length < 256)
                {
                    NameLength = (byte)Name.Length;
                    Type = bytes[0];
                }
            }
        }

        public bool CheckMessage(ref byte type)
        {
            type = Type;
            if (Enum.IsDefined(typeof(UdpTypes), Type) && Name.Length == NameLength)
            {
                return true;
            }
            return false;
        }

        public string GetName()
        {
            return Encoding.Unicode.GetString(Name);
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[NameLength + 2];
            bytes[0] = Type; bytes[1] = NameLength;
            Array.Copy(Name, 0, bytes, 2, NameLength);
            return bytes;
        }
    }
}
