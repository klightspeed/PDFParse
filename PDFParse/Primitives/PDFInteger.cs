using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public struct PDFInteger : IPDFValue<long>
    {
        public PDFTokenType TokenType { get { return PDFTokenType.Integer; } }
        public long Value { get; set; }
    }
}
