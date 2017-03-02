using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PDFParse
{
    public static class StackExtensions
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

        public static bool TryPop<T>(this Stack<IPDFToken> tokens, out T val, Func<T, bool> predicate)
        {
            if (tokens.Count == 0)
            {
                val = default(T);
                return false;
            }

            IPDFToken token = tokens.Peek();

            if (token is T && predicate((T)token))
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

        public static bool TryPop<T>(this Stack<IPDFToken> tokens, out T val)
            where T : IPDFToken
        {
            return TryPop<T>(tokens, out val, v => true);
        }

        public static bool TryPop<T>(this Stack<IPDFToken> tokens, PDFTokenType expected, out T val)
            where T : IPDFToken
        {
            return TryPop<T>(tokens, out val, v => v.TokenType == expected);
        }

        public static bool TryPopWhile<T>(this Stack<IPDFToken> tokens, out T val, params PDFTokenType[] expected)
            where T : IPDFToken
        {
            return TryPop<T>(tokens, out val, v => expected.Contains(v.TokenType));
        }

        public static bool TryPopUntil<T>(this Stack<IPDFToken> tokens, out T val, params PDFTokenType[] expected)
            where T : IPDFToken
        {
            return TryPop<T>(tokens, out val, v => !expected.Contains(v.TokenType));
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
