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
        public IPDFDictionary Root { get { return Trailer.TrailerDictionary.Dict.Get<IPDFDictionary>("Root"); } }
        public IPDFDictionary Info { get { return Trailer.TrailerDictionary.Dict.Get<IPDFDictionary>("Info"); } }

        public IEnumerable<IPDFDictionary> Pages { get { return GetPages(Root.Dict.Get<IPDFDictionary>("Pages")); } }

        protected static IEnumerable<IPDFDictionary> GetPages(IPDFDictionary root)
        {
            PDFName type;
            IPDFList kids;
            if (root.Dict.TryGet("Type", out type) && type.Name == "Pages" && root.Dict.TryGet("Kids", out kids))
            {
                foreach (IPDFDictionary node in kids.List.OfType<IPDFDictionary>())
                {
                    foreach (IPDFDictionary leaf in GetPages(node))
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

            foreach (IPDFDictionary page in doc.Pages)
            {
                IPDFElement content;
                if (page.Dict.TryGet<IPDFElement>("Contents", out content))
                {
                    IPDFList clist = content as IPDFList;
                    IPDFStream cstream = content as IPDFStream;

                    if (clist != null && clist.List != null)
                    {
                        List<byte> data = new List<byte>();
                        foreach (IPDFStream elem in clist.List.OfType<IPDFStream>())
                        {
                            if (elem.Stream != null)
                            {
                                data.AddRange(elem.Stream.Data);
                            }
                        }
                        page.Dict["PageContent"] = new PDFContent(data.ToArray(), page);
                    }
                    else if (cstream != null && cstream.Stream != null)
                    {
                        page.Dict["PageContent"] = new PDFContent(cstream.Stream.Data, page);
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
