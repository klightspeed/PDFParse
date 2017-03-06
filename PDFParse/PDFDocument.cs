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
        public IPDFDictionary StructTreeRoot { get { return Root.Dict.Get<IPDFDictionary>("StructTreeRoot"); } }
        public Dictionary<PDFObjRef, Dictionary<long, PDFContentBlock>> ContentBlocks { get; private set; }
        public Dictionary<PDFObjRef, Dictionary<long, PDFContentBlock>> StructBlocks { get; private set; }
        public PDFContentBlock StructTree { get; private set; }

        public IEnumerable<IPDFDictionary> Pages { get { return GetPages(Root.Dict.Get<IPDFDictionary>("Pages")); } }

        public PDFDocument()
        {
            this.ContentBlocks = new Dictionary<PDFObjRef,Dictionary<long,PDFContentBlock>>();
            this.StructBlocks = new Dictionary<PDFObjRef, Dictionary<long, PDFContentBlock>>();
        }

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

        protected PDFContentBlock ProcessTreeNode(IPDFDictionary node, PDFName type)
        {
            PDFContentBlock cb = new PDFContentBlock
            {
                StartMarker = new PDFContentOperator
                {
                    Name = "BDC",
                    Arguments = new List<IPDFToken>
                    {
                        type,
                        node
                    }
                },
                Content = new List<PDFContentOperator>()
            };

            if (node.Dict.ContainsKey("K"))
            {
                IPDFElement K = node.Dict["K"];
                if (K is IPDFList && ((IPDFList)K).List != null)
                {
                    foreach (IPDFDictionary v in ((IPDFList)K).List.OfType<IPDFDictionary>())
                    {
                        PDFName vtype;
                        v.Dict.TryGet("S", out vtype);
                        PDFContentBlock blk = ProcessTreeNode(v, vtype);
                        blk.Parent = cb;
                        cb.Content.Add(blk);
                    }
                }
                else if (K is IPDFDictionary && ((IPDFDictionary)K).Dict != null)
                {
                    PDFName vtype;
                    ((IPDFDictionary)K).Dict.TryGet("S", out vtype);
                    PDFContentBlock blk = ProcessTreeNode((IPDFDictionary)K, vtype);
                    blk.Parent = cb;
                    cb.Content.Add(blk);
                }
                else if (K is PDFInteger && node.Dict.ContainsKey("Pg"))
                {
                    long mcid = ((PDFInteger)K).Value;
                    IPDFObjRef objref;

                    if (node.Dict.TryGet("Pg", out objref))
                    {
                        if (StructBlocks.ContainsKey(objref.ObjRef))
                        {
                            Dictionary<long, PDFContentBlock> blocksByMcid = StructBlocks[objref.ObjRef];
                            if (blocksByMcid.ContainsKey(mcid))
                            {
                                PDFContentBlock blk = blocksByMcid[mcid];
                                blk.Parent = cb;
                                cb.Content.Add(blk);
                            }
                        }
                    }
                }
            }

            return cb;
        }

        protected PDFContentBlock ProcessParentTreeLeaf(IPDFDictionary node)
        {
            IPDFObjRef objref;
            IPDFElement kv;
            PDFName type;

            PDFContentBlock cb = null;

            if (node.Dict.TryGet("K", out kv) && node.Dict.TryGet("Pg", out objref) && node.Dict.TryGet("S", out type))
            {
                cb = new PDFContentBlock
                {
                    StartMarker = new PDFContentOperator
                    {
                        Name = "BDC",
                        Arguments = new List<IPDFToken>
                        {
                            type,
                            node
                        }
                    },
                    Content = new List<PDFContentOperator>()
                };

                if (kv is PDFInteger)
                {
                    PDFInteger mcid = (PDFInteger)kv;
                    PDFContentBlock blk = ContentBlocks[objref.ObjRef][mcid.Value];
                    cb.Content.Add(blk);
                }
                else if (kv is IPDFList && ((IPDFList)kv).List != null)
                {
                    PDFList kl = ((IPDFList)kv).List;
                    for (int k = 0; k < kl.Count; k++)
                    {
                        IPDFElement elem;
                        if (kl.TryGet(k, out elem))
                        {
                            if (elem is PDFInteger)
                            {
                                PDFInteger mcid = (PDFInteger)elem;
                                if (ContentBlocks.ContainsKey(objref.ObjRef) && ContentBlocks[objref.ObjRef].ContainsKey(mcid.Value))
                                {
                                    PDFContentBlock blk = ContentBlocks[objref.ObjRef][mcid.Value];
                                    cb.Content.Add(blk);
                                }
                            }
                            else if (elem is IPDFDictionary)
                            {
                                IPDFDictionary dict = (IPDFDictionary)node;
                            }
                        }
                    }
                }
            }

            return cb;
        }

        protected PDFContentBlock CreateParentTreeNode(int j, IPDFDictionary d)
        {
            PDFContentBlock cb = null;
            IPDFObjRef objref;
            IPDFElement kv;
            PDFName type;

            if (d.Dict.TryGet("K", out kv) && d.Dict.TryGet("Pg", out objref) && d.Dict.TryGet("S", out type))
            {
                if (!StructBlocks.ContainsKey(objref.ObjRef))
                {
                    StructBlocks[objref.ObjRef] = new Dictionary<long, PDFContentBlock>();
                }

                cb = new PDFContentBlock
                {
                    StartMarker = new PDFContentOperator
                    {
                        Name = "BDC",
                        Arguments = new List<IPDFToken>
                        {
                            type,
                            d
                        }
                    },
                    Content = new List<PDFContentOperator>()
                };

                StructBlocks[objref.ObjRef][j] = cb;

                if (kv is PDFInteger)
                {
                    PDFInteger mcid = (PDFInteger)kv;
                    PDFContentBlock blk = ContentBlocks[objref.ObjRef][mcid.Value];
                    cb.Content.Add(blk);
                }
            }

            return cb;
        }

        protected void ProcessParentTreeNode(PDFContentBlock cb, IPDFDictionary d)
        {
            IPDFObjRef objref;
            IPDFList kv;
            PDFName type;

            if (d.Dict.TryGet("K", out kv) && d.Dict.TryGet("Pg", out objref) && d.Dict.TryGet("S", out type))
            {
                if (kv.List != null)
                {
                    PDFList kl = ((IPDFList)kv).List;
                    for (int k = 0; k < kl.Count; k++)
                    {
                        IPDFElement elem;
                        if (kl.TryGet(k, out elem))
                        {
                            if (elem is PDFInteger)
                            {
                                PDFInteger mcid = (PDFInteger)elem;
                                if (StructBlocks.ContainsKey(objref.ObjRef) && StructBlocks[objref.ObjRef].ContainsKey(mcid.Value))
                                {
                                    PDFContentBlock blk = StructBlocks[objref.ObjRef][mcid.Value];
                                    cb.Content.Add(blk);
                                }
                            }
                            else if (elem is IPDFDictionary)
                            {
                                cb.Content.Add(ProcessParentTreeLeaf((IPDFDictionary)elem));
                            }
                        }
                    }
                }
            }
        }

        protected void ProcessParentTree()
        {
            IPDFDictionary troot = StructTreeRoot;
            IPDFDictionary ptree;
            if (troot.Dict.TryGet("ParentTree", out ptree))
            {
                IPDFList nums;
                if (ptree.Dict.TryGet("Nums", out nums))
                {
                    PDFList list = nums.List;

                    for (int i = 0; i < list.Count; i += 2)
                    {
                        PDFInteger vi;
                        IPDFElement vv;
                        if (list.TryGet(i, out vi) && list.TryGet(i + 1, out vv))
                        {
                            if (vv is IPDFList && ((IPDFList)vv).List != null)
                            {
                                IPDFList l = (IPDFList)vv;
                                PDFContentBlock[] cblist = new PDFContentBlock[l.List.Count];
                                IPDFDictionary[] dlist = new IPDFDictionary[l.List.Count];

                                for (int j = 0; j < l.List.Count; j++)
                                {
                                    IPDFDictionary d;
                                    if (l.List.TryGet(j, out d))
                                    {
                                        dlist[j] = d;
                                        cblist[j] = CreateParentTreeNode(j, d);
                                    }
                                }

                                for (int j = 0; j < cblist.Length; j++)
                                {
                                    if (dlist[j] != null && cblist[j] != null)
                                    {
                                        ProcessParentTreeNode(cblist[j], dlist[j]);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        protected void ProcessPageContentBlock(PDFContentBlock block, Dictionary<long, PDFContentBlock> blocksByMcid)
        {
            if (block.BlockOptions != null && block.BlockOptions.ContainsKey("MCID"))
            {
                PDFInteger mcid;
                if (block.BlockOptions.TryGet<PDFInteger>("MCID", out mcid))
                {
                    blocksByMcid[mcid.Value] = block;
                }
            }

            foreach (PDFContentBlock cblock in block.Content.OfType<PDFContentBlock>())
            {
                ProcessPageContentBlock(cblock, blocksByMcid);
            }
        }

        protected void ProcessPageContentBlocks(PDFContent content, Dictionary<long, PDFContentBlock> blocksByMcid)
        {
            foreach (PDFContentBlock block in content.Tokens.OfType<PDFContentBlock>())
            {
                ProcessPageContentBlock(block, blocksByMcid);
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
                    PDFContent pcontent = null;

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
                        pcontent = new PDFContent(data.ToArray(), page);
                    }
                    else if (cstream != null && cstream.Stream != null)
                    {
                        pcontent = new PDFContent(cstream.Stream.Data, page);
                    }

                    if (pcontent != null)
                    {
                        page.Dict["PageContent"] = pcontent;
                        Dictionary<long, PDFContentBlock> blocks = new Dictionary<long, PDFContentBlock>();
                        doc.ContentBlocks[((IPDFObjRef)page).ObjRef] = blocks;
                        doc.ProcessPageContentBlocks(pcontent, blocks);
                    }
                }
            }

            doc.ProcessParentTree();
            doc.StructTree = doc.ProcessTreeNode(doc.StructTreeRoot, (PDFName)doc.StructTreeRoot.Dict["Type"]);

            return doc;
        }

        public static PDFDocument ParseDocument(string filename)
        {
            return ParseDocument(new ByteStreamReader(File.ReadAllBytes(filename)));
        }
    }
}
