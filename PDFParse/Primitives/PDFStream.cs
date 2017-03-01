using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFStream : IPDFToken
    {
        public PDFTokenType TokenType { get { return PDFTokenType.Stream; } }
        public byte[] Data { get; set; }
    }
}
