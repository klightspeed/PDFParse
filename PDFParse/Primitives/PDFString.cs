using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public struct PDFString : IPDFValue<string>
    {
        public PDFTokenType TokenType { get { return PDFTokenType.String; } }
        public string Value { get; set; }
    }
}
