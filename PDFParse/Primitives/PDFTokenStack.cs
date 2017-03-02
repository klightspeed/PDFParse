using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFTokenStack : Stack<IPDFToken>
    {
        protected virtual IPDFToken ProcessKeyword(PDFKeyword token)
        {
            switch (token.Name)
            {
                case "true": return new PDFBoolean { Value = true };
                case "false": return new PDFBoolean { Value = false };
                case "null": return new PDFNull();
                case "obj": return new PDFToken(PDFTokenType.StartObject);
                case "xref": return new PDFToken(PDFTokenType.Xref);
                case "startxref": return new PDFToken(PDFTokenType.StartXref);
                case "endobj": return PDFObject.Parse(this);
                case "trailer": return PDFTrailer.Parse(this);
                case "n": return PDFXrefEntry.Parse(this, PDFTokenType.XrefEntryInUse);
                case "f": return PDFXrefEntry.Parse(this, PDFTokenType.XrefEntryFree);
                case "R": return PDFObjRef.Parse(this);
                default: throw new InvalidDataException(String.Format("Unknown keyword '{0}'", token.Name));
            }
        }

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
                case PDFTokenType.EOF:
                    this.Push(PDFTrailer.Parse(this));
                    break;
                case PDFTokenType.Keyword:
                    this.Push(ProcessKeyword((PDFKeyword)token));
                    break;
                default:
                    this.Push(token);
                    break;
            }
        }
    }
}
