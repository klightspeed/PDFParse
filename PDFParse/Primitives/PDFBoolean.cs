using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public struct PDFBoolean : IPDFValue<bool>
    {
        public PDFTokenType TokenType { get { return PDFTokenType.Boolean; } }
        public bool Value { get; set; }

        public override string ToString()
        {
            return Value ? "true" : "false";
        }
    }
}
