using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFXref : IPDFToken
    {
        public PDFTokenType TokenType { get { return PDFTokenType.Xref; } }
        public List<PDFXrefEntry> Entries { get; set; }

        public static PDFXref Parse(Stack<IPDFToken> tokens)
        {
            PDFXref xref = new PDFXref { Entries = new List<PDFXrefEntry>() };

            while (tokens.Count != 0 && (tokens.Peek().TokenType == PDFTokenType.XrefEntry || tokens.Peek().TokenType == PDFTokenType.Integer))
            {
                Stack<PDFXrefEntry> entries = new Stack<PDFXrefEntry>();

                PDFXrefEntry entry = null;
                while (tokens.TryPop<PDFXrefEntry>(out entry))
                {
                    entries.Push(entry);
                }

            
                int count = (int)tokens.Pop<PDFInteger>().Value;
                int start = (int)tokens.Pop<PDFInteger>().Value;

                while (entries.Count != 0)
                {
                    PDFXrefEntry ent = entries.Pop();
                    ent.ID = start++;
                    xref.Entries.Add(ent);
                }

                IPDFToken token;
                if (tokens.TryPop(PDFTokenType.Xref, out token))
                {
                    return xref;
                }
            }

            throw new InvalidDataException();
        }
    }
}
