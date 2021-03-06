﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TUP.AsmResolver.NET.Specialized
{
    public class InterfaceImplementation : MetaDataMember
    {
        TypeDefinition @class = null;
        TypeReference @interface = null;

        public InterfaceImplementation(MetaDataRow row)
            : base(row)
        {
        }

        public InterfaceImplementation(TypeDefinition @class, TypeReference @interface)
            : base(new MetaDataRow(@class.TableIndex, 0U))
        {
            this.@class = @class;
            this.@interface = @interface;
        }
    
        public TypeDefinition Class
        {
            get
            {
                if (@class == null)
                {
                    int token = Convert.ToInt32(metadatarow.parts[0]) - 1;
                    if (token >= 0)
                        @class = (TypeDefinition)netheader.TablesHeap.GetTable(MetaDataTableType.TypeDef).Members[token];
                }
                return @class;
            }
        }

        public TypeReference Interface
        {
            get
            {
                if (@interface == null)
                    @interface = (TypeReference)netheader.TablesHeap.TypeDefOrRef.GetMember(Convert.ToInt32(metadatarow.parts[1]));
                return @interface;
            }
        }

        public override string ToString()
        {
            return Interface.ToString();
        }

        public override void ClearCache()
        {
            @class = null;
            @interface = null;
        }
    }
}
