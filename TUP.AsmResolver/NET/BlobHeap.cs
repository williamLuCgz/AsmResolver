﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TUP.AsmResolver.NET.Specialized;
using TUP.AsmResolver.NET.Specialized.MSIL;
using TUP.AsmResolver.PE;
namespace TUP.AsmResolver.NET
{
    /// <summary>
    /// Represents the blob heap stream containing various values of many metadata Members.
    /// </summary>
    public class BlobHeap : MetaDataStream
    {

        internal SortedDictionary<uint, byte[]> readBlobs = new SortedDictionary<uint, byte[]>();
        
        //MetaDataStream stream)
            //: base(stream)
        internal BlobHeap(NETHeader netheader, int headeroffset, Structures.METADATA_STREAM_HEADER rawHeader, string name)
            : base(netheader, headeroffset, rawHeader, name)
        {
        }


        internal override void Initialize()
        {
        }

        internal void Reconstruct()
        {
            // will be removed once blobs are being serialized.

            MemoryStream newStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(newStream);
            writer.Write((byte)0);
            ReadAllBlobs();
        
            foreach (var blob in readBlobs)
            {
                NETGlobals.WriteCompressedUInt32(writer, (uint)blob.Value.Length);
                writer.Write(blob.Value);
            }
        
            mainStream.Dispose();
            binReader.Dispose();
            binWriter.Dispose();
            mainStream = newStream;
            binReader = new BinaryReader(newStream);
            binWriter = new BinaryWriter(newStream);
            this.streamHeader.Size = (uint)newStream.Length;
        }

        internal void ReadAllBlobs()
        {
            mainStream.Seek(1, SeekOrigin.Begin);
            while (mainStream.Position < mainStream.Length)
            {
                bool alreadyExisted = readBlobs.ContainsKey((uint)mainStream.Position);
                byte[] value = GetBlob((uint)mainStream.Position);

                int length = value.Length;
                if (length == 0)
                    break;
                if (alreadyExisted)
                    mainStream.Seek(length + NETGlobals.GetCompressedUInt32Size((uint)length), SeekOrigin.Current);

            }
        }

        public override void Dispose()
        {
            ClearCache();
            base.Dispose();
        }

        public override void ClearCache()
        {
            readBlobs.Clear();
        }

        /// <summary>
        /// Gets the blob value by it's signature/index.
        /// </summary>
        /// <param name="index">The index or signature to get the blob value from.</param>
        /// <returns></returns>
        public byte[] GetBlob(uint index)
        {
            byte[] bytes = null;
            if (readBlobs.TryGetValue(index, out bytes))
                return bytes;

            mainStream.Seek(index, SeekOrigin.Begin);
            int length = (int)NETGlobals.ReadCompressedUInt32(binReader);

            bytes = binReader.ReadBytes(length);
            readBlobs.Add(index, bytes);
            return bytes;
            
        }

        /// <summary>
        /// Gets the blob value by it's signature/index and creates a binary reader.
        /// </summary>
        /// <param name="index">The index or signature to get the blob value from.</param>
        /// <returns></returns>
        public BlobSignatureReader GetBlobReader(uint index)
        {
            byte[] bytes = GetBlob(index);
            MemoryStream newStream = new MemoryStream(bytes);
            newStream.Seek(0, SeekOrigin.Begin);
            BlobSignatureReader reader = new BlobSignatureReader(newStream);
            return reader;
        }

        /// <summary>
        /// Gets the blob value by it's signature/index and creates a binary reader using a generic instance.
        /// </summary>
        /// <param name="index">The index or signature to get the blob value from.</param>
        /// <param name="instance">The generic instance that is being used as a context.</param>
        /// <returns></returns>
        public BlobSignatureReader GetBlobReader(uint index, IGenericContext instance)
        {
            BlobSignatureReader reader = GetBlobReader(index);
            reader.GenericContext = instance;
            return reader;
        }

