using PDFParse.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PDFParse
{
    public class PDFDocumentBase
    {
        public PDFTrailer Trailer;
        public PDFVersion Version;
        public SortedDictionary<PDFObjRef, PDFObject> Objects = new SortedDictionary<PDFObjRef, PDFObject>();

        public void Load(IEnumerable<IPDFToken> tokens)
        {
            Stack<IPDFToken> stack = new Stack<IPDFToken>();

            foreach (IPDFToken token in tokens)
            {
                switch (token.TokenType)
                {
                    case PDFTokenType.Comment:
                        break;
                    case PDFTokenType.Version:
                        if (stack.Count == 0 && this.Version == null)
                        {
                            this.Version = token as PDFVersion;
                        }
                        break;
                    case PDFTokenType.EndDictionary:
                        stack.Push(PDFDictionary.Parse(stack));
                        break;
                    case PDFTokenType.EndList:
                        stack.Push(PDFList.Parse(stack));
                        break;
                    case PDFTokenType.EndObject:
                        PDFObject obj = PDFObject.Parse(stack);
                        Objects[obj.ToObjRef()] = obj;
                        stack.Push(obj);
                        break;
                    case PDFTokenType.ObjectRef:
                        stack.Push(PDFObjRef.Parse(stack));
                        break;
                    case PDFTokenType.XrefEntryFree:
                    case PDFTokenType.XrefEntryInUse:
                        stack.Push(PDFXrefEntry.Parse(stack, token.TokenType));
                        break;
                    case PDFTokenType.Trailer:
                        stack.Push(PDFXref.Parse(stack));
                        break;
                    case PDFTokenType.EOF:
                        this.Trailer = PDFTrailer.Parse(stack);
                        break;
                    default:
                        stack.Push(token);
                        break;
                }
            }
        }

        public void Load(ByteStreamReader reader)
        {
            Load(new PDFTokenizer(reader));
        }
    }
}
