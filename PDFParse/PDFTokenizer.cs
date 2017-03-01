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

        public PDFTokenizer(ByteStreamReader reader)
        {
            this.reader = reader;
        }

        protected IPDFToken ReadComment()
        {
            string str = ISO88591.GetString(reader.ReadUntilAny("\r\n", true));

            if (str.StartsWith("PDF-1.") && str.Length == 7 && str[6] >= '0' && str[6] <= '7')
            {
                return new PDFVersion { Minor = str[6] - '0' };
            }
            else if (str == "%EOF")
            {
                return new PDFToken(PDFTokenType.EOF);
            }
            else
            {
                return new PDFComment { Value = str };
            }
        }

        protected IPDFToken ReadName()
        {
            StringBuilder sb = new StringBuilder();
            int len = reader.FindAny(" \t\r\f\n%/[]<>()", false);

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
            StringBuilder sb = new StringBuilder();
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

                        sb.Append((char)octal);
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
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            default: sb.Append((char)c); break;
                        }
                    }
                }
                else if (c == '(')
                {
                    parenlevel++;
                    sb.Append('(');
                }
                else if (c == ')')
                {
                    parenlevel--;
                    if (parenlevel >= 1)
                    {
                        sb.Append(')');
                    }
                }
                else
                {
                    sb.Append((char)c);
                }
            }

            return new PDFString { Value = sb.ToString() };
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
            byte[] data = reader.ReadUntil("endstream", " \r\n\t\f");
            reader.Read("endstream".Length);
            return new PDFStream { Data = data };
        }

        protected IPDFToken ReadKeyword()
        {
            string keyword = ISO88591.GetString(reader.ReadUntilAny(" \t\r\f\n%/[]<>()"));

            switch (keyword)
            {
                case "true": return new PDFBoolean { Value = true };
                case "false": return new PDFBoolean { Value = false };
                case "null": return new PDFNull();
                case "obj": return new PDFToken(PDFTokenType.StartObject);
                case "endobj": return new PDFToken(PDFTokenType.EndObject);
                case "xref": return new PDFToken(PDFTokenType.Xref);
                case "startxref": return new PDFToken(PDFTokenType.StartXref);
                case "trailer": return new PDFToken(PDFTokenType.Trailer);
                case "n": return new PDFToken(PDFTokenType.XrefEntryInUse);
                case "f": return new PDFToken(PDFTokenType.XrefEntryFree);
                case "R": return new PDFToken(PDFTokenType.ObjectRef);
                case "stream": return ReadStream();
                default: throw new InvalidDataException(String.Format("Unknown keyword '{0}'", keyword));
            }
        }

        public IPDFToken Read()
        {
            while (!reader.EOF)
            {
                reader.ReadWhileAny("\r\n\t\f ", true);

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
                else if (c == '-' || (c >= '0' && c <= '9'))
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
