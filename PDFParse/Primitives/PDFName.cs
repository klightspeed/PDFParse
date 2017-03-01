using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public struct PDFName : IPDFElement
    {
        public PDFTokenType TokenType { get { return PDFTokenType.Name; } }
        public string Name { get; set; }

        public override string ToString()
        {
            return "/" + Name;
        }
    }
}
