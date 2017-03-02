using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public struct PDFInteger : IPDFValue<long>, IPDFValue<double>
    {
        public PDFTokenType TokenType { get { return PDFTokenType.Integer; } }
        public long Value { get; set; }

        double IPDFValue<double>.Value { get { return (double)Value; } set { Value = (long)value; } }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
