using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public struct PDFObjRef : IPDFElement, IComparable<PDFObjRef>, IEquatable<PDFObjRef>, IPDFObjRef
    {
        public PDFTokenType TokenType { get { return PDFTokenType.ObjectRef; } }
        public int ID { get; set; }
        public int Version { get; set; }

        PDFObjRef IPDFObjRef.ObjRef { get { return this; } }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public int CompareTo(PDFObjRef other)
        {
            int comp = this.ID.CompareTo(other.ID);
            if (comp == 0)
            {
                return this.Version.CompareTo(other.Version);
            }
            else
            {
                return comp;
            }
        }

        public bool Equals(PDFObjRef other)
        {
            return this.ID == other.ID && this.Version == other.Version;
        }

        public override bool Equals(object obj)
        {
            if (obj is PDFObjRef)
            {
                return this.Equals((PDFObjRef)obj);
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            return String.Format("{0} {1} R", ID, Version);
        }

        public static PDFObjRef Parse(Stack<IPDFToken> tokens)
        {
            PDFObjRef objref = new PDFObjRef();
            objref.Version = (int)tokens.Pop<PDFInteger>().Value;
            objref.ID = (int)tokens.Pop<PDFInteger>().Value;
            return objref;
        }
    }
}
