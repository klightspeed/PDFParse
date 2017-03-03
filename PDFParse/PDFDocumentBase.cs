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

        protected IPDFList ResolveReferences(PDFObject parent, IPDFList list)
        {
            PDFList _list = list.List;
            for (int i = 0; i < _list.Count; i++)
            {
                _list[i] = ResolveReferences(parent, _list[i]);
            }
            return list;
        }

        protected IPDFDictionary ResolveReferences(PDFObject parent, IPDFDictionary dict)
        {
            PDFDictionary _dict = dict.Dict;
            foreach (string key in _dict.Keys.ToArray())
            {
                _dict[key] = ResolveReferences(parent, _dict[key]);
            }
            return dict;
        }

        protected PDFObject ResolveReferences(PDFObject parent, PDFObjRef objref)
        {
            return GetOrCreate(objref).AddRef(parent);
        }

        protected IPDFElement ResolveReferences(PDFObject parent, IPDFElement val)
        {
            if (val is IPDFList && ((IPDFList)val).List != null)
            {
                return ResolveReferences(parent, (IPDFList)val);
            }
            else if (val is IPDFDictionary && ((IPDFDictionary)val).Dict != null)
            {
                return ResolveReferences(parent, (IPDFDictionary)val);
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
            PDFDictionary dict = ((IPDFDictionary)obj).Dict;
            if (dict != null)
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
                    IPDFStream ostream = (IPDFStream)obj;
                    if (ostream.Stream != null && ostream.Stream.Options.TryGet("Type", out type) && type.Name == "ObjStm")
                    {
                        foreach (PDFObject sobj in PDFObject.FromObjStm(ostream.Stream))
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
