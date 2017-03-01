using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public struct PDFDouble : IPDFValue<double>
    {
        public PDFTokenType TokenType { get { return PDFTokenType.Double; } }
        public double Value { get; set; }
    }
}
