using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFComment : IPDFToken
    {
        public PDFTokenType TokenType { get { return PDFTokenType.Comment; } }
        public string Value { get; set; }
    }
}