        public uint GetBlobIndex(byte[] blobValue)
        {
            ReadAllBlobs();

            if (readBlobs.ContainsValue(blobValue))
                return readBlobs.FirstOrDefault(b => b.Value == blobValue).Key;

            mainStream.Seek(0, SeekOrigin.End);
            uint index = (uint)mainStream.Position;
            NETGlobals.WriteCompressedUInt32(binWriter, (uint)blobValue.Length);
            binWriter.Write(blobValue);
            readBlobs.Add(index, blobValue);
            return index;
        }

        public IMemberSignature ReadMemberRefSignature(uint sig, IGenericContext context)
        {
            IMemberSignature signature = null;
            using (BlobSignatureReader reader = GetBlobReader(sig, context))
            {
                byte flag = reader.ReadByte();

                if (flag == 0x6)
                {
                    FieldSignature fieldsignature = new FieldSignature();
                    fieldsignature.ReturnType = ReadTypeReference(reader, (ElementType)reader.ReadByte());
                    signature = fieldsignature;
                }
                else
                {
                    MethodSignature methodsignature = new MethodSignature();
                    
                    if ((flag & 0x20) != 0)
                    {
                        methodsignature.HasThis = true;
                        flag = (byte)(flag & -33);
                    }
                    if ((flag & 0x40) != 0)
                    {
                        methodsignature.ExplicitThis = true;
                        flag = (byte)(flag & -65);
                    }
                    if ((flag & 0x10) != 0)
                    {
                        uint genericsig = NETGlobals.ReadCompressedUInt32(reader);
                    }
                    methodsignature.CallingConvention = (MethodCallingConvention)flag;

                    uint num3 = NETGlobals.ReadCompressedUInt32(reader);
                    ElementType type = (ElementType)reader.ReadByte();

                    methodsignature.ReturnType = ReadTypeReference(reader, type);

                    if (num3 != 0)
                    {
                        ParameterReference[] parameters = new ParameterReference[num3];
                        for (int i = 0; i < num3; i++)
                        {
                            parameters[i] = new ParameterReference() { ParameterType = ReadTypeReference(reader, (ElementType)reader.ReadByte())};
                        }
                        methodsignature.Parameters = parameters;
                    }

                    signature = methodsignature;

                }
            }
            return signature;
        }
        
        public PropertySignature ReadPropertySignature(uint signature, PropertyDefinition parentProperty)
        {
            PropertySignature propertySig = null;
            using (BlobSignatureReader reader = GetBlobReader(signature))
            {
                reader.GenericContext = parentProperty.DeclaringType;

                byte flag = reader.ReadByte();

                if ((flag & 8) == 0)
                    throw new ArgumentException("Signature doesn't refer to a valid property signature.");

                propertySig = new PropertySignature();
                propertySig.HasThis = (flag & 0x20) != 0;
                NETGlobals.ReadCompressedUInt32(reader);
                propertySig.ReturnType = ReadTypeReference(reader, (ElementType)reader.ReadByte());
            }
            return propertySig;
        }
        
        public VariableDefinition[] ReadVariableSignature(uint signature, MethodDefinition parentMethod)
        {
            VariableDefinition[] variables = null;
            using (BlobSignatureReader reader = GetBlobReader(signature))
            {
                reader.GenericContext = parentMethod;

                byte local_sig = reader.ReadByte();

                if (local_sig != 0x7)
                    throw new ArgumentException("Signature doesn't refer to a valid local variable signature");

                uint count = NETGlobals.ReadCompressedUInt32(reader);

                if (count == 0)
                    return null;

                variables = new VariableDefinition[count];

                for (int i = 0; i < count; i++)
                    variables[i] = new VariableDefinition(i, ReadTypeReference(reader, (ElementType)reader.ReadByte()));
            }
            return variables;
        }
              
        public TypeReference ReadTypeSignature(uint signature, IGenericContext paramProvider)
        {
            TypeReference typeRef = null;
            using (BlobSignatureReader reader = GetBlobReader(signature))
            {
                reader.GenericContext = paramProvider;
                typeRef = ReadTypeReference(reader, (ElementType)NETGlobals.ReadCompressedUInt32(reader));
            }

            return typeRef;
        }
        
