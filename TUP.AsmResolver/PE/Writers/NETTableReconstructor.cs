﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TUP.AsmResolver.NET;
using TUP.AsmResolver.NET.Specialized;
using System.Runtime.InteropServices;
namespace TUP.AsmResolver.PE.Writers
{
    internal class NETTableReconstructor : IReconstructionTask
    {
        TablesHeap tablesHeap;
        NETHeader netHeader;
        MemoryStream stream;
        BinaryWriter writer;

        StringsHeap newStringsHeap;

        internal NETTableReconstructor(TablesHeap tablesHeap)
        {
            this.tablesHeap = tablesHeap;
            this.netHeader = tablesHeap.netheader;
            stream = new MemoryStream();
            writer = new BinaryWriter(stream);
            stream.Seek(0, SeekOrigin.Begin);

        }


        public void Reconstruct()
        {
            newStringsHeap = new StringsHeap(netHeader, netHeader.StringsHeap.headeroffset, new Structures.METADATA_STREAM_HEADER(), "#Strings");
            newStringsHeap.binWriter.Write((byte)0);

            RemoveEmptyTables();
            WriteMetaDataTablessHeaders();
            WriteMetaDataRows();
            UpdateHeapOffsetSizes();
            WriteTablesHeapHeader();

            netHeader.ClearCache();
            int stringsHeapIndex = netHeader.MetaDataStreams.FindIndex(s => s is StringsHeap);
            netHeader.MetaDataStreams[stringsHeapIndex] = newStringsHeap;

            tablesHeap.MakeEmpty();
            tablesHeap.mainStream = stream;
            tablesHeap.binWriter = new BinaryWriter(stream);
            tablesHeap.binReader = new BinaryReader(stream);
            tablesHeap.streamHeader.Size = (uint)stream.Length;
        }

        internal void RemoveEmptyTables()
        {
            ulong validMask = 0;
            for (int i = 0; i < 45; i++)
            {
                if (tablesHeap.HasTable((MetaDataTableType)i))
                {
                    MetaDataTable table = tablesHeap.GetTable((MetaDataTableType)i);
                    if (table.Members.Length > 0)
                        validMask |= ((ulong)1 << i);
                    else
                        tablesHeap.tables[i] = null;
                }
            }
            tablesHeap.MaskValid = validMask;

        }

        internal void UpdateHeapOffsetSizes()
        {
            tablesHeap.HeapOffsetSizes = 0;
            foreach (MetaDataStream stream in netHeader.MetaDataStreams)
            {
                if (stream.StreamSize >= 0xFFFF)
                {
                    stream.indexsize = 4;

                    if (stream is StringsHeap)
                        tablesHeap.HeapOffsetSizes |= 1;
                    if (stream is GuidHeap)
                        tablesHeap.HeapOffsetSizes |= 2;
                    if (stream is BlobHeap)
                        tablesHeap.HeapOffsetSizes |= 4;
                }
            }           

        }

        internal void WriteMetaDataTablessHeaders()
        {
            stream.Seek(0, SeekOrigin.Begin);
            writer.Write(new byte[Marshal.SizeOf(typeof(Structures.METADATA_TABLE_HEADER))]);
            for (int i = 0; i < 45; i++)
            {
                if (tablesHeap.HasTable((MetaDataTableType)i))
                {
                    writer.Write(tablesHeap.GetTable((MetaDataTableType)i).Members.Length);

                }
            }
        }

        internal void WriteTablesHeapHeader()
        {
            stream.Seek(0, SeekOrigin.Begin);
            ASMGlobals.WriteStructureToWriter(writer, tablesHeap.header);
        }

