// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Serialization
{
    using Azure.IIoT.OpcUa.Core.Exceptions;
    using System;
    using System.Buffers;
    using System.IO;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Additional coverage tests for <see cref="Json"/> — reflection fallback
    /// and serialization/deserialization exception wrapping.
    /// </summary>
    public sealed class JsonMoreTests
    {
        // ── Reflection-fallback path (OptionsFor catch NotSupportedException) ─

        [Fact]
        public void Deserialize_UnregisteredType_ReturnsDeserializedObject()
        {
            // UnregisteredType is not in any source-generated context, so
            // OptionsFor creates a reflection resolver on the fly.
            var result = Json.Deserialize<UnregisteredJsonType>("""{"value":"hello"}""");

            Assert.NotNull(result);
            Assert.Equal("hello", result!.Value);
        }

        [Fact]
        public void SerializeToString_UnregisteredType_ReturnsJsonString()
        {
            var result = Json.SerializeToString(new UnregisteredJsonType { Value = "world" });

            Assert.Contains("world", result);
        }

        [Fact]
        public void SerializeToMemory_UnregisteredType_ReturnsJsonBytes()
        {
            var bytes = Json.SerializeToMemory(new UnregisteredJsonType { Value = "mem" });
            var text = Encoding.UTF8.GetString(bytes.Span);

            Assert.Contains("mem", text);
        }

        [Fact]
        public void SerializeObjectToString_UnregisteredType_ReturnsJsonString()
        {
            var result = Json.SerializeObjectToString(
                new UnregisteredJsonType { Value = "obj" }, typeof(UnregisteredJsonType));

            Assert.Contains("obj", result);
        }

        [Fact]
        public void SerializeObjectToMemory_UnregisteredType_ReturnsJsonBytes()
        {
            var bytes = Json.SerializeObjectToMemory(
                new UnregisteredJsonType { Value = "objmem" }, typeof(UnregisteredJsonType));
            var text = Encoding.UTF8.GetString(bytes.Span);

            Assert.Contains("objmem", text);
        }

        [Fact]
        public void SerializeObject_Buffer_UnregisteredType_WritesJson()
        {
            var buffer = new ArrayBufferWriter<byte>();
            Json.SerializeObject(buffer, new UnregisteredJsonType { Value = "buf" },
                typeof(UnregisteredJsonType));
            var text = Encoding.UTF8.GetString(buffer.WrittenSpan);

            Assert.Contains("buf", text);
        }

        [Fact]
        public void FromObject_UnregisteredType_ReturnsJsonNode()
        {
            var node = Json.FromObject(new UnregisteredJsonType { Value = "node" });

            Assert.NotNull(node);
        }

        [Fact]
        public void Deserialize_RuntimeType_UnregisteredType_ReturnsObject()
        {
            var result = Json.Deserialize("""{"value":"rt"}""", typeof(UnregisteredJsonType));

            Assert.IsType<UnregisteredJsonType>(result);
            Assert.Equal("rt", ((UnregisteredJsonType)result!).Value);
        }

        [Fact]
        public void Deserialize_ReadOnlyMemory_RuntimeType_UnregisteredType_ReturnsObject()
        {
            var bytes = Encoding.UTF8.GetBytes("""{"value":"rom"}""");
            ReadOnlyMemory<byte> memory = bytes;

            var result = Json.Deserialize(memory, typeof(UnregisteredJsonType));

            Assert.IsType<UnregisteredJsonType>(result);
            Assert.Equal("rom", ((UnregisteredJsonType)result!).Value);
        }

        // ── Exception wrapping — deserialization ─────────────────────────────

        [Fact]
        public void Deserialize_ReadOnlySequence_BadJson_ThrowsSerializerException()
        {
            var bytes = Encoding.UTF8.GetBytes("{");
            var seq = new ReadOnlySequence<byte>(bytes);

            Assert.Throws<SerializerException>(() =>
                Json.Deserialize<UnregisteredJsonType>(seq));
        }

        [Fact]
        public void Deserialize_RuntimeType_BadJson_ThrowsSerializerException()
        {
            Assert.Throws<SerializerException>(() =>
                Json.Deserialize("{", typeof(UnregisteredJsonType)));
        }

        [Fact]
        public void Deserialize_ReadOnlyMemory_RuntimeType_BadJson_ThrowsSerializerException()
        {
            var bytes = Encoding.UTF8.GetBytes("{");
            Assert.Throws<SerializerException>(() =>
                Json.Deserialize(new ReadOnlyMemory<byte>(bytes), typeof(UnregisteredJsonType)));
        }

        [Fact]
        public async Task DeserializeAsync_BadJsonStream_ThrowsSerializerExceptionAsync()
        {
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("{bad"));

            await Assert.ThrowsAsync<SerializerException>(async () =>
                await Json.DeserializeAsync<UnregisteredJsonType>(stream));
        }

        // ── Exception wrapping — serialization (circular reference) ──────────

        [Fact]
        public void SerializeToString_CircularReference_ThrowsSerializerException()
        {
            var obj = new CircularJsonType();
            obj.Self = obj;

            Assert.Throws<SerializerException>(() => Json.SerializeToString(obj));
        }

        [Fact]
        public void SerializeObjectToString_CircularReference_ThrowsSerializerException()
        {
            var obj = new CircularJsonType();
            obj.Self = obj;

            Assert.Throws<SerializerException>(() =>
                Json.SerializeObjectToString(obj, typeof(CircularJsonType)));
        }

        [Fact]
        public void SerializeToMemory_CircularReference_ThrowsSerializerException()
        {
            var obj = new CircularJsonType();
            obj.Self = obj;

            Assert.Throws<SerializerException>(() => Json.SerializeToMemory(obj));
        }

        [Fact]
        public void SerializeObjectToMemory_CircularReference_ThrowsSerializerException()
        {
            var obj = new CircularJsonType();
            obj.Self = obj;

            Assert.Throws<SerializerException>(() =>
                Json.SerializeObjectToMemory(obj, typeof(CircularJsonType)));
        }

        [Fact]
        public void SerializeObject_Buffer_CircularReference_ThrowsSerializerException()
        {
            var buffer = new ArrayBufferWriter<byte>();
            var obj = new CircularJsonType();
            obj.Self = obj;

            Assert.Throws<SerializerException>(() =>
                Json.SerializeObject(buffer, obj, typeof(CircularJsonType)));
        }

        [Fact]
        public void FromObject_CircularReference_ThrowsSerializerException()
        {
            var obj = new CircularJsonType();
            obj.Self = obj;

            Assert.Throws<SerializerException>(() => Json.FromObject(obj));
        }
    }

    internal sealed class UnregisteredJsonType
    {
        public string? Value { get; set; }
    }

    internal sealed class CircularJsonType
    {
        public CircularJsonType? Self { get; set; }
    }
}