        public TypeReference[] ReadGenericArgumentsSignature(uint signature, IGenericContext context)
        {
            using (BlobSignatureReader reader = GetBlobReader(signature, context))
            {
                if (reader.ReadByte() == 0xa)
                {
                    uint count = NETGlobals.ReadCompressedUInt32(reader);
                    TypeReference[] types = new TypeReference[count];
                    for (int i = 0; i < count; i++)
                        types[i] = ReadTypeReference(reader, (ElementType)reader.ReadByte());

                    return types;
                }
            }
            throw new ArgumentException("Signature doesn't point to a valid generic arguments signature");
        }
        
        public object ReadConstantValue(ElementType type, uint signature)
        {
            object value = null;
            using (BlobSignatureReader reader = GetBlobReader(signature))
            {

                switch (type)
                {
                    case ElementType.Boolean:
                        value = reader.ReadByte() == 1;
                        break;
                    case ElementType.Char:
                        value = (char)reader.ReadUInt16();
                        break;
                    case ElementType.String:
                        value = Encoding.Unicode.GetString(reader.ReadBytes((int)reader.BaseStream.Length));
                        break;
                    case ElementType.I1:
                        value =  reader.ReadSByte();
                        break;
                    case ElementType.I2:
                        value =  reader.ReadInt16();
                        break;
                    case ElementType.I4:
                        value =  reader.ReadInt32();
                        break;
                    case ElementType.I8:
                        value =  reader.ReadInt64();
                        break;
                    case ElementType.U1:
                        value =  reader.ReadByte();
                        break;
                    case ElementType.U2:
                        value =  reader.ReadUInt16();
                        break;
                    case ElementType.U4:
                        value =  reader.ReadUInt32();
                        break;
                    case ElementType.U8:
                        value =  reader.ReadUInt64();
                        break;
                    case ElementType.R4:
                        value =  reader.ReadSingle();
                        break;
                    case ElementType.R8:
                        value =  reader.ReadDouble();
                        break;
                    default:
                        throw new ArgumentException("Invalid constant type", "type");
                }
            }
            return value;
        }
       
        public CustomAttributeSignature ReadCustomAttributeSignature(CustomAttribute parent, uint signature)
        {
            CustomAttributeSignature customAttrSig = null;
            using (BlobSignatureReader reader = GetBlobReader(signature))
            {
                ushort sign = reader.ReadUInt16();
                if (sign != 0x0001)
                    throw new ArgumentException("Signature doesn't refer to a valid Custom Attribute signature");


                int fixedArgCount = 0;



                if (parent.Constructor.Signature != null && parent.Constructor.Signature.Parameters != null)
                    fixedArgCount = parent.Constructor.Signature.Parameters.Length;

                CustomAttributeArgument[] fixedArgs = new CustomAttributeArgument[fixedArgCount];


                for (int i = 0; i < fixedArgCount; i++)
                {
                    fixedArgs[i] = new CustomAttributeArgument(ReadArgumentValue(reader,parent.Constructor.Signature.Parameters[i].ParameterType));

                }

                int namedArgCount = 0;
                CustomAttributeArgument[] namedArgs = new CustomAttributeArgument[namedArgCount];

                customAttrSig = new CustomAttributeSignature(fixedArgs, namedArgs);
            }
            return customAttrSig;
        }

