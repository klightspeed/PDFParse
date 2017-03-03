using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFStream : IPDFElement, IPDFParsableToken, IPDFDictionary, IPDFStream
    {
        public PDFTokenType TokenType { get { return PDFTokenType.Stream; } }
        public byte[] Data { get; set; }
        public PDFDictionary Options { get; set; }
        PDFDictionary IPDFDictionary.Dict { get { return Options; } }
        PDFStream IPDFStream.Stream { get { return this; } }

        public PDFStream() { }

        public PDFStream(PDFStream other)
        {
            this.Data = other.Data;
            this.Options = other.Options;
        }

        public IPDFToken Parse(Stack<IPDFToken> stack)
        {
            PDFDictionary dict;

            if (stack.TryPop(out dict))
            {
                this.Options = dict;
                return this.ApplyFilters();
            }
            else
            {
                return this;
            }
        }

        public PDFStream FlateDecode(PDFDictionary filterParams)
        {
            using (ZlibStream strm = new ZlibStream(new MemoryStream(Data), CompressionMode.Decompress))
            {
                byte[] outdata = new byte[1048576];
                int pos = 0;
                int len = 0;

                do
                {
                    Array.Resize(ref outdata, pos + 1048576);
                    len = strm.Read(outdata, pos, outdata.Length - pos);
                    pos += len;
                }
                while (len > 0);

                Array.Resize(ref outdata, pos);

                return new PDFStream { Data = outdata, Options = Options };
            }
        }

        public PDFStream ApplyFilter(string filter, PDFDictionary filterParams)
        {
            switch (filter)
            {
                case "FlateDecode": return FlateDecode(filterParams);
                case "DCTDecode": return new PDFImage(this, filterParams);
                default: throw new NotImplementedException();
            }
        }

        protected PDFStream ApplyFilters()
        {
            PDFStream data = this;
            if (Options != null)
            {
                PDFInteger length;
                if (Options.TryGet("Length", out length))
                {
                    data = new PDFStream { Data = new byte[length.Value], Options = Options };
                    Array.Copy(Data, data.Data, data.Data.Length);

                    PDFList filters;
                    PDFName filter;

                    if (Options.TryGet("Filter", out filter))
                    {
                        filters = new PDFList { filter };
                    }
                    else
                    {
                        Options.TryGet("Filter", out filters);
                    }

                    if (filters != null)
                    {
                        PDFDictionary decodeparams;
                        PDFList decodeparamslist;

                        if (Options.TryGet("DecodeParams", out decodeparams))
                        {
                            decodeparamslist = new PDFList { decodeparams };
                        }
                        else
                        {
                            Options.TryGet("DecodeParams", out decodeparamslist);

                            if (decodeparamslist == null)
                            {
                                decodeparamslist = new PDFList();
                            }
                        }

                        for (int i = 0; i < filters.Count; i++)
                        {
                            string filtername = filters.Get<PDFName>(i).Name;
                            PDFDictionary filterparams;
                            decodeparamslist.TryGet<PDFDictionary>(i, out filterparams);
                            data = data.ApplyFilter(filtername, filterparams);
                        }
                    }
                }
            }

            return data;
        }

        public string StreamString { get { return ISO88591.GetString(Data); } }

    }
}
