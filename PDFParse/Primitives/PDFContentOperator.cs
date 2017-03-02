using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFContentOperator : PDFKeyword
    {
        public List<IPDFToken> Arguments { get; set; }

        public override string ToString()
        {
            if (Name == "TJ")
            {
                return "(" + Text + ") TJ";
            }
            else
            {
                return String.Join(" ", Arguments.Select(a => a.ToString())) + " " + Name;
            }
        }

        public virtual string Text 
        { 
            get 
            { 
                if (Name == "TJ")
                {
                    return String.Join("", Arguments.OfType<PDFList>().SelectMany(a => a.OfType<PDFString>().Select(v => v.Value)));
                }
                else if (Name == "Tj")
                {
                    return String.Join("", Arguments.OfType<PDFString>().Select(v => v.Value));
                }
                else
                {
                    return null;
                }
            } 
        }
    }
}
