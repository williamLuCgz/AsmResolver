﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TUP.AsmResolver.NET.Specialized
{
    public class ParameterDefinition : MetaDataMember
    {
        string name = null;

        public ParameterDefinition(MetaDataRow row)
            : base(row)
        {
        }

        public ParameterDefinition(string name, ElementType parameterType, ParameterAttributes attributes, ushort sequence)
            : base(new MetaDataRow((uint)attributes, sequence, 0U, (uint)parameterType))
        {
        }

        public ParameterAttributes Attributes
        {
            get{return (ParameterAttributes)metadatarow.parts[0];}
        }

        public ushort Sequence
        {
            get { return Convert.ToUInt16(metadatarow.parts[1]); }
        }

        public string Name
        {
            get
            {
                if (name == null)
                    name = netheader.StringsHeap.GetStringByOffset(Convert.ToUInt32(metadatarow.parts[2]));
                return name;
            }
        }

        public ElementType ParameterType
        {
            get;
            internal set;
        }

        public override string ToString()
        {
            return Name;
        }

        public override void ClearCache()
        {
            name = null;
        }
    }
}
