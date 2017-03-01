using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse
{
    public enum PDFTokenType
    {
        Comment,
        Integer,
        Double,
        Boolean,
        Null,
        Name,
        String,
        HexString,
        StartList,
        EndList,
        List,
        StartDictionary,
        EndDictionary,
        Dictionary,
        ObjectRef,
        StartObject,
        EndObject,
        Object,
        Stream,
        Xref,
        Trailer,
        StartXref,
        XrefEntryInUse,
        XrefEntryFree,
        XrefEntry,
        Version,
        EOF
    }
}
