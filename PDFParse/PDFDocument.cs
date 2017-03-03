using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using Ionic.Zlib;
using PDFParse.Primitives;

namespace PDFParse
{

    public class PDFDocument : PDFDocumentBase
    {
        public PDFObject Root { get { return Trailer.TrailerDictionary.Get<PDFObject>("Root"); } }
        public PDFObject Info { get { return Trailer.TrailerDictionary.Get<PDFObject>("Info"); } }

        public IEnumerable<PDFObject> Pages { get { return GetPages(Root.Get<PDFObject>("Pages")); } }

        protected static IEnumerable<PDFObject> GetPages(PDFObject root)
        {
            PDFName type;
            if (root.Value == null || (root.TryGet("Type", out type) && type.Name == "Pages"))
            {
                foreach (PDFObject node in root.Children)
                {
                    foreach (PDFObject leaf in GetPages(node))
                    {
                        yield return leaf;
                    }
                }
            }
            else
            {
                yield return root;
            }
        }

        protected static PDFDocument ParseDocument(ByteStreamReader reader)
        {
            PDFDocument doc = new PDFDocument();
            doc.Load(reader);

            foreach (PDFObject obj in doc.Objects.Values)
            {
                PDFDictionary dict;
                if (obj.TryGet(out dict))
                {
                    PDFList kids;
                    if (dict.TryGet("Kids", out kids))
                    {
                        for (int i = 0; i < kids.Count; i++)
                        {
                            PDFObject kid;
                            if (kids.TryGet(i, out kid))
                            {
                                obj.Children.Add(kid);
                                kid.Parent = obj;
                            }
                        }
                    }

                    /*
                    PDFObject parent;
                    if (dict.TryGet("Parent", out parent))
                    {
                        obj.Parent = parent;
                        parent.Children.Add(obj);
                    }
                     */
                }
            }

            foreach (PDFObject page in doc.Pages)
            {
                IPDFElement content;
                if (page.TryGet<IPDFElement>("Contents", out content))
                {
                    if (content is PDFList)
                    {
                        List<byte> data = new List<byte>();
                        foreach (IPDFElement elem in (PDFList)content)
                        {
                            if (elem is PDFObject)
                            {
                                PDFObject contentobj = (PDFObject)elem;
                                if (contentobj.Stream != null)
                                {
                                    data.AddRange(contentobj.Stream.Data);
                                }
                            }
                        }
                        page.Dict["PageContent"] = new PDFContent(data.ToArray(), page);
                    }
                    else if (content is PDFObject)
                    {
                        PDFObject contentobj = (PDFObject)content;
                        if (contentobj.Stream != null)
                        {
                            page.Dict["PageContent"] = new PDFContent(contentobj.Stream.Data, page);
                        }
                    }
                }
            }

            return doc;
        }

        public static PDFDocument ParseDocument(string filename)
        {
            return ParseDocument(new ByteStreamReader(File.ReadAllBytes(filename)));
        }
    }
}