        private TypeReference ReadTypeReference(BlobSignatureReader reader, ElementType type)
        {
            switch (type)
            {
                case ElementType.Void:
                    return netheader.TypeSystem.Void;
                case ElementType.I:
                    return netheader.TypeSystem.IntPtr;
                case ElementType.I1:
                    return netheader.TypeSystem.Int8;
                case ElementType.I2:
                    return netheader.TypeSystem.Int16;
                case ElementType.I4:
                    return netheader.TypeSystem.Int32;
                case ElementType.I8:
                    return netheader.TypeSystem.Int64;
                case ElementType.U:
                    return netheader.TypeSystem.UIntPtr;
                case ElementType.U1:
                    return netheader.TypeSystem.UInt8;
                case ElementType.U2:
                    return netheader.TypeSystem.UInt16;
                case ElementType.U4:
                    return netheader.TypeSystem.UInt32;
                case ElementType.U8:
                    return netheader.TypeSystem.UInt64;
                case ElementType.Object:
                    return netheader.TypeSystem.Object;
                case ElementType.R4:
                    return netheader.TypeSystem.Single;
                case ElementType.R8:
                    return netheader.TypeSystem.Double;
                case ElementType.String:
                    return netheader.TypeSystem.String;
                case ElementType.Char:
                    return netheader.TypeSystem.Char;
                case ElementType.Type:
                    return netheader.TypeSystem.Type;
                case ElementType.Boolean:
                    return netheader.TypeSystem.Boolean;
                case ElementType.Ptr:
                    return new PointerType(ReadTypeReference(reader,(ElementType)reader.ReadByte()));
                case ElementType.MVar:

                    if(reader.GenericContext == null)
                        return new GenericParamReference(NETGlobals.ReadCompressedInt32(reader), new TypeReference(string.Empty, "MVar",null) { elementType = ElementType.MVar, @namespace = "", netheader = this.netheader });

                    return ReadGenericType(reader);

                case ElementType.Var:
                    uint token = NETGlobals.ReadCompressedUInt32(reader);
                    if (reader.GenericContext != null)
                    {
                        if (reader.GenericContext.DeclaringType != null && reader.GenericContext.DeclaringType.GenericParameters != null && reader.GenericContext.DeclaringType.GenericParameters.Length > token)
                            return reader.GenericContext.DeclaringType.GenericParameters[token];
                        else if (reader.GenericContext.GenericParameters != null && reader.GenericContext.GenericParameters.Length > token)
                            return reader.GenericContext.GenericParameters[token];
                    }
                    return new GenericParamReference((int)token, new TypeReference(string.Empty, "Var", null) { elementType = ElementType.Var, @namespace = "", netheader = this.netheader });

                case ElementType.Array:
                    return ReadArrayType(reader);
                case ElementType.SzArray:
                    return new ArrayType(ReadTypeReference(reader,(ElementType)reader.ReadByte()));
                case ElementType.Class:
                    return (TypeReference)netheader.TablesHeap.TypeDefOrRef.GetMember((int)NETGlobals.ReadCompressedUInt32(reader));
                case ElementType.ValueType:
                    TypeReference typeRef = (TypeReference)netheader.TablesHeap.TypeDefOrRef.GetMember((int)NETGlobals.ReadCompressedUInt32(reader));
                    typeRef.IsValueType = true;
                    return typeRef;
                case ElementType.ByRef:
                    return new ByReferenceType(ReadTypeReference(reader, (ElementType)reader.ReadByte()));
                case ElementType.Pinned:
                    return new PinnedType(ReadTypeReference(reader, (ElementType)reader.ReadByte()));
                case ElementType.GenericInst:
                    bool flag = reader.ReadByte() == 0x11;
                    TypeReference reference2 = ReadTypeToken(reader);
                    GenericInstanceType instance = new GenericInstanceType(reference2);
                    this.ReadGenericInstanceSignature(reader, instance);
                    if (flag)
                    {
                        instance.IsValueType = true;
                        
                    }
                    return instance;
            }
            return new TypeReference(string.Empty, type.ToString(), null) { netheader = this.netheader };

        }

        private void ReadGenericInstanceSignature(BlobSignatureReader reader, GenericInstanceType genericType)
        {
            uint number = NETGlobals.ReadCompressedUInt32(reader);
            
            genericType.genericArguments = new TypeReference[number];

            for (int i = 0; i < number; i++)
                genericType.genericArguments[i] = ReadTypeReference(reader, (ElementType)reader.ReadByte());

        }

        private TypeReference ReadTypeToken(BlobSignatureReader reader)
        {
            TypeReference typeRef = netheader.TablesHeap.TypeDefOrRef.GetMember((int)NETGlobals.ReadCompressedUInt32(reader)) as TypeReference;
            if (typeRef is ISpecification)
                typeRef = (typeRef as TypeSpecification).TransformWith(reader.GenericContext) as TypeReference;
            return typeRef;
        }

