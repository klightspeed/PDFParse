using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse
{
    public interface IPDFValue<T> : IPDFElement
    {
        T Value { get; set; }
    }

    public interface IPDFElement : IPDFToken
    {
    }

    public interface IPDFParsableToken
    {
        IPDFToken Parse(Stack<IPDFToken> stack);
    }

    public interface IPDFToken
    {
        PDFTokenType TokenType { get; }
    }
}
