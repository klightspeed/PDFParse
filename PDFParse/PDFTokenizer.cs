using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PDFParse.Primitives;

namespace PDFParse
{
    public class PDFTokenizer : IEnumerable<IPDFToken>
    {
        protected ByteStreamReader reader;
        public bool UseStreamKeyword { get; set; }

        public PDFTokenizer(ByteStreamReader reader, bool useStreamKeyword = false)
        {
            this.reader = reader;
            this.UseStreamKeyword = useStreamKeyword;
        }

        protected IPDFToken ReadComment()
        {
            // Some PDFs have an object immediately following the %%EOF without an intervening line break
            if (reader.Find("%EOF") == 0)
            {
                reader.Read(4);
                return new PDFToken(PDFTokenType.EOF);
            }
            else
            {
                string str = ISO88591.GetString(reader.ReadUntilAny("\r\n", true));

                if (str.StartsWith("PDF-1.") && str.Length == 7 && str[6] >= '0' && str[6] <= '7')
                {
                    return new PDFVersion { Minor = str[6] - '0' };
                }
                else
                {
                    return new PDFComment { Value = str };
                }
            }
        }

        protected IPDFToken ReadName()
        {
            StringBuilder sb = new StringBuilder();
            int len = reader.FindAny(" \0\t\r\f\n%/[]<>()", false);

            for (int i = 0; i < len; i++)
            {
                byte c = reader.Read();

                if (c == '#')
                {
                    sb.Append((char)reader.FromHex(reader.Read(2)));
                    i += 2;
                }
                else
                {
                    sb.Append((char)c);
                }
            }

            return new PDFName { Name = sb.ToString() };
        }

        protected IPDFToken ReadNumber()
        {
            bool isdouble = false;
            List<byte> data = new List<byte>();

            if (reader.Peek == '-')
            {
                data.Add(reader.Read());
            }
            else if (reader.Peek == '+')
            {
                reader.Read();
            }

            data.AddRange(reader.ReadWhileAny("0123456789"));

            if (reader.Peek == '.')
            {
                data.Add(reader.Read());
                isdouble = true;
            }

            data.AddRange(reader.ReadWhileAny("0123456789"));

            string numstr = ISO88591.GetString(data.ToArray());

            if (isdouble)
            {
                return new PDFDouble { Value = Convert.ToDouble(numstr.Trim('0') + "0") };
            }
            else
            {
                return new PDFInteger { Value = Convert.ToInt64(numstr, 10) };
            }
        }

        protected IPDFToken ReadStringLiteral()
        {
            List<byte> bytes = new List<byte>();
            int parenlevel = 1;

            while (parenlevel >= 1)
            {
                byte c = reader.Read();

                if (c == '\\')
                {
                    c = reader.Read();
                    if (c >= '0' && c <= '3')
                    {
                        int octal = c - '0';
                        if (reader.Peek >= '0' && reader.Peek <= '7')
                        {
                            octal = octal * 8 + (reader.Read() - '0');
                            if (reader.Peek >= '0' && reader.Peek <= '7')
                            {
                                octal = octal * 8 + (reader.Read() - '0');
                            }
                        }

                        bytes.Add((byte)octal);
                    }
                    else if (c == '\r')
                    {
                        if (reader.Peek == '\n')
                        {
                            reader.Read();
                        }
                    }
                    else if (c != '\n')
                    {
                        switch ((char)c)
                        {
                            case 'n': bytes.Add((byte)'\n'); break;
                            case 'r': bytes.Add((byte)'\r'); break;
                            case 't': bytes.Add((byte)'\t'); break;
                            case 'b': bytes.Add((byte)'\b'); break;
                            case 'f': bytes.Add((byte)'\f'); break;
                            default: bytes.Add(c); break;
                        }
                    }
                }
                else if (c == '(')
                {
                    parenlevel++;
                    bytes.Add((byte)'(');
                }
                else if (c == ')')
                {
                    parenlevel--;
                    if (parenlevel >= 1)
                    {
                        bytes.Add((byte)')');
                    }
                }
                else
                {
                    bytes.Add(c);
                }
            }

            if (bytes.Count >= 2 && (bytes.Count % 2) == 0 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                char[] chars = new char[bytes.Count / 2 - 1];

                for (int i = 0; i < chars.Length; i++)
                {
                    chars[i] = (char)(((int)bytes[i * 2 + 2] << 8) | (int)bytes[i * 2 + 3]);
                }

                return new PDFString { Value = new String(chars) };
            }
            else if (bytes.Count >= 2 && (bytes.Count % 2) == 0 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                char[] chars = new char[bytes.Count / 2 - 1];

                for (int i = 0; i < chars.Length; i++)
                {
                    chars[i] = (char)(((int)bytes[i * 2 + 3] << 8) | (int)bytes[i * 2 + 2]);
                }

                return new PDFString { Value = new String(chars) };
            }
            else
            {
                return new PDFString { Value = ISO88591.GetString(bytes.ToArray()) };
            }
        }

