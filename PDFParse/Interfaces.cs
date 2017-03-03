using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PDFParse.Primitives;

namespace PDFParse
{
    public interface IPDFValue<T> : IPDFElement
    {
        T Value { get; set; }
    }

    public interface IPDFDictionary : IPDFElement
    {
        PDFDictionary Dict { get; }
    }

    public interface IPDFList : IPDFElement
    {
        PDFList List { get; }
    }

    public interface IPDFStream : IPDFElement
    {
        PDFStream Stream { get; }
    }

    public interface IPDFObjRef : IPDFElement
    {
        PDFObjRef ObjRef { get; }
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
