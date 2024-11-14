// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Avro;
    using Furly;
    using Furly.Extensions.Logging;
    using Furly.Extensions.Messaging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Test avro file writer
    /// </summary>
    public sealed class AvroFileWriterTests
    {
        [Fact]
        public void SupportsContentTypeTests()
        {
            using var writer = new AvroFileWriter(Options.Create(new AvroFileWriterOptions()),
                Log.Console<AvroFileWriter>());
            Assert.True(writer.SupportsContentType(ContentType.Avro));
            Assert.True(writer.SupportsContentType(ContentType.AvroGzip));
            Assert.False(writer.SupportsContentType(ContentType.Uadp));
            Assert.False(writer.SupportsContentType(ContentMimeType.Binary));
        }
        [Fact]
        public void SupportsNothingWhenDisabledTests()
        {
            using var writer = new AvroFileWriter(Options.Create(
                new AvroFileWriterOptions { Disabled = true }),
                Log.Console<AvroFileWriter>());
            Assert.False(writer.SupportsContentType(ContentType.Avro));
            Assert.False(writer.SupportsContentType(ContentType.AvroGzip));
            Assert.False(writer.SupportsContentType(ContentType.Uadp));
        }

        [Fact]
        public async Task TestAvroFileWriter1Async()
        {
            var file = Path.GetTempFileName();
            try
            {
                using (var writer = new AvroFileWriter(Options.Create(new AvroFileWriterOptions()),
                    Log.Console<AvroFileWriter>()))
                {
                    await writer.WriteAsync(file, DateTime.UtcNow, new[]
                    {
                        new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 })
                    }, null, new DummyEventSchema(), ContentType.Avro);
                }

                Assert.True(File.Exists(file + ".avro"));
                using var reader = new AvroFileReader(file);
                Assert.True(reader.HasMore());
                var result = reader.Read((s, t) =>
                {
                    var buffer = t.ReadAsBuffer();
                    Assert.Equal(1, buffer[0]);
                    Assert.Equal(2, buffer[1]);
                    Assert.Equal(3, buffer[2]);
                    Assert.Equal(3, buffer.Count);
                    Assert.True(s is UnionSchema);
                    return 123;
                });
                Assert.False(reader.HasMore());
                Assert.Equal(123, result);
            }
            finally
            {
                File.Delete(file + ".avro");
            }
        }

        [Fact]
        public async Task TestAvroFileWriter2Async()
        {
            var file = Path.GetTempFileName();
            try
            {
                using (var writer = new AvroFileWriter(Options.Create(new AvroFileWriterOptions()),
                    Log.Console<AvroFileWriter>()))
                {
                    await writer.WriteAsync(file, DateTime.UtcNow, new[]
                    {
                        new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 }),
                        new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 }),
                        new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 })
                    }, null, new DummyEventSchema(), ContentType.Avro);
                }

                using var reader = new AvroFileReader(file);
                for (var i = 0; i < 3; i++)
                {
                    Assert.True(reader.HasMore());
                    var result = reader.Read((s, t) =>
                    {
                        Assert.Equal(1, t.ReadByte());
                        Assert.Equal(2, t.ReadByte());
                        Assert.Equal(3, t.ReadByte());
                        Assert.True(s is UnionSchema);
                        return 123;
                    });
                    Assert.Equal(123, result);
                }
                Assert.False(reader.HasMore());
            }
            finally
            {
                File.Delete(file + ".avro");
            }
        }

        [Fact]
        public async Task TestAvroFileWriter3Async()
        {
            var file = Path.GetTempFileName();
            try
            {
                using (var writer = new AvroFileWriter(Options.Create(new AvroFileWriterOptions()),
                    Log.Console<AvroFileWriter>()))
                {
                    await writer.WriteAsync(file, DateTime.UtcNow, Array.Empty<ReadOnlySequence<byte>>(),
                        null, new DummyEventSchema(), ContentType.Avro);
                }

                Assert.True(File.Exists(file + ".avro"));
                using var reader = new AvroFileReader(file);
                Assert.False(reader.HasMore());
                Assert.Throws<EndOfStreamException>(() => reader.Read((s, t) => 123));
            }
            finally
            {
                File.Delete(file + ".avro");
            }
        }

        [Fact]
        public async Task TestAvroFileWriter4Async()
        {
            var file = Path.GetTempFileName();
            try
            {
                const string expected = "teststring";
                var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(expected));
                using (var writer = new AvroFileWriter(Options.Create(new AvroFileWriterOptions()),
                    Log.Console<AvroFileWriter>()))
                {
                    await writer.WriteAsync(file, DateTime.UtcNow, new[]
                    {
                        buffer.GzipCompress()
                    }, new Dictionary<string, string>
                    {
                        ["testmeta"] = "mtest"
                    }, new DummyEventSchema(), ContentType.AvroGzip);
                }

                Assert.True(File.Exists(file + ".avro"));
                using var reader = new AvroFileReader(file);
                Assert.True(reader.HasMore());
                var result = reader.Read((s, t) =>
                {
                    var buffer = t.ReadAsBuffer();
                    Assert.Equal(expected, Encoding.UTF8.GetString(buffer));
                    return 123;
                });
                Assert.False(reader.HasMore());
                Assert.Equal(123, result);
            }
            finally
            {
                File.Delete(file + ".avro");
            }
        }

        [Fact]
        public async Task TestAvroFileWriter5Async()
        {
            var file = Path.GetTempFileName();
            try
            {
                const string expected = "teststring";
                var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(expected));
                using (var writer = new AvroFileWriter(Options.Create(new AvroFileWriterOptions()),
                    Log.Console<AvroFileWriter>()))
                {
                    await writer.WriteAsync(file, DateTime.UtcNow, new[]
                    {
                        buffer.GzipCompress(),
                        buffer.GzipCompress(),
                        buffer.GzipCompress()
                    }, null, new DummyEventSchema(), ContentType.AvroGzip);
                }

                Assert.True(File.Exists(file + ".avro"));
                using var reader = new AvroFileReader(file);
                Assert.True(reader.HasMore());
                var result = reader.Read((s, t) =>
                {
                    var buffer = t.ReadAsBuffer();
                    Assert.Equal(expected + expected + expected, Encoding.UTF8.GetString(buffer));
                    return 123;
                });
                Assert.Equal(123, result);
                Assert.True(reader.HasMore());
            }
            finally
            {
                File.Delete(file + ".avro");
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("ab")]
        [InlineData("badheader")]
        public async Task TestBadHeader1Async(string data)
        {
            var file = Path.GetTempFileName();
            try
            {
                await File.WriteAllTextAsync(file + ".avro", data);
                Assert.Throws<FormatException>(() => new AvroFileReader(file));
            }
            finally
            {
                File.Delete(file + ".avro");
            }
        }

        private sealed class DummyEventSchema : IEventSchema
        {
            public string Id => "test";
            public string Schema => "[]";
            public string Type => "test";
            public string Name => "test";
            public ulong Version => 1;
        }
    }
}
