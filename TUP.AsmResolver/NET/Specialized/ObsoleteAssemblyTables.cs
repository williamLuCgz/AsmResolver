﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUP.AsmResolver.NET.Specialized
{
    public class AssemblyOS : MetaDataMember
    {
        public AssemblyOS(MetaDataRow row)
            : base(row)
        {
        }

        public AssemblyOS(uint platformID, uint majorversion, uint minorversion)
            : base(new MetaDataRow(platformID, majorversion, minorversion))
        {
        }

        public uint PlatformID
        {
            get { return Convert.ToUInt32(metadatarow.parts[0]); }
            set { metadatarow.parts[0] = value; }
        }

        public uint MajorVersion
        {
            get { return Convert.ToUInt32(metadatarow.parts[1]); }
            set { metadatarow.parts[1] = value; }
        }

        public uint MinorVersion
        {
            get { return Convert.ToUInt32(metadatarow.parts[2]); }
            set { metadatarow.parts[2] = value; }
        }

        public override void ClearCache()
        {
        }
    }

    public class AssemblyProcessor : MetaDataMember 
    {
        public AssemblyProcessor(MetaDataRow row)
            : base(row)
        {
        }

        public AssemblyProcessor(uint processor)
            : base(new MetaDataRow(processor))
        {
        }

        public uint Processor
        {
            get { return Convert.ToUInt32(metadatarow.parts[0]); }
            set { metadatarow.parts[0] = value; }
        }

        public override void ClearCache()
        {
        }
    }

    public class AssemblyRefOS : AssemblyOS
    {
        AssemblyReference reference;

        public AssemblyRefOS(MetaDataRow row)
            : base(row)
        {
        }

        public AssemblyRefOS(uint platformID, uint majorversion, uint minorversion, AssemblyReference asmReference)
            : base(new MetaDataRow(platformID, majorversion, minorversion, asmReference.TableIndex))
        {
            reference = asmReference;
        }

        public AssemblyReference Reference
        {
            get
            {
                if (reference == null && NETHeader.TablesHeap.HasTable(MetaDataTableType.AssemblyRef))
                {
                    MetaDataTable asmrefTable = NETHeader.TablesHeap.GetTable(MetaDataTableType.AssemblyRef);
                    asmrefTable.TryGetMember(Convert.ToInt32(metadatarow.parts[3]), out reference);
                }
                return reference;
            }
        }

        public override void ClearCache()
        {
            base.ClearCache();
            reference = null;
        }
    }

    public class AssemblyRefProcessor : AssemblyProcessor
    {
        AssemblyReference reference;

        public AssemblyRefProcessor(MetaDataRow row)
            : base(row)
        {
        }

        public AssemblyRefProcessor(uint processor, AssemblyReference asmReference)
            : base(new MetaDataRow(processor, asmReference.TableIndex))
        {
            reference = asmReference;
        }

        public AssemblyReference Reference
        {
            get
            {
                if (reference == null && NETHeader.TablesHeap.HasTable(MetaDataTableType.AssemblyRef))
                {
                    MetaDataTable asmrefTable = NETHeader.TablesHeap.GetTable(MetaDataTableType.AssemblyRef);
                    asmrefTable.TryGetMember(Convert.ToInt32(metadatarow.parts[1]), out reference);
                }
                return reference;
            }
        }

        public override void ClearCache()
        {
            base.ClearCache();
            reference = null;
        }
    }
}
