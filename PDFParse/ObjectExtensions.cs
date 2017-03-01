using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PDFParse
{
    public static class ObjectExtensions
    {
        public static T Pop<T>(this Stack<IPDFToken> tokens)
            where T : IPDFToken
        {
            IPDFToken token = tokens.Pop();
            if (!(token is T))
            {
                throw new InvalidDataException();
            }
            return (T)token;
        }

        public static bool TryPop<T>(this Stack<IPDFToken> tokens, out T val)
            where T : IPDFToken
        {
            IPDFToken token = tokens.Peek();

            if (token is T)
            {
                val = (T)tokens.Pop();
                return true;
            }
            else
            {
                val = default(T);
                return false;
            }
        }

        public static T TryPop<T>(this Stack<IPDFToken> tokens)
            where T : IPDFToken
        {
            T val;
            TryPop<T>(tokens, out val);
            return val;
        }

        public static IPDFToken Pop(this Stack<IPDFToken> tokens, PDFTokenType expected)
        {
            IPDFToken token = tokens.Pop();

            if (token.TokenType != expected)
            {
                throw new InvalidDataException();
            }

            return token;
        }

        public static bool Has(this Stack<IPDFToken> tokens, PDFTokenType expected)
        {
            return tokens.Peek().TokenType == expected;
        }
    }
}
