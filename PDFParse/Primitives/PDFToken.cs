using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFToken : IPDFToken, IPDFParsableToken
    {
        public PDFTokenType TokenType { get; protected set; }

        public PDFToken(PDFTokenType tokenType)
        {
            this.TokenType = tokenType;
        }

        public IPDFToken Parse(Stack<IPDFToken> stack)
        {
            switch (TokenType)
            {
                case PDFTokenType.EndDictionary:
                    return PDFDictionary.Parse(stack);
                case PDFTokenType.EndList:
                    return PDFList.Parse(stack);
                case PDFTokenType.EOF:
                    return PDFTrailer.Parse(stack);
                default:
                    return this;
            }
        }

        public override string ToString()
        {
            switch (this.TokenType)
            {
                case PDFTokenType.EndDictionary: return ">>";
                case PDFTokenType.EndList: return "]";
                case PDFTokenType.StartDictionary: return "<<";
                case PDFTokenType.StartList: return "[";
                case PDFTokenType.StartObject: return "obj";
                case PDFTokenType.StartXref: return "startxref";
                case PDFTokenType.Xref: return "xref";
                default: return "?";
            }
        }
    }
}