        protected IPDFToken ReadHexString()
        {
            byte[] data = reader.ReadHexString();

            if (reader.Read() != '>')
            {
                throw new InvalidDataException();
            }

            return new PDFHexString { Value = data };
        }

        protected IPDFToken ReadStream()
        {
            if (reader.Peek == '\r') reader.Read();
            if (reader.Peek == '\n') reader.Read();
            byte[] data = reader.ReadUntil("endstream", " \0\r\n\t\f");
            reader.Read("endstream".Length);
            return new PDFStream { Data = data };
        }

        protected IPDFToken ReadKeyword()
        {
            string keyword = ISO88591.GetString(reader.ReadUntilAny(" \t\r\f\n%/[]<>()"));

            if (keyword == "stream" && UseStreamKeyword)
            {
                return ReadStream();
            }

            return new PDFKeyword { Name = keyword };
        }

        public IPDFToken Read()
        {
            while (!reader.EOF)
            {
                reader.ReadWhileAny("\0\r\n\t\f ", true);

                if (reader.EOF)
                {
                    return null;
                }

                char c = (char)reader.Peek;

                
                if (c == '%')
                {
                    reader.Read();
                    IPDFToken comment = ReadComment();
                    if (!(comment is PDFComment))
                    {
                        return comment;
                    }
                }
                else if (c == '-' || c == '+' || (c >= '0' && c <= '9'))
                {
                    return ReadNumber();
                }
                else if (c == '/')
                {
                    reader.Read();
                    return ReadName();
                }
                else if (c == '[')
                {
                    reader.Read();
                    return new PDFToken(PDFTokenType.StartList);
                }
                else if (c == ']')
                {
                    reader.Read();
                    return new PDFToken(PDFTokenType.EndList);
                }
                else if (c == '(')
                {
                    reader.Read();
                    return ReadStringLiteral();
                }
                else if (c == '<')
                {
                    reader.Read();
                    if (reader.Peek == '<')
                    {
                        reader.Read();
                        return new PDFToken(PDFTokenType.StartDictionary);
                    }
                    else
                    {
                        return ReadHexString();
                    }
                }
                else if (c == '>')
                {
                    reader.Read();
                    if (reader.Read() == '>')
                    {
                        return new PDFToken(PDFTokenType.EndDictionary);
                    }
                    else
                    {
                        throw new InvalidDataException("Unexpected '>'");
                    }
                }
                else if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    return ReadKeyword();
                }
                else
                {
                    throw new InvalidDataException();
                }
            }

            return null;
        }

        public bool Read(out IPDFToken token)
        {
            token = Read();
            return token != null;
        }

        public IEnumerator<IPDFToken> GetEnumerator()
        {
            IPDFToken token;
            while (Read(out token))
            {
                yield return token;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
