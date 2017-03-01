using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFVersion : IPDFToken
    {
        public PDFTokenType TokenType { get { return PDFTokenType.Version; } }
        public int Major { get { return 1; } }
        public int Minor { get; set; }
    }
}