        private ArrayType ReadArrayType(BlobSignatureReader reader)
        {
            TypeReference arrayType = ReadTypeReference(reader, (ElementType)reader.ReadByte());
            uint rank = NETGlobals.ReadCompressedUInt32(reader);
            uint[] upperbounds = new uint[NETGlobals.ReadCompressedUInt32(reader)];

            for (int i = 0; i < upperbounds.Length; i++)
                upperbounds[i] = NETGlobals.ReadCompressedUInt32(reader);

            int[] lowerbounds = new int[NETGlobals.ReadCompressedUInt32(reader)];

            for (int i = 0; i < lowerbounds.Length; i++)
                lowerbounds[i] = NETGlobals.ReadCompressedInt32(reader);


            ArrayDimension[] dimensions = new ArrayDimension[rank];

            for (int i = 0; i < rank; i++)
            {
                int? lower = null;
                int? upper = null;

                if (i < lowerbounds.Length)
                    lower = new int?(lowerbounds[i]);

                if (i < upperbounds.Length)
                {
                    int x = (int)upperbounds[i];
                    upper = (lower.HasValue ? new int?(lower.GetValueOrDefault() + x) : 0) - 1;
                }
                ArrayDimension dimension = new ArrayDimension(lower,upper);
                dimensions[i] = dimension;

            }


         
            return new ArrayType(arrayType, (int)rank, dimensions);

        }

        private TypeReference ReadGenericType(BlobSignatureReader reader)
        {
            // not finished yet!

            uint token = NETGlobals.ReadCompressedUInt32(reader);
            object genericType;

            if (reader.GenericContext.IsDefinition)
            {
                if (TryGetArrayValue(reader.GenericContext.GenericParameters, token, out genericType))
                    return genericType as TypeReference;
            }

            if (TryGetArrayValue(reader.GenericContext.GenericArguments, token, out genericType))
                return genericType as TypeReference;

            return new TypeReference(string.Empty, token.ToString(), null);

        }

        private object ReadArgumentValue(BlobSignatureReader reader, TypeReference paramType)
        {
            if (!paramType.IsArray || !(paramType as ArrayType).IsVector)
                return ReadElement(reader,paramType);
            
           // throw new NotImplementedException("Array constructor values are not supported yet.");

            ushort elementcount = reader.ReadUInt16();
            object[] elements = new object[elementcount];
            for (int i = 0; i < elementcount; i++)
                elements[i] = ReadElement(reader,(paramType as ArrayType).OriginalType);

            return elements;
        }

        private object ReadElement(BlobSignatureReader reader, TypeReference paramType)
        {
            switch (paramType.elementType)
            {
                case ElementType.I1:
                    return reader.ReadSByte();
                case ElementType.I2:
                    return reader.ReadInt16();
                case ElementType.I4:
                    return reader.ReadInt32();
                case ElementType.I8:
                    return reader.ReadInt64();
                case ElementType.U1:
                    return reader.ReadByte();
                case ElementType.U2:
                    return reader.ReadInt16();
                case ElementType.U4:
                    return reader.ReadInt32();
                case ElementType.U8:
                    return reader.ReadInt64();
                case ElementType.R4:
                    return reader.ReadSingle();
                case ElementType.R8:
                    return reader.ReadDouble();
                case ElementType.Type:
                case ElementType.String:
                    uint size = NETGlobals.ReadCompressedUInt32(reader);
                    if (size == 0xFF)
                        return string.Empty;
                    byte[] rawdata = reader.ReadBytes((int)size);
                    return Encoding.UTF8.GetString(rawdata);
                case ElementType.Char:
                    return reader.ReadChar();
                    throw new NotSupportedException();
                case ElementType.Boolean:
                    return reader.ReadByte() == 1;
                

            }
            return null;
        }

        private bool TryGetArrayValue(Array array, uint index, out object value)
        {
            value = null;
            if (array == null || array.Length < index || index < 0)
                return false;
            value = array.GetValue(index);
            return true;
        }
    }
}