        internal void WriteMetaDataRows()
        {
            for (int i = 0; i < tablesHeap.tables.Length; i++)
            {
                if (tablesHeap.HasTable((MetaDataTableType)i))
                {
                    MetaDataTable table = tablesHeap.GetTable((MetaDataTableType)i);

                    foreach (MetaDataMember member in table.Members)
                    {
                        switch (table.Type)
                        {
                            case MetaDataTableType.Module:
                                WriteModule((ModuleDefinition)member);
                                break;
                            case MetaDataTableType.TypeRef:
                                WriteTypeRef((TypeReference)member);
                                break;
                            case MetaDataTableType.TypeDef:
                                WriteTypeDef((TypeDefinition)member);
                                break;
                            case MetaDataTableType.Field:
                                WriteFieldDef((FieldDefinition)member);
                                break;
                            case MetaDataTableType.Method:
                                WriteMethodDef((MethodDefinition)member);
                                break;
                            case MetaDataTableType.ParamPtr:
                                WriteParamPtr((ParamPtr)member);
                                break;
                            case MetaDataTableType.Param:
                                WriteParamDef((ParameterDefinition)member);
                                break;
                            case MetaDataTableType.InterfaceImpl:
                                WriteInterfaceImpl((InterfaceImplementation)member);
                                break;
                            case MetaDataTableType.MemberRef:
                                WriteMemberRef((MemberReference)member);
                                break;
                            case MetaDataTableType.Constant:
                                WriteConstant((Constant)member);
                                break;
                            case MetaDataTableType.CustomAttribute:
                                WriteCustomAttribute((CustomAttribute)member);
                                break;
                            case MetaDataTableType.FieldMarshal:
                                WriteFieldMarshal((FieldMarshal)member);
                                break;
                            case MetaDataTableType.DeclSecurity:
                                WriteSecurityDecl((SecurityDeclaration)member);
                                break;
                            case MetaDataTableType.ClassLayout:
                                WriteClassLayout((ClassLayout)member);
                                break;
                            case MetaDataTableType.FieldLayout:
                                WriteFieldLayout((FieldLayout)member);
                                break;
                            case MetaDataTableType.StandAloneSig:
                                WriteStandAloneSig((StandAloneSignature)member);
                                break;
                            case MetaDataTableType.EventMap:
                                WriteEventMap((EventMap)member);
                                break;
                            case MetaDataTableType.Event:
                                WriteEventDef((EventDefinition)member);
                                break;
                            case MetaDataTableType.PropertyMap:
                                WritePropertyMap((PropertyMap)member);
                                break;
                            case MetaDataTableType.Property:
                                WritePropertyDef((PropertyDefinition)member);
                                break;
                            case MetaDataTableType.MethodSemantics:
                                WriteMethodSemantics((MethodSemantics)member);
                                break;
                            case MetaDataTableType.MethodImpl:
                                WriteMethodImpl((MethodImplementation)member);
                                break;
                            case MetaDataTableType.ModuleRef:
                                WriteModuleRef((ModuleReference)member);
                                break;
                            case MetaDataTableType.TypeSpec:
                                WriteTypeSpec((TypeSpecification)member);
                                break;
                            case MetaDataTableType.MethodSpec:
                                WriteMethodSpec((MethodSpecification)member);
                                break;
                            case MetaDataTableType.ImplMap:
                                WritePInvokeImpl((PInvokeImplementation)member);
                                break;
                            case MetaDataTableType.FieldRVA:
                                WriteFieldRVA((FieldRVA)member);
                                break;
                            case MetaDataTableType.Assembly:
                                WriteAssemblyDef((AssemblyDefinition)member);
                                break;
                            case MetaDataTableType.AssemblyRef:
                                WriteAssemblyRef((AssemblyReference)member);
                                break;
                            case MetaDataTableType.File:
                                WriteFileRef((FileReference)member);
                                break;
                            case MetaDataTableType.ExportedType:
                                WriteExportedType((ExportedType)member);
                                break;
                            case MetaDataTableType.ManifestResource:
                                WriteManifestRes((ManifestResource)member);
                                break;
                            case MetaDataTableType.NestedClass:
                                WriteNestedClass((NestedClass)member);
                                break;
                            case MetaDataTableType.EncLog:
                                WriteEnCLog((EnCLog)member);
                                break;
                            case MetaDataTableType.EncMap:
                                WriteEnCMap((EnCMap)member);
                                break;
                            case MetaDataTableType.GenericParam:
                                WriteGenericParam((GenericParameter)member);
                                break;
                            case MetaDataTableType.GenericParamConstraint:
                                WriteGenericParamConstraint((GenericParamConstraint)member);
                                break;
                        }
                    }
                }
            }
        }

