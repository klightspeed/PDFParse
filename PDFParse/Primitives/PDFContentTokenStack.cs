using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFContentTokenStack : PDFTokenStack
    {
        protected override IPDFToken ProcessKeyword(PDFKeyword token)
        {
            switch (token.Name)
            {
                case "W": return ProcessOperator("W", "re");
                case "EMC": return ProcessBlock("EMC", "BMC", "BDC");
                //case "ET": return ProcessBlock("ET", "BT");
                default: return ProcessOperator(token.Name);
            }
        }

        protected PDFContentOperator ProcessOperator(string name)
        {
            Stack<IPDFToken> tokens = new Stack<IPDFToken>();

            IPDFToken token;
            while (this.TryPopUntil(out token, PDFTokenType.Keyword))
            {
                tokens.Push(token);
            }

            return new PDFContentOperator { Name = name, Arguments = tokens.ToList() };
        }

        protected PDFContentOperator ProcessOperator(string name, string pop_op)
        {
            PDFContentOperator op = ProcessOperator(name);
            PDFContentOperator prev;
            if (this.TryPop(out prev, v => v.Name == pop_op))
            {
                op.Arguments.Add(prev);
            }
            return op;
        }

        protected PDFContentBlock ProcessBlock(string name, params string[] startmarkers)
        {
            Stack<IPDFToken> args = new Stack<IPDFToken>();
            IPDFToken token;
            while (this.TryPopUntil<IPDFToken>(out token, PDFTokenType.Keyword))
            {
                args.Push(token);
            }

            Stack<PDFContentOperator> content = new Stack<PDFContentOperator>();
            PDFContentOperator ctoken;
            while (this.TryPop<PDFContentOperator>(out ctoken))
            {
                if (startmarkers.Contains(ctoken.Name))
                {
                    break;
                }

                content.Push(ctoken);
            }
            return new PDFContentBlock { StartMarker = ctoken, Arguments = args.ToList(), Content = content.ToList(), Name = name };
        }
    }
}
