// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    using Furly;
    using Furly.Extensions.Messaging;
    using System;
    using System.Buffers;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    /// <summary>
    /// Test file writers
    /// </summary>
    public sealed class ZipFileWriterTests
    {
        [Fact]
        public void SupportsContentTypeTests()
        {
            using var writer = new ZipFileWriter();
            Assert.False(writer.SupportsContentType(ContentType.Avro));
            Assert.False(writer.SupportsContentType(ContentType.AvroGzip));
            Assert.False(writer.SupportsContentType("test"));
            Assert.True(writer.SupportsContentType(ContentType.Uadp));
            Assert.True(writer.SupportsContentType(ContentType.JsonGzip));
            Assert.True(writer.SupportsContentType(ContentMimeType.Binary));
            Assert.True(writer.SupportsContentType(ContentMimeType.UaNonReversibleJson));
            Assert.True(writer.SupportsContentType(ContentMimeType.UaLegacyPublisher));
            Assert.True(writer.SupportsContentType(ContentMimeType.UaJson));
            Assert.Throws<FormatException>(() => ZipFileWriter.Suffix(ZipFileWriter.ContentType.None));
        }

        [Fact]
        public async Task TestZipFileWriter1()
        {
            var file = Path.GetTempFileName();
            try
            {
                using (var writer = new ZipFileWriter())
                {
                    await writer.WriteAsync(file, DateTime.UtcNow, new[]
                    {
                        new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 })
                    }, null, new DummyEventSchema(), ContentType.Uadp);
                }

                Assert.True(File.Exists(file + ".zip"));
                using var reader = new ZipFileReader(file);
                Assert.True(reader.HasMore());
                var result = reader.Read((s, t) =>
                {
                    var buffer = t.ReadAsBuffer();
                    Assert.Equal(1, buffer[0]);
                    Assert.Equal(2, buffer[1]);
                    Assert.Equal(3, buffer[2]);
                    Assert.Equal(3, buffer.Count);
                    Assert.Equal("[]", s);
                    return 123;
                });
                Assert.False(reader.HasMore());
                Assert.Equal(123, result);
            }
            finally
            {
                File.Delete(file + ".zip");
            }
        }

        [Fact]
        public async Task TestZipFileWriter2()
        {
            var file = Path.GetTempFileName();
            try
            {
                using (var writer = new ZipFileWriter())
                {
                    await writer.WriteAsync(file, DateTime.UtcNow, new[]
                    {
                        new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 })
                    }, null, new DummyEventSchema(), ContentType.Uadp);
                    await writer.WriteAsync(file, DateTime.UtcNow, new[]
                    {
                        new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 })
                    }, null, new DummyEventSchema(), ContentType.Uadp);
                    await writer.WriteAsync(file, DateTime.UtcNow, new[]
                     {
                        new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 })
                    }, null, new DummyEventSchema(), ContentType.Uadp);
                }

                using var reader = new ZipFileReader(file);
                for (var i = 0; i < 3; i++)
                {
                    Assert.True(reader.HasMore());
                    var result = reader.Read((s, t) =>
                    {
                        Assert.Equal(1, t.ReadByte());
                        Assert.Equal(2, t.ReadByte());
                        Assert.Equal(3, t.ReadByte());
                        Assert.Equal("[]", s);
                        return 123;
                    });
                    Assert.Equal(123, result);
                }
                Assert.False(reader.HasMore());
            }
            finally
            {
                File.Delete(file + ".zip");
            }
        }

        [Fact]
        public async Task TestZipFileWriter3()
        {
            var file = Path.GetTempFileName();
            try
            {
                using (var writer = new ZipFileWriter())
                {
                    await writer.WriteAsync(file, DateTime.UtcNow, Array.Empty<ReadOnlySequence<byte>>(),
                        null, null, ContentType.Uadp);
                }

                Assert.True(File.Exists(file + ".zip"));
                using var reader = new ZipFileReader(file);
                Assert.False(reader.HasMore());

                Assert.Throws<EndOfStreamException>(() => reader.Read((s, t) => 123));
                Assert.False(reader.HasMore());
            }
            finally
            {
                File.Delete(file + ".zip");
            }
        }

        [Fact]
        public async Task TestZipFileWriter4()
        {
            var file = Path.GetTempFileName();
            try
            {
                const string expected = "{\"teststring\": 1}";
                var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(expected));
                using (var writer = new ZipFileWriter())
                {
                    await writer.WriteAsync(file, DateTime.UtcNow, new[]
                    {
                        buffer.GzipCompress()
                    }, null, new DummyEventSchema(), ContentType.JsonGzip);
                }

                Assert.True(File.Exists(file + ".zip"));
                using var reader = new ZipFileReader(file);
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
                File.Delete(file + ".zip");
            }
        }

        [Fact]
        public async Task TestZipFileWriter5()
        {
            var file = Path.GetTempFileName();
            try
            {
                const string expected = "{\"teststring\": 1}";
                var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(expected));
                using (var writer = new ZipFileWriter())
                {
                    await writer.WriteAsync(file, DateTime.UtcNow, new[]
                    {
                        buffer.GzipCompress()
                    }, null, new DummyEventSchema(), ContentType.JsonGzip);
                    await writer.WriteAsync(file, DateTime.UtcNow, new[]
                    {
                        buffer.GzipCompress()
                    }, null, new DummyEventSchema(), ContentType.JsonGzip);
                    await writer.WriteAsync(file, DateTime.UtcNow, new[]
                    {
                        buffer.GzipCompress()
                    }, null, new DummyEventSchema(), ContentType.JsonGzip);
                }

                Assert.True(File.Exists(file + ".zip"));
                using var reader = new ZipFileReader(file);
                Assert.True(reader.HasMore());
                var result = reader.Read((s, t) =>
                {
                    var buffer = t.ReadAsBuffer();
                    Assert.Equal(expected, Encoding.UTF8.GetString(buffer));
                    return 123;
                });
                Assert.Equal(123, result);
                Assert.True(reader.HasMore());
            }
            finally
            {
                File.Delete(file + ".zip");
            }
        }

        [Fact]
        public async Task TestZipFileWriter6()
        {
            var file = Path.GetTempFileName();
            try
            {
                const string expected = "{\"teststring\": 1}";
                var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(expected));
                using (var writer = new ZipFileWriter())
                {
                    await writer.WriteAsync(file, DateTime.UtcNow, new[]
                    {
                        buffer
                    }, null, new DummyEventSchema(), ContentMimeType.Json);
                    await writer.WriteAsync(file, DateTime.UtcNow, new[]
                    {
                        buffer
                    }, null, new DummyEventSchema(), ContentMimeType.Json);
                    await writer.WriteAsync(file, DateTime.UtcNow, new[]
                    {
                        buffer
                    }, null, new DummyEventSchema(), ContentMimeType.Json);
                }

                Assert.True(File.Exists(file + ".zip"));
                using var reader = new ZipFileReader(file);
                Assert.True(reader.HasMore());
                var result = reader.Stream((s, t) =>
                {
                    var buffer = t.ReadAsBuffer();
                    Assert.Equal(expected, Encoding.UTF8.GetString(buffer));
                    return 123;
                }).ToList();
                Assert.Equal(3, result.Count);
                Assert.All(result, a => Assert.Equal(123, a));
                Assert.False(reader.HasMore());
            }
            finally
            {
                File.Delete(file + ".zip");
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("ab")]
        [InlineData("badheader")]
        public async Task TestBadHeader1(string data)
        {
            var file = Path.GetTempFileName();
            try
            {
                await File.WriteAllTextAsync(file + ".zip", data);
                Assert.Throws<FormatException>(() => new ZipFileReader(file));
            }
            finally
            {
                File.Delete(file + ".zip");
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("ab")]
        [InlineData("badcontentType")]
        public void TestBadHeader2(string data)
        {
            var file = Path.GetTempFileName();
            try
            {
                using (var s = File.Open(file + ".zip", FileMode.Create))
                using (var zip = new ZipArchive(s, ZipArchiveMode.Create))
                {
                    var entry = zip.CreateEntry(ZipFileWriter.ContentTypeFile);
                    using var stream = entry.Open();
                    if (data == null)
                    {
                        stream.WriteByte(0);
                    }
                    else
                    {
                        stream.Write(Encoding.UTF8.GetBytes(data));
                    }
                }
                Assert.Throws<FormatException>(() => new ZipFileReader(file));
            }
            finally
            {
                File.Delete(file + ".zip");
            }
        }

        [Fact]
        public void TestBadHeader3()
        {
            var file = Path.GetTempFileName();
            try
            {
                using (var s = File.Open(file + ".zip", FileMode.Create))
                using (var zip = new ZipArchive(s, ZipArchiveMode.Create))
                {
                    var entry = zip.CreateEntry("1.json");
                    using var stream = entry.Open();
                    stream.Write(Encoding.UTF8.GetBytes("data"));
                }
                Assert.Throws<FormatException>(() => new ZipFileReader(file));
            }
            finally
            {
                File.Delete(file + ".zip");
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