        internal object GetHeapOffset(MetaDataStream stream, uint offset)
        {
            if (stream.indexsize == 4)
                return offset;
            else
                return (ushort)offset;
        }

        internal object GetMemberIndex(MetaDataMember member)
        {
            MetaDataTable table = tablesHeap.GetTable(member.Table);
            uint index =  (uint)(member.metadatatoken - ((uint)table.Type << 24));
            return ProcessIndex(table, index);
        }

        internal object ProcessIndex(MetaDataTable table, uint index)
        {
            if (table == null || !table.IsLarge(0))
                return (ushort)index;
            else
                return index;
        }

        internal object GetCodedIndex(MetaDataTableGroup group, MetaDataMember member)
        {
            if (group.IsLarge)
                return group.GetCodedIndex(member);
            else
                return (ushort)group.GetCodedIndex(member);
        }

        internal void WriteModule(ModuleDefinition moduleDef)
        {
            if (moduleDef.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    (ushort)0,
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(moduleDef.Name)),
                    GetHeapOffset(netHeader.GuidHeap, netHeader.GuidHeap.GetGuidOffset(moduleDef.Mvid)),
                    GetHeapOffset(netHeader.GuidHeap, 0),
                    GetHeapOffset(netHeader.GuidHeap, 0),
                };
                moduleDef.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(moduleDef.MetaDataRow.GenerateBytes());
        }

