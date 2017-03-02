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

        public string ToString()
        {
            switch (this.TokenType)
            {
                case PDFTokenType.EndDictionary: return ">>";
                case PDFTokenType.EndList: return "]";
                case PDFTokenType.EndObject: return "endobj";
                case PDFTokenType.StartDictionary: return "<<";
                case PDFTokenType.StartList: return "[";
                case PDFTokenType.StartObject: return "obj";
                case PDFTokenType.StartXref: return "startxref";
                case PDFTokenType.Trailer: return "trailer";
                case PDFTokenType.Xref: return "xref";
                case PDFTokenType.XrefEntryFree: return "f";
                case PDFTokenType.XrefEntryInUse: return "n";
                default: return "?";
            }
        }
    }
}
