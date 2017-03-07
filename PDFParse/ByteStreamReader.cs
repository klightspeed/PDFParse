using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PDFParse
{
    public class ByteStreamReader
    {
        public byte[] Data;
        public int Offset;

        public ByteStreamReader(byte[] data)
        {
            this.Data = data;
            this.Offset = 0;
        }

        public byte[] Read(int len)
        {
            byte[] outdata = Enumerable.Range(Offset, len).Select(i => Data[i]).ToArray();
            Offset += len;
            return outdata;
        }

        public int FindAny(byte[] find, bool permiteof = false)
        {
            for (int pos = Offset; pos < Data.Length; pos++)
            {
                if (find.Contains(Data[pos]))
                {
                    return pos - Offset;
                }
            }

            if (permiteof)
            {
                return Data.Length - Offset;
            }
            else
            {
                throw new EndOfStreamException();
            }
        }

        public int FindAny(string find, bool permiteof = false)
        {
            return FindAny(ISO88591.GetBytes(find), permiteof);
        }

        public int Find(byte[] find, byte[] final = null, bool permiteof = false)
        {
            for (int pos = Offset; pos < Data.Length - find.Length; pos++)
            {
                if (find.Select((v, i) => Data[pos + i] == v).All(v => v) && ((find.Length + pos == Data.Length) || final == null || final.Contains(Data[pos + find.Length])))
                {
                    if (permiteof || find.Length + pos < Data.Length)
                    {
                        return pos - Offset;
                    }
                }
            }

            throw new EndOfStreamException();
        }

        public int Find(string find, string final = null, bool permiteof = false)
        {
            return Find(ISO88591.GetBytes(find), final == null ? null : ISO88591.GetBytes(final), permiteof);
        }

        public int Skip(byte[] skip, bool permiteof = false)
        {
            for (int pos = Offset; pos < Data.Length; pos++)
            {
                if (!skip.Contains(Data[pos]))
                {
                    return pos - Offset;
                }
            }

            if (permiteof)
            {
                return Data.Length - Offset;
            }
            else
            {
                throw new EndOfStreamException();
            }
        }

        public int Skip(string skip, bool permiteof = false)
        {
            return Skip(ISO88591.GetBytes(skip), permiteof);
        }

        public int FromHexDigit(byte data)
        {
            if (data >= '0' && data <= '9')
            {
                return data - '0';
            }
            else if (data >= 'A' && data <= 'F')
            {
                return data - 'A' + 10;
            }
            else if (data >= 'a' && data <= 'f')
            {
                return data - 'a' + 10;
            }
            else
            {
                return 0;
            }
        }

        public byte FromHex(byte[] data)
        {
            return (byte)((FromHexDigit(data[0]) << 4) | FromHexDigit(data.Length > 1 ? data[1] : (byte)0));
        }

        public byte[] ReadHex(int len)
        {
            return Enumerable.Range(0, len / 2).Select(i => FromHex(Read(i * 2 < len - 1 ? 2 : 1))).ToArray();
        }

        public byte[] ReadUntilAny(string find, bool permiteof = false)
        {
            return Read(FindAny(find, permiteof));
        }

        public byte[] ReadUntil(string find, string final, bool permiteof = false)
        {
            return Read(Find(find, final, permiteof));
        }

        public byte[] ReadWhileAny(string skip, bool permiteof = false)
        {
            return Read(Skip(skip, permiteof));
        }

        public byte[] ReadHexString()
        {
            return ReadHex(Skip("0123456789abcdefABCDEF"));
        }

        public byte Read()
        {
            return Data[Offset++];
        }

        public byte Peek
        {
            get
            {
                return Data[Offset];
            }
        }

        public bool EOF
        {
            get
            {
                return Offset >= Data.Length;
            }
        }

        public char[] CData
        {
            get
            {
                return Data.Select(c => (char)c).ToArray();
            }
        }
    }
}