        internal void WriteTypeRef(TypeReference typeRef)
        {
            if (typeRef.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    GetCodedIndex(tablesHeap.ResolutionScope, typeRef.ResolutionScope as MetaDataMember),
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(typeRef.Name)),
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(typeRef.Namespace)),
                };
                typeRef.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(typeRef.MetaDataRow.GenerateBytes());
        }

        internal void WriteTypeDef(TypeDefinition typeDef)
        {
            if (typeDef.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    (uint)typeDef.Attributes,
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(typeDef.Name)),
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(typeDef.Namespace)),
                    GetCodedIndex(tablesHeap.TypeDefOrRef, typeDef.BaseType),
                    ProcessIndex(tablesHeap.GetTable(MetaDataTableType.Field), typeDef.FieldList),
                    ProcessIndex(tablesHeap.GetTable(MetaDataTableType.Method), typeDef.MethodList),
                };
                typeDef.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(typeDef.MetaDataRow.GenerateBytes());
        }

        internal void WriteFieldDef(FieldDefinition fieldDef)
        {
            if (fieldDef.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    (ushort)fieldDef.Attributes,
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(fieldDef.Name)),
                    GetHeapOffset(netHeader.BlobHeap, Convert.ToUInt32(fieldDef.metadatarow.parts[2])), // TODO: Serialize signatures.

                };
                fieldDef.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(fieldDef.MetaDataRow.GenerateBytes());
        }

        internal void WriteMethodDef(MethodDefinition methodDef)
        {
            if (methodDef.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    (uint)methodDef.RVA,
                    (ushort)methodDef.ImplementationAttributes,
                    (ushort)methodDef.Attributes,
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(methodDef.Name)),
                    GetHeapOffset(netHeader.BlobHeap, Convert.ToUInt32(methodDef.metadatarow.parts[4])), // TODO: Serialize signatures.
                    ProcessIndex(tablesHeap.GetTable(MetaDataTableType.Param), Convert.ToUInt32(methodDef.metadatarow.parts[5])),
                };
                methodDef.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(methodDef.MetaDataRow.GenerateBytes());
        }

        internal void WriteParamPtr(ParamPtr paramPtr)
        {
            if (paramPtr.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    GetHeapOffset(netHeader.BlobHeap, Convert.ToUInt32(paramPtr.metadatarow.parts[0])), // TODO: Serialize signatures.
                };
                paramPtr.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(paramPtr.MetaDataRow.GenerateBytes());
        }

        internal void WriteParamDef(ParameterDefinition paramDef)
        {
            if (paramDef.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    (ushort)paramDef.Attributes,
                    paramDef.Sequence,
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(paramDef.Name)),
                };
                paramDef.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(paramDef.MetaDataRow.GenerateBytes());
        }

        internal void WriteInterfaceImpl(InterfaceImplementation interfaceImpl)
        {
            if (interfaceImpl.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    GetMemberIndex(interfaceImpl.Class),
                    GetCodedIndex(tablesHeap.TypeDefOrRef, interfaceImpl.Interface),
                };
                interfaceImpl.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(interfaceImpl.MetaDataRow.GenerateBytes());
        }

        internal void WriteMemberRef(MemberReference memberRef)
        {
            if (memberRef.UpdateRowOnRebuild)
            {
                object[] parts = new object[] 
                {
                    GetCodedIndex(tablesHeap.MemberRefParent, memberRef.DeclaringType),
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(memberRef.Name)),
                    GetHeapOffset(netHeader.BlobHeap, Convert.ToUInt32(memberRef.MetaDataRow.Parts[2])), // TODO: Serialize signatures.
                };
                memberRef.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(memberRef.MetaDataRow.GenerateBytes());
        }

        internal void WriteConstant(Constant constant)
        {
            if (constant.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    (byte)constant.ConstantType,
                    (byte)0,
                    GetCodedIndex(tablesHeap.HasConstant, constant.Parent),
                    GetHeapOffset(netHeader.BlobHeap, Convert.ToUInt32(constant.MetaDataRow.Parts[3])), // TODO: Serialize signatures.
                };
                constant.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(constant.MetaDataRow.GenerateBytes());
        }

        internal void WriteCustomAttribute(CustomAttribute attribute)
        {
            if (attribute.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    GetCodedIndex(tablesHeap.HasCustomAttribute, attribute.Parent),
                    GetCodedIndex(tablesHeap.CustomAttributeType, attribute.Constructor),
                    GetHeapOffset(netHeader.BlobHeap, Convert.ToUInt32(attribute.MetaDataRow.Parts[2])), // TODO: Serialize signatures.
                };
                attribute.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(attribute.MetaDataRow.GenerateBytes());
        }

        internal void WriteFieldMarshal(FieldMarshal fieldMarshal)
        {
            if (fieldMarshal.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    GetCodedIndex(tablesHeap.HasFieldMarshall, fieldMarshal.Parent),
                    GetHeapOffset(netHeader.BlobHeap, Convert.ToUInt32(fieldMarshal.MetaDataRow.Parts[1])), // TODO: Serialize signatures.
                };
                fieldMarshal.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(fieldMarshal.MetaDataRow.GenerateBytes());
        }

        internal void WriteSecurityDecl(SecurityDeclaration securityDecl)
        {
            if (securityDecl.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    (ushort)securityDecl.Action,
                    GetCodedIndex(tablesHeap.HasDeclSecurity, securityDecl.Parent),
                    GetHeapOffset(netHeader.BlobHeap, Convert.ToUInt32(securityDecl.MetaDataRow.Parts[2])), // TODO: Serialize signatures.
                };
                securityDecl.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(securityDecl.MetaDataRow.GenerateBytes());
        }

        internal void WriteClassLayout(ClassLayout classLayout)
        {
            if (classLayout.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    classLayout.PackingSize,
                    classLayout.ClassSize,
                    ProcessIndex(tablesHeap.GetTable(MetaDataTableType.TypeDef),classLayout.Parent.TableIndex),
                };
                classLayout.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(classLayout.MetaDataRow.GenerateBytes());
        }

        internal void WriteFieldLayout(FieldLayout fieldLayout)
        {
            if (fieldLayout.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    fieldLayout.Offset,
                    GetMemberIndex(fieldLayout.Field),  
                };
                fieldLayout.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(fieldLayout.MetaDataRow.GenerateBytes());
        }

        internal void WriteStandAloneSig(StandAloneSignature signature)
        {
            if (signature.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    GetHeapOffset(netHeader.BlobHeap, Convert.ToUInt32(signature.MetaDataRow.Parts[0])), // TODO: Serialize signatures.
                };
                signature.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(signature.MetaDataRow.GenerateBytes());
        }

        internal void WriteEventMap(EventMap eventMap)
        {
            if (eventMap.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    GetMemberIndex(eventMap.Parent),
                    GetMemberIndex(eventMap.Events[0]),
                };
                eventMap.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(eventMap.MetaDataRow.GenerateBytes());
        }

        internal void WriteEventDef(EventDefinition eventDef)
        {
            if (eventDef.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    (ushort)eventDef.Attributes,
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(eventDef.Name)),
                    GetCodedIndex(tablesHeap.TypeDefOrRef, eventDef.EventType),
                };
                eventDef.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(eventDef.MetaDataRow.GenerateBytes());
        }

        internal void WritePropertyMap(PropertyMap propertyMap)
        {
            if (propertyMap.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    GetMemberIndex(propertyMap.Parent),
                    GetMemberIndex(propertyMap.Properties[0]),
                };
                propertyMap.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(propertyMap.MetaDataRow.GenerateBytes());
        }

        internal void WritePropertyDef(PropertyDefinition propertyDef)
        {
            if (propertyDef.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    (ushort)propertyDef.Attributes,
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(propertyDef.Name)),
                    GetHeapOffset(netHeader.BlobHeap, Convert.ToUInt32(propertyDef.MetaDataRow.parts[2])), // TODO: Serialize signatures.
                };
                propertyDef.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(propertyDef.MetaDataRow.GenerateBytes());
        }

        internal void WriteMethodSemantics(MethodSemantics semantics)
        {
            if (semantics.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    (ushort)semantics.Attributes,
                    GetMemberIndex(semantics.Method),
                    GetCodedIndex(tablesHeap.HasSemantics, semantics.Association),
                };
                semantics.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(semantics.MetaDataRow.GenerateBytes());
        }

        internal void WriteMethodImpl(MethodImplementation implementation)
        {
            if (implementation.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    GetMemberIndex(implementation.Class),
                    GetCodedIndex(tablesHeap.MethodDefOrRef, implementation.MethodBody),
                    GetCodedIndex(tablesHeap.MethodDefOrRef, implementation.MethodDeclaration),
                };
                implementation.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(implementation.MetaDataRow.GenerateBytes());
        }

        internal void WriteModuleRef(ModuleReference moduleRef)
        {
            if (moduleRef.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(moduleRef.Name)),
                };
                moduleRef.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(moduleRef.MetaDataRow.GenerateBytes());
        }

        internal void WriteTypeSpec(TypeSpecification typeSpec)
        {
            if (typeSpec.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    GetHeapOffset(netHeader.BlobHeap, typeSpec.Signature), // TODO: Serialize signatures.
                };
                typeSpec.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(typeSpec.MetaDataRow.GenerateBytes());
        }

        internal void WriteMethodSpec(MethodSpecification methodSpec)
        {
            if (methodSpec.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    GetCodedIndex(tablesHeap.MethodDefOrRef, methodSpec.OriginalMethod),
                    GetHeapOffset(netHeader.BlobHeap, Convert.ToUInt32(methodSpec.MetaDataRow.parts[1])), // TODO: Serialize signatures.
                };
                methodSpec.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(methodSpec.MetaDataRow.GenerateBytes());
        }

        internal void WritePInvokeImpl(PInvokeImplementation implementation)
        {
            if (implementation.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    (ushort)implementation.Attributes,
                    GetCodedIndex(tablesHeap.MemberForwarded, implementation.Member),
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(implementation.Entrypoint)),
                    GetMemberIndex(implementation.ImportScope),
                };
                implementation.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(implementation.MetaDataRow.GenerateBytes());
        }

        internal void WriteFieldRVA(FieldRVA fieldRVA)
        {
            if (fieldRVA.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    (uint)fieldRVA.RVA,
                    GetMemberIndex(fieldRVA.Field)
                };
                fieldRVA.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(fieldRVA.MetaDataRow.GenerateBytes());
        }

        internal void WriteAssemblyDef(AssemblyDefinition asmDef)
        {
            if (asmDef.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    (uint)asmDef.HashAlgorithm,
                    (ushort)asmDef.Version.Major,
                    (ushort)asmDef.Version.Minor,
                    (ushort)asmDef.Version.Build,
                    (ushort)asmDef.Version.Revision,
                    (uint)asmDef.Attributes,
                    GetHeapOffset(netHeader.BlobHeap, Convert.ToUInt32(asmDef.MetaDataRow.parts[6])), // TODO: Serialize signatures.
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(asmDef.Name)),
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(asmDef.Culture)),
                };
                asmDef.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(asmDef.MetaDataRow.GenerateBytes());
        }

        internal void WriteAssemblyRef(AssemblyReference asmRef)
        {
            if (asmRef.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    (ushort)asmRef.Version.Major,
                    (ushort)asmRef.Version.Minor,
                    (ushort)asmRef.Version.Build,
                    (ushort)asmRef.Version.Revision,
                    (uint)asmRef.Attributes,
                    GetHeapOffset(netHeader.BlobHeap, Convert.ToUInt32(asmRef.MetaDataRow.parts[5])), // TODO: Serialize signatures.
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(asmRef.Name)),
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(asmRef.Culture)),
                    GetHeapOffset(netHeader.BlobHeap, Convert.ToUInt32(asmRef.MetaDataRow.parts[8])), // TODO: Serialize signatures.
                    
                };
                asmRef.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(asmRef.MetaDataRow.GenerateBytes());
        }

        internal void WriteFileRef(FileReference fileRef)
        {
            if (fileRef.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    (uint)fileRef.Flags,
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(fileRef.Name)),
                    GetHeapOffset(netHeader.BlobHeap, fileRef.Hash), // TODO: Serialize signatures.
                };
                fileRef.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(fileRef.MetaDataRow.GenerateBytes());
        }

        internal void WriteExportedType(ExportedType type)
        {
            if (type.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    (uint)type.Attributes,
                    type.TypeID,
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(type.TypeName)),
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(type.TypeNamespace)),
                    GetCodedIndex(tablesHeap.Implementation, type.Implementation),
                };
                type.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(type.MetaDataRow.GenerateBytes());
        }

        internal void WriteManifestRes(ManifestResource resource)
        {
            if (resource.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    resource.Offset,
                    (uint)resource.Attributes,
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(resource.Name)),
                    GetCodedIndex(tablesHeap.Implementation, resource.Implementation),
                };
                resource.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(resource.MetaDataRow.GenerateBytes());
        }

        internal void WriteNestedClass(NestedClass nestedClass)
        {
            if (nestedClass.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    GetMemberIndex(nestedClass.Class),
                    GetMemberIndex(nestedClass.EnclosingClass),
                };
                nestedClass.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(nestedClass.MetaDataRow.GenerateBytes());
        }

        internal void WriteEnCLog(EnCLog log)
        {
            if (log.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    log.Token,
                    log.FuncCode, 
                };
                log.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(log.MetaDataRow.GenerateBytes());
        }

        internal void WriteEnCMap(EnCMap map)
        {
            if (map.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    map.Token,
                };
                map.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(map.MetaDataRow.GenerateBytes());
        }

        internal void WriteGenericParam(GenericParameter genericParameter)
        {
            if (genericParameter.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    genericParameter.Index,
                    (ushort)genericParameter.GenericAttributes,
                    GetCodedIndex(tablesHeap.TypeOrMethod, genericParameter.Owner),
                    GetHeapOffset(newStringsHeap, newStringsHeap.GetStringOffset(genericParameter.Name)),
                };
                genericParameter.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(genericParameter.MetaDataRow.GenerateBytes());
        }

        internal void WriteGenericParamConstraint(GenericParamConstraint paramConstraint)
        {
            if (paramConstraint.UpdateRowOnRebuild)
            {
                object[] parts = new object[]
                {
                    GetMemberIndex(paramConstraint.Owner),
                    GetCodedIndex(tablesHeap.TypeDefOrRef, paramConstraint.Constraint),
                };
                paramConstraint.MetaDataRow = new MetaDataRow(parts);
            }
            writer.Write(paramConstraint.MetaDataRow.GenerateBytes());
        }

    }
}
