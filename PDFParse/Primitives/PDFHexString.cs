using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public struct PDFHexString : IPDFValue<byte[]>
    {
        public PDFTokenType TokenType { get { return PDFTokenType.HexString; } }
        public byte[] Value { get; set; }
    }
}
