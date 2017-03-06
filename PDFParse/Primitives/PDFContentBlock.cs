using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace PDFParse.Primitives
{
    public class PDFContentBlock : PDFContentOperator
    {
        public PDFContentOperator StartMarker { get; set; }
        public List<PDFContentOperator> Content { get; set; }
        public PDFContentBlock Parent { get; set; }

        public override string Text { get { return String.Join("", Content.OfType<PDFContentOperator>().Select(c => c.Text).Where(t => t != null)); } }

        public PointF TextPos
        {
            get
            {
                PDFContentOperator Tm = Content.FirstOrDefault(c => c.Name == "Tm");
                if (Tm != null)
                {
                    double x = ((IPDFValue<double>)Tm.Arguments[4]).Value;
                    double y = ((IPDFValue<double>)Tm.Arguments[5]).Value;
                    return new PointF((float)x, (float)y);
                }
                else
                {
                    return PointF.Empty;
                }
            }
        }

        public double Rot
        {
            get
            {
                PDFContentOperator Tm = Content.FirstOrDefault(c => c.Name == "Tm");
                if (Tm != null)
                {
                    double rotx = ((IPDFValue<double>)Tm.Arguments[0]).Value;
                    double roty = ((IPDFValue<double>)Tm.Arguments[1]).Value;
                    double size = Math.Sqrt(rotx * rotx + roty * roty);

                    if (size < 0.1)
                    {
                        return 0;
                    }
                    else
                    {
                        return Math.Atan2(roty, rotx) * 180.0 / Math.PI;
                    }
                }
                else
                {
                    return 0;
                }
            }
        }

        public RectangleF CropBox
        {
            get
            {
                PDFContentOperator W = Content.FirstOrDefault(c => c.Name == "W");
                if (W != null)
                {
                    PDFContentOperator re = W.Arguments.OfType<PDFContentOperator>().FirstOrDefault(c => c.Name == "re");
                    if (re != null)
                    {
                        double x = ((IPDFValue<double>)re.Arguments[0]).Value;
                        double y = ((IPDFValue<double>)re.Arguments[1]).Value;
                        double w = ((IPDFValue<double>)re.Arguments[2]).Value;
                        double h = ((IPDFValue<double>)re.Arguments[3]).Value;
                        return new RectangleF((float)x, (float)y, (float)w, (float)h);
                    }
                }

                return RectangleF.Empty;
            }
        }

        public PDFName BlockType { get { return StartMarker.Arguments.OfType<PDFName>().FirstOrDefault(); } }
        public PDFDictionary BlockOptions { get { return StartMarker.Arguments.OfType<PDFDictionary>().FirstOrDefault(); } }


        public override string ToString()
        {
            PointF pos = TextPos;
            double rot = Rot;
            PDFName blocktype = BlockType;

            return (blocktype.Name ?? "") + " @(" + pos.X.ToString() + ", " + pos.Y.ToString() + ") Rot(" + rot.ToString() + "): " + Text;
            //return String.Join(" ", new[] { StartMarker.ToString() }.Concat(Content.Select(s => s.ToString())).Concat(Arguments.Select(s => s.ToString()))) + " " + Name;
        }
    }
}
