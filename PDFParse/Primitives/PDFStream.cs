using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFStream : IPDFToken
    {
        public PDFTokenType TokenType { get { return PDFTokenType.Stream; } }
        public byte[] Data { get; set; }

        public PDFStream FlateDecode(PDFDictionary filterParams, PDFDictionary streamParams)
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

                return new PDFStream { Data = outdata };
            }
        }

        public PDFStream ApplyFilter(string filter, PDFDictionary filterParams, PDFDictionary streamParams)
        {
            switch (filter)
            {
                case "FlateDecode": return FlateDecode(filterParams, streamParams);
                case "DCTDecode": return new PDFImage(this, filterParams, streamParams);
                default: throw new NotImplementedException();
            }
        }
    }
}
