using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFTokenStack : Stack<IPDFToken>
    {
        public void ProcessToken(IPDFToken token)
        {
            switch (token.TokenType)
            {
                case PDFTokenType.Comment:
                    break;
                case PDFTokenType.EndDictionary:
                    this.Push(PDFDictionary.Parse(this));
                    break;
                case PDFTokenType.EndList:
                    this.Push(PDFList.Parse(this));
                    break;
                case PDFTokenType.EndObject:
                    this.Push(PDFObject.Parse(this));
                    break;
                case PDFTokenType.ObjectRef:
                    this.Push(PDFObjRef.Parse(this));
                    break;
                case PDFTokenType.XrefEntryFree:
                case PDFTokenType.XrefEntryInUse:
                    this.Push(PDFXrefEntry.Parse(this, token.TokenType));
                    break;
                case PDFTokenType.Trailer:
                    this.Push(PDFXref.Parse(this));
                    break;
                case PDFTokenType.EOF:
                    this.Push(PDFTrailer.Parse(this));
                    break;
                default:
                    this.Push(token);
                    break;
            }
        }
    }
}
