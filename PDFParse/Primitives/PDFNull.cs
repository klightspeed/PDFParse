using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public struct PDFNull : IPDFElement
    {
        public PDFTokenType TokenType { get { return PDFTokenType.Null; } }
    }
}
