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
        public Dictionary<string, List<PDFObject>> ObjectByType = new Dictionary<string, List<PDFObject>>();

        protected PDFObject GetOrCreate(PDFObjRef objref)
        {
            if (!Objects.ContainsKey(objref))
            {
                Objects[objref] = new PDFObject(objref.ID, objref.Version);
            }

            return Objects[objref];
        }

        protected PDFList ResolveReferences(PDFObject parent, PDFList list)
        {
            return new PDFList(list.Select(v => ResolveReferences(parent, v)));
        }

        protected PDFDictionary ResolveReferences(PDFObject parent, PDFDictionary dict)
        {
            return new PDFDictionary(dict.Select(kvp => new KeyValuePair<string, IPDFElement>(kvp.Key, ResolveReferences(parent, kvp.Value))));
        }

        protected PDFObject ResolveReferences(PDFObject parent, PDFObjRef objref)
        {
            return GetOrCreate(objref).AddRef(parent);
        }

        protected IPDFElement ResolveReferences(PDFObject parent, IPDFElement val)
        {
            if (val is PDFList)
            {
                return ResolveReferences(parent, (PDFList)val);
            }
            else if (val is PDFDictionary)
            {
                return ResolveReferences(parent, (PDFDictionary)val);
            }
            else if (val is PDFObjRef)
            {
                return ResolveReferences(parent, (PDFObjRef)val);
            }
            else
            {
                return val;
            }
        }

        protected void ResolveReferences(PDFObject obj)
        {
            if (obj.Value != null)
            {
                obj.Value = ResolveReferences(obj, obj.Value);
            }
        }

        protected void AddToObjectTypes(PDFObject obj)
        {
            PDFDictionary dict;
            if (obj.TryGet(out dict))
            {
                PDFName type;
                if (dict.TryGet("Type", out type))
                {
                    if (!ObjectByType.ContainsKey(type.Name))
                    {
                        ObjectByType[type.Name] = new List<PDFObject>();
                    }
                    ObjectByType[type.Name].Add(obj);
                }
            }
        }

        public void Load(IEnumerable<IPDFToken> tokens)
        {
            PDFTokenStack stack = new PDFTokenStack();

            foreach (IPDFToken token in tokens)
            {
                stack.ProcessToken(token);
            }

            foreach (IPDFToken token in stack)
            {
                if (token is PDFVersion && Version == null)
                {
                    Version = (PDFVersion)token;
                }
                else if (token is PDFTrailer)
                {
                    Trailer = (PDFTrailer)token;
                }
                else if (token is PDFObject)
                {
                    PDFObject obj = (PDFObject)token;
                    Objects[obj.ToObjRef()] = obj;

                    PDFName type;
                    if (obj.TryGet("Type", out type) && type.Name == "ObjStm" && obj.Stream != null)
                    {
                        foreach (PDFObject sobj in PDFObject.FromObjStm(obj))
                        {
                            Objects[sobj.ToObjRef()] = sobj;
                        }
                    }
                }
            }

            foreach (PDFObject obj in Objects.Values.ToArray())
            {
                AddToObjectTypes(obj);
            }

            foreach (PDFObject obj in Objects.Values.ToArray())
            {
                ResolveReferences(obj);
            }

            if (Trailer.TrailerDictionary != null)
            {
                Trailer.TrailerDictionary = ResolveReferences(null, Trailer.TrailerDictionary);
            }
        }

        public void Load(ByteStreamReader reader)
        {
            Load(new PDFTokenizer(reader, true));
        }
    }
}
