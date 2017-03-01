using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFToken : IPDFToken
    {
        public PDFTokenType TokenType { get; protected set; }

        public PDFToken(PDFTokenType tokenType)
        {
            this.TokenType = tokenType;
        }
    }
}
