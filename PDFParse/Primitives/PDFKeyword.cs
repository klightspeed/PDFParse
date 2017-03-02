using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFKeyword : IPDFToken
    {
        public PDFTokenType TokenType { get { return PDFTokenType.Keyword; } }
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
