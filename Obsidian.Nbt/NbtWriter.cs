﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace Obsidian.Nbt
{
    public sealed partial class NbtWriter : IDisposable, IAsyncDisposable
    {
        private NbtTagType? expectedListType;

        private Stack<NbtTagType> nodes = new();

        private int listSize;
        private int listIndex;

        public NbtTagType RootType { get; private set; }

        public bool IsClosed { get; internal set; }

        public Stream BaseStream { get; }

        public NbtWriter(Stream outstream, string name)
        {
            this.BaseStream = outstream;
            this.RootType = NbtTagType.Compound;

            this.Write(NbtTagType.Compound);
            this.WriteString(name);

            this.AddTag(NbtTagType.Compound);
        }

        public NbtWriter(Stream outstream, CompressionMode compressionMode, string name)
        {
            switch (compressionMode)
            {
                case CompressionMode.Compress:
                    this.BaseStream = new GZipStream(outstream, compressionMode);
                    break;
                default:
                    throw new NotSupportedException();
            }

            this.RootType = NbtTagType.Compound;

            this.Write(NbtTagType.Compound);
            this.WriteString(name);

            this.AddTag(NbtTagType.Compound);
        }

        private void AddTag(NbtTagType type) => this.nodes.Push(type);

        public void WriteCompoundStart(string name = "")
        {
            this.Validate(name, NbtTagType.Compound);

            this.AddTag(NbtTagType.Compound);

            this.RootType = NbtTagType.Compound;

            this.Write(NbtTagType.Compound);
            this.WriteString(name);
        }

        public void WriteListStart(string name, NbtTagType listType, int length)
        {
            this.Validate(name, NbtTagType.List);

            this.AddTag(NbtTagType.List);

            this.RootType = NbtTagType.List;

            this.listSize = length;
            this.expectedListType = listType;

            this.Write(NbtTagType.List);
            this.WriteString(name);
            this.Write(listType);
            this.WriteInt(length);
        }

        public void EndList()
        {
            if (this.listIndex < this.listSize)
                throw new InvalidOperationException("List cannot end because its size is smaller than the pre-defined size.");

            var tag = this.nodes.Pop();

            if (tag != NbtTagType.List)
                throw new InvalidOperationException();

            this.listSize = 0;
            this.listIndex = 0;
            this.expectedListType = null;
        }

        public void EndCompound()
        {
            var tag = this.nodes.Pop();

            if (tag != NbtTagType.Compound)
                throw new InvalidOperationException();

            this.Write(NbtTagType.End);
        }

        public void WriteTag(INbtTag tag)
        {
            var name = tag.Name;

            switch (tag.Type)
            {
                case NbtTagType.End:
                    throw new InvalidOperationException("Use writer.EndCompound() instead.");
                case NbtTagType.Byte:
                    if (tag is NbtTag<byte> byteTag)
                    {
                        this.WriteByte(name, byteTag.Value);
                    }
                    else if (tag is NbtTag<bool> boolValue)
                    {
                        this.WriteByte(name, (byte)(boolValue.Value ? 1 : 0));
                    }
                    break;
                case NbtTagType.Short:
                    this.WriteShort(name, ((NbtTag<short>)tag).Value);
                    break;
                case NbtTagType.Int:
                    this.WriteInt(name, ((NbtTag<int>)tag).Value);
                    break;
                case NbtTagType.Long:
                    this.WriteLong(name, ((NbtTag<long>)tag).Value);
                    break;
                case NbtTagType.Float:
                    this.WriteFloat(name, ((NbtTag<float>)tag).Value);
                    break;
                case NbtTagType.Double:
                    this.WriteDouble(name, ((NbtTag<double>)tag).Value);
                    break;
                case NbtTagType.String:
                    this.WriteString(name, ((NbtTag<string>)tag).Value);
                    break;
                case NbtTagType.List:
                    var list = (NbtList)tag;

                    this.WriteListStart(name, list.ListType, list.Count);

                    foreach (var child in list)
                    {
                        this.WriteTag(child);
                    }

                    this.EndList();
                    break;
                case NbtTagType.Compound:
                    this.WriteCompoundStart(name);

                    foreach (var (_, child) in (NbtCompound)tag)
                    {
                        this.WriteTag(child);
                    }

                    this.EndCompound();
                    break;
                case NbtTagType.ByteArray:
                case NbtTagType.IntArray:
                case NbtTagType.LongArray:
                    this.WriteArray(tag);
                    break;
                case NbtTagType.Unknown:
                default:
                    throw new InvalidOperationException("Unknown tag type");
            }
        }

        public void WriteArray(INbtTag array)
        {
            this.Validate(array.Name, array.Type);

            if (array is NbtArray<int> intArray)
            {
                this.Write(NbtTagType.IntArray);
                this.WriteString(array.Name);
                this.WriteInt(intArray.Count);

                for (int i = 0; i < intArray.Count; i++)
                    this.WriteInt(intArray[i]);
            }
            else if (array is NbtArray<long> longArray)
            {
                this.Write(NbtTagType.LongArray);
                this.WriteString(array.Name);
                this.WriteInt(longArray.Count);

                for (int i = 0; i < longArray.Count; i++)
                    this.WriteLong(longArray[i]);
            }
            else if (array is NbtArray<byte> byteArray)
            {
                this.Write(NbtTagType.ByteArray);
                this.WriteString(array.Name);
                this.WriteInt(byteArray.Count);

                for (int i = 0; i < byteArray.Count; i++)
                    this.BaseStream.Write(byteArray.GetArray());
            }
        }

        public void WriteString(string name, string value)
        {
            this.Validate(name, NbtTagType.String);

            this.Write(NbtTagType.String);
            this.WriteString(name);
            this.WriteString(value);
        }

        public void WriteByte(string name, byte value)
        {
            this.Validate(name, NbtTagType.Byte);

            this.Write(NbtTagType.Byte);
            this.WriteString(name);
            this.WriteByte(value);
        }

        public void WriteBool(string name, bool value)
        {
            this.Validate(name, NbtTagType.Byte);

            this.Write(NbtTagType.Byte);
            this.WriteString(name);
            this.WriteByte((byte)(value ? 1 : 0));
        }

        public void WriteShort(string name, short value)
        {
            this.Validate(name, NbtTagType.Short);

            this.Write(NbtTagType.Short);
            this.WriteString(name);
            this.WriteShort(value);
        }

        public void WriteInt(string name, int value)
        {
            this.Validate(name, NbtTagType.Int);

            this.Write(NbtTagType.Int);
            this.WriteString(name);
            this.WriteInt(value);
        }

        public void WriteLong(string name, long value)
        {
            this.Validate(name, NbtTagType.Long);

            this.Write(NbtTagType.Long);
            this.WriteString(name);
            this.WriteLong(value);
        }

        public void WriteFloat(string name, float value)
        {
            this.Validate(name, NbtTagType.Float);

            this.Write(NbtTagType.Float);
            this.WriteString(name);
            this.WriteFloat(value);
        }

        public void WriteDouble(string name, double value)
        {
            this.Validate(name, NbtTagType.Double);

            this.Write(NbtTagType.Double);
            this.WriteString(name);
            this.WriteDouble(value);
        }

        public void Validate(string name, NbtTagType type)
        {
            if (this.IsClosed)
                throw new InvalidOperationException("Cannot write any more tags. Writer has been closed.");

            var parent = this.nodes.Peek();

            if (string.IsNullOrEmpty(name) && parent == NbtTagType.Compound)
                throw new ArgumentException($"Tags inside a compound tag must have a name. Tag({type}:{name})");

            if (parent == NbtTagType.List)
            {
                if (this.expectedListType != type)
                    throw new InvalidOperationException($"Expected list type: {this.expectedListType}. Got: {type}");
                else if (!string.IsNullOrEmpty(name))
                    throw new InvalidOperationException("Tags inside lists must be nameless.");
                else if (this.listIndex > this.listSize)
                    throw new IndexOutOfRangeException("Exceeded pre-defined list size");

                this.listIndex++;
            }
        }

        //TODO
        public void TryFinish() => this.BaseStream.Flush();

        public ValueTask DisposeAsync() => this.BaseStream.DisposeAsync();
        public void Dispose() => this.BaseStream.Dispose();

        private record TagNode(NbtTagType Type);
    }
}
