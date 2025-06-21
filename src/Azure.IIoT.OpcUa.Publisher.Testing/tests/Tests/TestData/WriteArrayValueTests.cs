// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Testing.Tests
{
    using Azure.IIoT.OpcUa.Encoders;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Furly.Extensions.Serializers;
    using Furly.Extensions.Serializers.Json;
    using Opc.Ua;
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using Xunit;

    public class WriteArrayValueTests<T>
    {
        /// <summary>
        /// Create node services tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="connection"></param>
        /// <param name="readExpected"></param>
        public WriteArrayValueTests(Func<INodeServices<T>> services, T connection,
            Func<T, string, IJsonSerializer, Task<VariantValue>> readExpected)
        {
            _services = services;
            _connection = connection;
            _serializer = new DefaultJsonSerializer();
            _readExpected = readExpected;
        }

        public async Task NodeWriteStaticArrayBooleanValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10300";

            var expected = _serializer.Parse(
                "[true,true,true,false,false,false,true,true,true,false,true," +
                "false,false,false,true,false,false,false,false,true,false,true," +
                "true,true,true,false]");

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Boolean"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArraySByteValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10301";

            var expected = _serializer.Parse(
                "[-94,94,62,22,-50,36,105,103,-60,56,-102,-14,-59,-83,119,-101," +
                "-39,85,-9,-14,-7,-100,64,122,-107,-61,13,-10,-19,81,-52,57," +
                "-32,-90,27,-128,92,44,-32,13,-93,-10,46,9,-38,55,116,-11,-43," +
                "63,-45,-103,2]");

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "SByte"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayByteValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10302";

            var expected = _serializer.Parse(
                "\"jgYexIAKF3N6c2tgEh6R9j+tdOlOAm43n15OFyGtfjI2VhgVYpis1fYvfL" +
                "qdeiRVY94AJSUZ\"");

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "ByteString"
                // TODO: Assert.Equal("Byte", result.DataType);
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayInt16ValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10303";

            var expected = _serializer.FromObject(_generator.GetRandomArray<short>());

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Int16"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayUInt16ValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10304";

            var expected = _serializer.FromObject(_generator.GetRandomArray<ushort>());

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "UInt16"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayInt32ValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10305";

            var expected = _serializer.FromObject(_generator.GetRandomArray<int>());

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Int32"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayUInt32ValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10306";

            var expected = _serializer.FromObject(_generator.GetRandomArray<uint>());

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "UInt32"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayInt64ValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10307";

            var expected = _serializer.FromObject(_generator.GetRandomArray<long>());

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Int64"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayUInt64ValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10308";

            var expected = _serializer.FromObject(_generator.GetRandomArray<ulong>());

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "UInt64"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayFloatValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10309";

            var expected = _serializer.FromObject(new float[] {
                float.NaN,
                0.0f,
                1.0f,
                0.0034f,
                2543.354f,
                float.MaxValue,
                float.MinValue
            });

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Float"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayDoubleValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10310";

            var expected = _serializer.FromObject(new double[] {
                -5.0,
                1.0,
                0.0,
                6.0,
                10000.1,
                double.MinValue,
                double.MaxValue,
                double.PositiveInfinity,
                double.NegativeInfinity,
                double.NaN
            });

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Double"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayStringValueVariableTest1Async(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10311";

            var expected = _serializer.FromObject(new string[] {
                "test",
                "test2",
                "test3",
                "test4,",
                "Test",
                "TEST"
            });

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "String"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayStringValueVariableTest2Async(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10311";

            var expected = _serializer.FromObject(_generator.GetRandomArray<string>());

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "String"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayDateTimeValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10312";

            var expected = _serializer.FromObject(_generator.GetRandomArray<DateTime>());

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "DateTime"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayGuidValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10313";

            var expected = _serializer.FromObject(_generator.GetRandomArray<Guid>());

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Guid"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayByteStringValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10314";

            var expected = _serializer.Parse(
                "[\"y5rM6KSrJ9+U0zDRyN8nPrLz4zyKydoagl0A2Sz0XTeJ0GevE2/tFCMCp" +
                "Fj9IA7zLA==\",\"OLyUL7UBwLFmRqtOs6B1+Ef3eHgqQgbPGIglgRxhTw2t" +
                "uLugo1obZe5hCadp1E4E4wTZFnuzSPa2xRYs3D+UBfeeoQ==\",\"5NVV7dw" +
                "Sicg5X2ZEOiCeGIc4kflvxeneQ+6Ir80Er8oz\",\"Hw2v7DpoKn+8V17S8r" +
                "uhfH2+DKeLH+t3qe2K2vs0r5sQ5wGQaZr+Kc/vCJJ/cPvMDlCG8b6LxwAhOE" +
                "JM3U+DiKfDwQu6EBPiv+PYmxZZ9znxvcyrrb3dhR8AbjFXsUt/bIDq\",\"r" +
                "vAR/TyjMtuzK+nIQAlbIIPY2urlPOy6oeKLDpdZL5ZuO4oB\",\"zm4JiwLS" +
                "9Bam8E5WK/uTpQDWcgRXW1oYI0Na+1NaBX5adLVp0F7javej+/I8uGxdm/Ft" +
                "ZBj9VHMU8Ge+ePjDTOvP+7dGKtWi8ggMtGBo3GvX/F9h7KBlXSBBVN/2JQ==" +
                "\",\"/9+uB2y8tRdxG8pyEPCfEqwU++d7mGFfSmmelgTV9azNFg3VsWFvryH" +
                "l7kUhLlVsgC+oyvo+dmZ9TrwCHR4SmyYepfrjymFLpUzFlqocPhwUMRbVLSq" +
                "xj5+nWGKkbJED0lIf\",\"iVuJts1dJa144LxhEKDu6wc9bvttFlo8cv6mOb" +
                "rCw1f8O5O5qGAHzwUDFr/q9lIEfqX7leQjlXIATUPPtuzzthxk\",\"1A00Q" +
                "6c2nvZp/5pRe6yiuxXGLnPYpiC5Wxw4GRkQYKOGkn32SWJUX0UtK3Nl+8giF" +
                "DHe7+3xjC7se+eK/YXcWZ/3y+kvq3Gycg==\",\"Lap7rVNfVBUF6DJwtZQG" +
                "TgnNeslhv1DR024w3/Novyt9Hg==\",\"4bM/KIg26ID0Jy9BNuPgoqQ4qG5" +
                "uWqnZ5S51IZwoBvKX0SSbyqXyk3g+W6qkLA==\",\"+xLGG/PUsFSGCWVCWc" +
                "HG48wgPKpkvkTX/kUV9S/+/jW4NHkO1g0THuZ8npFk\",\"mNGyWo+KZ4GVP" +
                "c7uHK2+uyWICy+JJUe3Db4MHsPoILNznK1EQ9VudM03njtA6PFmXytXFYY02" +
                "HOJ+k3Rfg==\",\"vawdGnp/AHp4UBmWYlKGDHYkzKa+mJEKLD7DObpIh532" +
                "g3nyHiVtcWXkswf879KF7H2DKQTXiyUbEu48vMbAL8h7oN7ZlO1QtltA2eSK" +
                "Dj1rPUw8xA==\",\"mR74U9YMNzG1nySDPoBO9KtYkDNyqJc=\",\"QJEEsd" +
                "B+eYNrauKC5Ya94BmEpqG1YUuN7neR9KuR6/CHRxJFCIZhNiWgGNOdzA==\"" +
                ",\"JZqvGe4C9fzfRXbCWOyF19o=\",\"8g==\",\"nnN42JMjb3sUSXjwImh" +
                "sWfx3/e2RSzMAkcMISqDIPVSr0XGxItaGy5N655bMhqDwgCvbYoEEaVNAd0f" +
                "PGiXaSwsjRFD3uzYl8DJaFmOcMwsiGMl07dvTpmcecKqPt7Bt\",\"m4upQV" +
                "D5xfS2ck+qUu+kRDutAltmXjBPnejZSoyVOYHsd8gRicnSHgR/NmmD/BUuzG" +
                "ZUCa4SOTxbWQ==\",\"ZyB6aCA4zr3bJ5O6cdrjVuYoxsyh06p+JUEv0to+i" +
                "7YvJmv+ihcWMLQ/Ogc5rOIvy7tSFJSNApN1/EPfCPzinZvciktv5cw=\",\"" +
                "EZYXYpmiRHlfAAcf63YhoiPXiI7TNyi4XJ1rXCGWwOXvgwL+daD607fJfwj1" +
                "w7Eku+g=\",\"l1w++9ptXwb8NuhYyEAhMGd0IyyD3LXWCbcgw5jrR6E4byi" +
                "IsIcnkBaa/pzOcbfH05NJsJe7o7un\",\"8SNNm20Q70cfWBiYf+BMkxbp3h" +
                "gHmP9Y7wShlgl9y2zhSZUEQnos7ayHUl6vOxRpZAOlut2PX1kk\",\"rGlvW" +
                "T2ijau3NUpIUZu98GyARqCos/A4MQXXEI9S6TzgTM7DflJJr9EEhN4eHLIJ/" +
                "ewi3hKDTfh5Nig3ynwbZUwOZwTN+nEZNpbPoy8RGQ==\",\"U84Toi8KKV3+" +
                "1UlHKhLlVOgfkhC0rjwDewmofLOqyWcdE+T8aHg1QeQKBfewKe4ksXu6D34J" +
                "YKSRECcmYqypVAzhKhct6etw6VErqalFUlEag8s477rpFLJAalg=\",\"D94" +
                "4YbDrzv/X9sePSiUt3rb7TL7WDN/hcWVS+BZk7X6THtpy8w==\",\"+FmDUU" +
                "KNA6illLw1/EQdmnsD/1bvS5nW6XIISPwrS7rqbQLyOoTJUt3JwL084GipYf" +
                "mj13OcWa3ArWg=\",\"hLuseUkBS45CK2D2Oq14JovbmQgKX+cCiHNHTRQ=\"" +
                ",\"MYwc+M0iHEZ0OpXZ\",\"NZisnMUkMiLlE1/FJNOp8dW6/Xnt+dPaQd9" +
                "VCURnXnMw+LFp29aBKeEyTHi8MqlPclSegJhgeTzF0lvmzindfbTrlSshPxb" +
                "+ONOXuqxaHQ42Bpbvyd4=\",\"0H91pAh7m1q4Vc/E0Z1+LTM+n0fEyL+Par" +
                "VMsMiS7bTWEMSsrJsOg51432TdlkxwuNhjsSmW\",\"CL/lOVMu6pIlZxvtw" +
                "dnRgJgwHKpM6Vqy1Gjb+O8vPIo/+bzsx2G6VLLmufzCqYuX5ljLgQ3syh5kM" +
                "mXqU/Ki9iuQrccc5Lg2dcf3ZS9d8CrdQxGXZLp8\",\"Lp2IlfvQAb9DwtEx" +
                "+8N7AZ+UC0X0ZO8GQToMD+ELknEUWZl+XCM7pcY3ImyMvy8ayBD3z642Xsmu" +
                "gVPAw/HeNzBf\",\"oSn8pHxZZn69QoJ5gQvxR9ATwsLd+9DQ28964dVY6js" +
                "soo3xy2t7dki4oYcrB9K10qkNJG6dP6ZZLMmt3oWpZFdGTd59AHxl\",\"S3" +
                "klWUik0/3FDytKxs2PY+eXnqLNmMIOPC/kuvM30S2oiyIjGkwQh/WwGHZ9Gp" +
                "F5vnuVaN0=\",\"yrO3Fh0ZwBkJjT3+oQ==\",\"qmtJm4cWrB1/TJEinWv8" +
                "d6FoT+XBb2wDL/bH26aLOX94Vs5TMfrhxxasM2qF+nxUHZLs1K68eTr+RmyQ" +
                "TtF39BMRqkac\",\"RqeHiM8UPAJ+NHpzvhSeZNG73ZNU645ReEU1Pldz\"," +
                "\"NrdzS0RNJu1l/vzHOBbbEmNVrkDuaQlUWhg1D4re/aoK40S7SshaF6rOd5" +
                "VYI1Y7nTLS6akzYsyuH/EsFxZjhgFRvVTSH8g=\",\"+5eVIPFz+zbC+nJXD" +
                "BQB1oF3DCvXLf+Ua/GUB2YBaX1qN+0RnkLbOiO6ah2kC3rsUOY=\",\"WhOd" +
                "w82YAbQA3/TQwJH07dQm7zg/m9EHHcPh7192p7Z/Lg5qzXQfeZ2N21su6amM" +
                "\",\"HqbGYK6Uv0GVpQq2EVSJOD1Rkg==\",\"ih9fBnabscB22DMaVT9B0O" +
                "BW0JlPKDfPFcKLOFEi8JgXCSX+RsDpE+L/yr1y8GHSErlY54D4M2HokPCzD3" +
                "huVOebA0sZQ4kgbLy3\",\"Xno=\",\"JWG4JZjKCRce1sQWts82A2mQdtXW" +
                "7Ba5zzA3esRB4wNtM8pSV1LWit6E5HVVLdJQNrhNv+elhjhL/lZnko9S3PLA" +
                "qIME5KwWuuz3UgihZKJd50ML\",\"JBvIrZIHWPxu904xB4KUhU9LUgw2Bfs" +
                "=\",\"NDCpmrMJq0tH6M6k39Vy3FMaS/SoIAAezx0Om/H+vR4gG3hiXb5IZE" +
                "wD6R+08KV52XW4HLoHBRMvisqW+w9ZSCqvhMQ=\",\"EH+U0eanpDbhat93l" +
                "cF8RaFkGBVlMtLzsWk8qqH0BGvhHMn1I0rTX2DdTiDrH2VN3QKJS2uu996so" +
                "R98avqV7WZzlnj4hiFWkLm1iOOyB5ID782ea7kAMiTrSIIrV/RmR2C5\",\"" +
                "iexVVuEyurL5b/r6mYx4ey33gq1zY1QNPZ8rOcROQIP4jgkWmna8OP5NObXx" +
                "Tuie3nk5T94MBSWYRxlBBoHuqi53AQqDYeE=\",\"OkmuJSqeHA==\",\"to" +
                "1aSw==\",\"8aTsieN+Ulx7Mn1ejoZ6TN0afZeCFJVd3/v5KyqLeSajlD3SF" +
                "YGg449avxcxx5sYoqLYct2zk9wPjqnnhCcy8LhWu/lJpzZceg==\",\"CCdV" +
                "fSwYYOGIh7IHSe8BYMOH17heaCfPXtsLoc77a3z4Q6prhpFhkYejnqo9sQzL" +
                "+Ojjr4TCOqD/hh9RDL4SjRw2DgVEldoyJUYwu8l9ka3OQKg=\",\"F7z3NQg" +
                "KbAByEITJ\",\"EI4HogeGYw6xiT6Rf9aC9HGDzBUsdl8/2Q==\",\"OinZv" +
                "xXCZiu574oKx7lF12UzuKOJM+kMAI23s2OZLg==\",\"yQlMOjTydHwrUx2T" +
                "ame8EtBUESyGNXMGP82/\",\"CrLHH8DBQb1tCqJzvjx4NRZWoZjRpOMOiAJ" +
                "BlJVF+2cy7HF4i/7SqCb+pcWynm1KvA==\",\"7+grJrVlzrmXvoCtJygPO0" +
                "DSygCpGIT5ctL/Hlw=\",\"lPfXV1UTOws2DKnSZxQoeNd9tnZdm4C8lNdjM" +
                "rPjZrBcrSmI1c/S/P81m71W17Fw0Pn7gmjmi/l2GsN+rwhXU6X5dQ==\",\"" +
                "6p+jB6q5dij/wmW8JvNhuufkSHYhLxK6DFRN991HGF1h94UxVvPXrm9i+bS0" +
                "Ble7p51rwbD/l7qN6Ps=\",\"bH9LuKoIWi5uMGP0DJkHvjZdBQb8kU8/3Fa" +
                "sYl/LpG74gEHwF0EKrKspTXc6d7QX99mEL+L1WtsvC9tTW/jOo4v0/IxXF6f" +
                "0U1la0kTYBWWilWo=\",\"RnWMAiVmwAtltlGzWqrrRXoov+AxkNyJmoeWVg" +
                "Z2scWNSFwWaPlm8VabjpwIp3ea7L/qedaJVLaZA2idz2kF7wU=\",\"JRJN1" +
                "d2Awc2K\",\"AT+A8xD8aUaMN+8D8w==\",\"fWWRyCHslp+WfYFlTBfiabc" +
                "I/760WYJ1jdM=\",\"vmLNmyEfp1d37/msnf9iqANxqq9SZCKA5gQ08STjHD" +
                "pO7dg8yeFfRaW/anm+W2K/UbB+A+MjrVk/TbQd6rUzlNO+XrAnJd8i\",\"Z" +
                "AqS+qjBiqF1oHNkGFcqP379eyxBWu+L1aYA/bKXK9Bc8n4YGl+WjCl7dRXAM" +
                "LpmM3CoKGT1zwkaBg==\",\"AsGN7910ogcm2RIkH2bvA8bl2suk65nJQe8D" +
                "bGcidP0TOpTKnIuXoR0plI0hS4jCdPIJJEA7gBYnw67ucTWfA2UUEeXRJGY6" +
                "TTmte1cMqPZmQPd1GU+92Ll9otjdqS/niZAn\",\"OqLwT4vP1gaOBL8bG7e" +
                "HMD4q6hVWOCtTgY1cpAnF8sY=\",\"mrvDUKfpSXm2SbW+h70lIf+pXqYStg" +
                "F3nM79cjL8yBH4NoD7w+CWQKGbn+52lfrQNoIKG5Mg0uS6TLPZ7NZHuYGjxl" +
                "E9KV4eEx4EdhcSAAc=\",\"LTyv8qx+2GSbQXYsvFTKVOVt0s+pkgwk6wh1G" +
                "1vNp0ysBr+amMfUJe3soejn8VfJOiIxpS04KpovigakCLwIzg==\",\"KoC1" +
                "lS+8Hpit\",\"vPEkPfmwq+A4mwIec9skaw==\",\"wDAdMeTSTrxxoTQKZh" +
                "MMgFdtqApNDPNc288zlwwM2nDErl1okK8=\",\"H9aBiA921dKI9Z0cbntgw" +
                "LQ8wFUUn7avZMfERri2o/CnlQ==\",\"NMuD5OUG/auibvBr9GTuqSzPWsR/" +
                "INEkR7KPGtJRrXXY8iz0MFjbTxb0FEB0W9DhyQ13IUzRChP6X5wyduPYUMF6" +
                "kwI4+njU8wutiwyRsA==\",\"4a1QxNqMKQfoGPrRu6jYMhvEf/nIzqnPlKM" +
                "tQCM=\",\"B2iusFHIkhR1NVGMGSAApxhKM3twRxK2JBMEgTA3XujjaRCRIg" +
                "tXD58HTkmnPLSJzu2K4g0=\",\"Dr9W94w9xBHjPtZpqLAPboQN8yPbafnID" +
                "udfe7JDrHvaO1tQOyp/GkuOIxBzPpMEeWBofe/q\",\"pcaN6KTeSF6gklqP" +
                "bVW+M1mO2NdMRelV0FNbAg==\",\"2i6M7222iq1C2jwDL4J9GK8mr7osDu8" +
                "WE4Bf3TgYUIJGDMZjzp//njSFF7Oc1B3DazwgLKxHJcs/ddztVq11KQ==\"," +
                "\"ub0jvBpKECcQhqf2O3o+Y5xx/Pcj1v7m+iNZwhFIhxDuKeMX+QZR8hAFnM" +
                "F+1QT7nYq5O3gyZuK+PcH28E0Vx/+XCBGXJ2mVIArnHjo8ZyUfXiSHjkuXvA" +
                "==\",\"AFi7RFoBwu/UwufM8mR2ICQabktHqIHVnNeSKI45s/9MTZbOrVHGC" +
                "CQuZnqAWCz6rmZqMChxfFfX5qiDbplhmw7ngKTNp3LT\",\"ZiYYQgdURKqY" +
                "eifPg9fb0T73x6hDmorNy2uZ2IgsB/SX28JKYxXPkWTY1h85zIsQpNKOr6sq" +
                "EcucSnebu2sia3IzNw==\",\"FJBuKSg1RGrEa5P/4IgztZ5XSG3K9q4YnjT" +
                "V+LZqrAaU4/sUZGWHdeWmBzG3eLsyOGzNG+rK3R7iVYr0YEy0Wb6NavyNHw=" +
                "=\"]");

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "ByteString"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayXmlElementValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10315";

            var expected = _serializer.FromObject(_generator.GetRandomArray<XmlElement>());

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "XmlElement"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayNodeIdValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10316";

            var expected = _serializer.Parse(
                "[\"s=%eb%85%b9%ec%83%89%ec%9a%a9\"," +
                "\"http://test.org/UA/Data//Instance#g=bc3623b6-cb5f-e6be-1c13-378f9126c663\"," +
                "\"http://samples.org/UA/memorybuffer#i=1556000973\"," +
                "\"b=728HLX82OBG556w1XptI6JmYAeS%2bs33GiJu3gjZHWRJ8o3Bct7mc3f592D8ZLQ" +
                    "l5hMckxA%2fVWjvxQaTkfrXbco2auLoq7DHg%2byPlQNs%2brFuRPf5vIpUCp0k0ag%3d%3d\"," +
                "\"http://samples.org/UA/memorybuffer#b=XkiiZbF%2bdE9PmJVDyOzvGWCzlElTQbdsT%2fiYEQ%3d%3d\"," +
                "\"i=1137425282\"," +
                "\"http://opcfoundation.org/UA/Boiler/#i=781022622\"," +
                "\"http://opcfoundation.org/UA/Boiler//Instance#i=3738789614\"," +
                "\"s=%e8%9b%87\"," +
                "\"http://samples.org/UA/memorybuffer#s=%ec%9b%90%ec%88%ad%ec%9d%b4\"," +
                "\"http://opcfoundation.org/UA/Boiler/#g=63e6b815-59de-d915-7884-26527beb2666\"," +
                "\"http://opcfoundation.org/UA/Boiler//Instance#b=UJJxK4FZQCLL2gDeqAtnX9RHg2OUUABmSO9ltOiQe2hT\"," +
                "\"http://opcfoundation.org/UA/Boiler//Instance#b=pI1kCZ03Sv93pn1HE4tSHt%2btvg%3d%3d\"," +
                "\"http://test.org/UA/Data/#i=4103267082\"," +
                "\"http://test.org/UA/Data/#s=%e9%bb%91%e8%89%b2\"," +
                "\"http://opcfoundation.org/UA/Diagnostics#i=1186710474\"," +
                "\"http://opcfoundation.org/UA/Boiler//Instance#b=XQUB7qFxHaBdPl2JpQtAEpqq0" +
                    "hUQ%2fQ%2bf4BqefFDPCzpl52D6kBtBINbr2%2fwCDebTirEgnBFktV%2f2YQ6H0qFQjTIiJL6qRoF%2baLE%2b\"," +
                "\"http://opcfoundation.org/UA/Diagnostics#s=%e7%b7%91%e3%83%96%e3%83%89%e3%82%a6\"," +
                "\"http://samples.org/UA/memorybuffer/Instance#g=296faf9f-3101-0401-2e0c-b94d359a4dca\"," +
                "\"http://opcfoundation.org/UA/Diagnostics#i=792930600\"," +
                "\"http://samples.org/UA/memorybuffer/Instance#s=%eb%b0%94%eb%82%98%eb%82%98\"," +
                "\"http://opcfoundation.org/UA/Boiler/#i=139672771\"," +
                "\"http://opcfoundation.org/UA/Diagnostics#g=22f9c2de-a88d-5342-71ae-64098bd449bd\"," +
                "\"http://opcfoundation.org/UA/Boiler//Instance#i=225423009\"," +
                "\"http://test.org/UA/Data/#b=ocMhatBmRYbobwfmxpuAAdwwhPobBGmZBlNHZv" +
                    "9B0YYvRvlIeihSBDomVB1yFwIK2T3bfOwJxah2R9H96Gp52AtnvmDz\"," +
                "\"http://samples.org/UA/memorybuffer/Instance#i=56793307\"," +
                "\"http://test.org/UA/Data/#b=1kZSrfTPYIdPipmDIylA%2fPvHwSChwYTMoE578" +
                    "yc5Vi82S0iXM%2fmpsVb4XxFGcGsSmptnPJYOkBXChQ2mU21fFjNqTdjhqckBHg%2fKqWFzhi%2fiaFiM0hqY" +
                    "J7sy1a7rAg%3d%3d\"," +
                "\"http://samples.org/UA/memorybuffer/Instance#b=Sd3cIpMx8dJmHYZE57NO%2f97D6hXTRBk%3d\"," +
                "\"http://test.org/UA/Data/#g=1ad3ae1c-1c15-e1b1-0f18-96aa0c4f3766\"]");

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "NodeId"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayExpandedNodeIdValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10317";

            var expected = _serializer.Parse(
                "[\"http://samples.org/UA/memorybuffer/Instance#i=2144658193\"," +
                "\"http://samples.org/UA/memorybuffer#b=c9PGBcMJ%2fXaDHbdQAdVi15q4Vd" +
                    "F0s64Z2BzAQUguTWwH3T4OSRPSoA%2fZs0gCG%2fs8gfzkzVk8yr8krC2nSLstV" +
                    "dBLSCSWVI2H5rTBm%2f9mFrwhhMA%3d\", " +
                "\"http://opcfoundation.org/UA/Boiler/#g=7e12cb12-9cea-2be5-5753-ab5e78b7d3d7\"]");

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "ExpandedNodeId"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayQualifiedNameValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10318";

            var expected = _serializer.FromObject(new string[] {
                "http://test.org/UA/Data/#afsdff",
                "http://test.org/UA/Data/#tt",
                "http://test.org/UA/Data/#sdf",
                "http://test.org/UA/Data/#afsdff",
                "http://test.org/UA/Data/#sg",
                "http://test.org/UA/Data/#afsdff",
                "http://test.org/UA/Data/#afsdff",
                "http://test.org/UA/Data/#23234",
                "nanananana",
                "http://test.org/UA/Data/#afsdff",
                "http://test.org/UA/Data/#w",
                "afsdff"
            });

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "QualifiedName"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayLocalizedTextValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10319";

            var expected = _serializer.Parse("[" +
                "{\"Text\":\"복숭아_ 파인애플&quot 황색 말 검정: 황색 고양이 자주색! 파인애플 녹색( 암소& 개 딸기 양 망고 들쭉 뱀 용> 고양이 빨간 파란 빨간@ 들쭉\",\"Locale\":\"ko\"}," +
                "{\"Text\":\"Бело Корова. Известка\",\"Locale\":\"ru\"}," +
                "{\"Text\":\"석회* 파인애플) 말= 바나나^ 양/\",\"Locale\":\"ko\"}," +
                "{\"Text\":\"코끼리 백색! 황색# 뱀{ 바나나? 파인애플 녹색 백색^ 빨간> 녹색&\",\"Locale\":\"ko\"}," +
                "{\"Text\":\"Horse? Blueberry% Pineapple Peach Red/ Banana$ Cat& Black_ Lemon Cat Purple Pig Red Horse Pineapple&quot\",\"Locale\":\"en-US\"}," +
                "{\"Text\":\"紫色^ 菠萝 柠檬 桃子 蓝色; 马= 大象 马 狗 狗( 母牛$ 绵羊. 马 鼠 马 马 草莓 柠檬* 大象 马$ 柠檬 蛇 红色 白色$ 猴子 绿色 紫色* 猪 草莓& 草莓 龙# 绿色* 蛇 葡萄\",\"Locale\":\"zh-CN\"}," +
                "{\"Text\":\"绿色 紫色 白色 鼠 母牛} 蓝莓* 草莓 蛇< 猴子 香蕉_ 柠檬. 猫 猫 桃子 大象 柠檬 葡萄 桃子&quot 绿色 菠萝? 桃子\",\"Locale\":\"zh-CN\"}," +
                "{\"Text\":\"파란 바나나 빨간 파인애플&quot 검정 용 돼지 들쭉^ 암소 암소 고양이 황색> 복숭아\",\"Locale\":\"ko\"}," +
                "{\"Text\":\"Mango$ Snake Monkey&quot Dragon# Rat( Rat Mango] Pineapple Black Dog Dog/\",\"Locale\":\"en-US\"}," +
                "{\"Text\":\"Овцы&quot Овцы^ Дракон^ Манго Бело% Собака Змейка\",\"Locale\":\"ru\"}," +
                "{\"Text\":\"Чернота Собака] Корова Бело) Лимон/ Кот\",\"Locale\":\"ru\"}," +
                "{\"Text\":\"원숭이= 복숭아}\",\"Locale\":\"ko\"}," +
                "{\"Text\":\"黒% いちご; ブタ 赤い_ ドラゴン$ ブドウ- パイナップル@ 猫% バナナ 青い= レモン いちご* 象 バナナ 白い} 白い 犬&quot 紫色 馬 猫) 馬 マンゴ+ バナナ) ヒツジ ブドウ\",\"Locale\":\"jp\"}," +
                "{\"Text\":\"狗~ 大象, 柠檬 紫色 猴子 蓝色$ 柠檬 猫 猫 葡萄$ 猫 狗' 蓝色 绿色# 猴子 猪; 猴子 绿色/ 葡萄\",\"Locale\":\"zh-CN\"}," +
                "{\"Text\":\"Лимон# Желтыйцвет% Зеленыйцвет* Овцы Корова\",\"Locale\":\"ru\"}," +
                "{\"Text\":\"Известка Желтыйцвет^ Кот= Желтыйцвет( Зеленыйцвет) Кот Кот\",\"Locale\":\"ru\"}," +
                "{\"Text\":\"パイナップル ラット 犬* ドラゴン^ ドラゴン} ヘビ 猿- 黄色* パイナップル]\",\"Locale\":\"jp\"}," +
                "{\"Text\":\"말 돼지&quot 검정& 녹색* 딸기{ 망고] 들쭉&quot 뱀< 말# 양~ 용@ 뱀/ 파인애플 파인애플+ 돼지 황색{ 고양이, 녹색 검정 양. 뱀 용 자주색_ 들쭉\",\"Locale\":\"ko\"}," +
                "{\"Text\":\"파인애플 자주색 자주색: 빨간 백색 빨간\",\"Locale\":\"ko\"}," +
                "{\"Text\":\"猴子 猫! 桃子, 香蕉* 猴子; 蓝莓 草莓 红色 黄色 蓝莓/ 鼠 蛇 黑色> 芒果' 芒果, 红色@ 石灰 蓝色 猫{ 鼠\",\"Locale\":\"zh-CN\"}," +
                "{\"Text\":\"牛 石灰> 馬* ヒツジ 黒? 黒~ いちご_ バナナ_\",\"Locale\":\"jp\"}," +
                "{\"Text\":\"ヘビ} 青い 猿 猿 馬 ヘビ% 黄色) いちご いちご< 紫色/\",\"Locale\":\"jp\"}," +
                "{\"Text\":\"Овцы Голубика Красно Змейка` Ананас Персик` Кот Банан Крыса\",\"Locale\":\"ru\"}," +
                "{\"Text\":\"白色' 芒果 狗 芒果) 红色 桃子, 桃子; 蛇- 鼠 鼠 草莓 黄色 红色 蓝色* 白色&quot 葡萄%\",\"Locale\":\"zh-CN\"}]");

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "LocalizedText"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayStatusCodeValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10320";

            var expected = _serializer.Parse("[2555904,9306112]");

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "StatusCode"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayVariantValueVariableTest1Async(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10321";

            var encoder = new JsonVariantEncoder(new ServiceMessageContext(), _serializer);
            var values = _generator.GetRandomArray<string>();
            var expected = _serializer.FromObject(values
                .Select((object v) =>
                {
                    var body = encoder.Encode(new Variant(v), out var t);
                    return _serializer.FromObject(new
                    {
                        Type = t.ToString(),
                        Body = body
                    });
                }));

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Variant"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, _serializer.FromObject(values), result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayEnumerationValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10322";

            var expected = _serializer.Parse(
                "[213809063,256148911,1403441746,1765077059,1459915248,178083" +
                "9730,1170915216,676529496,206863250,700571842,1421759593,311" +
                "197401,2102772218,857824445,1673233467,1438792918,682491618," +
                "113683929,1784104203,508655730,154788171,1059835353,18362300" +
                "58,201534405,291502570,265713070,1462120528,163301337,883718" +
                "228,387243245,241294166,1024734946,868521396,1226516137,2049" +
                "366608,1946103069,966183232,874571368,1377393447,1387753822," +
                "558428041,1888176372,210663882,185274868,1386322555,12440540" +
                "68,45448351,2138501043,223371060,643696097,1931984232,590598" +
                "358,992471751,2028344606,1582760362,1301457357,567402500,120" +
                "1907341,1473574489,1194202178,318719202,1007613879,194029491" +
                "5,1967103652,459277676,397024647,1590159652,876256081,955941" +
                "484,1874614045,472609686,1955403897,883774129,396350381,2097" +
                "711336,414095446,849171471,1650955190,962695497,1194932149,2" +
                "20264719]");

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Int32"
                // Assert.Equal("Enumeration", result.DataType);
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayStructureValueVariableTestAsync(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10323";

            var expected = _serializer.Parse("""

[
    {
        "TypeId": "http://test.org/UA/Data/#i=9440",
        "Encoding": "Json",
        "Body": {
            "BooleanValue": true,
            "SByteValue": -38,
            "ByteValue": 110,
            "Int16Value": 8055,
            "UInt16Value": 55806,
            "Int32Value": 256543409,
            "UInt32Value": 1124716060,
            "Int64Value": 6272273485009155588,
            "UInt64Value": 8748332193282252019,
            "FloatValue": 1.3550572E+28,
            "DoubleValue": -55.151821136474609,
            "StringValue": "레몬 딸기^ 고양이) 파인애플",
            "DateTimeValue": "2071-08-08T14:25:16.7814639Z",
            "GuidValue": "2f1b64e2-9b6c-9ff9-9bcb-681a5030910b",
            "ByteStringValue": "XmIaOczWGerdvT4+Y1BOuQ==",
            "XmlElementValue": "PG4wOum7hOiJsiDjg5bjgr/jg6Ljg6I9IlZhY2EiIOOBhOOBoeOBlD0iQ2VyZG8iIOefs+eBsD0iQXLDoW5kYW5vIiDppqw9IlBlcnJvIiB4bWxuczpuMD0iaHR0cDovL+efs+eBsCI+PG4wOue0q+iJsj5Nb25vIFZlcmRlIFV2YSBTZXJwaWVudGUgTW9ubyBBenVsIFBpw7FhIE92ZWphLiBNYW5nbyBMaW1hPC9uMDrntKvoibI+PG4wOuefs+eBsD5NZWxvY290w7NuOyBQZXJybyBBcsOhbmRhbm8gTGltw7NuJmd0OyBBbWFyaWxsbzwvbjA655+z54GwPjxuMDrjg5bjg4njgqY+T3ZlamF+IFBlcnJvIFDDunJwdXJhXiBMaW1hIFJhdGEhIEJsYW5jb18gUMO6cnB1cmE9IEdhdG88L24wOuODluODieOCpj48L24wOum7hOiJsj4=",
            "NodeIdValue": "http://samples.org/UA/memorybuffer#b=672G6bOkm2X9OQ4V",
            "ExpandedNodeIdValue": "g=b869d987-396a-5018-7e4d-556d5e591587",
            "QualifiedNameValue": "http://opcfoundation.org/UA/Diagnostics#Dragon",
            "LocalizedTextValue": {
                "Text": "母牛@ 蛇 马- 蓝莓 猴子< 绿色 蛇{ 白色$ 绵羊 绵羊 紫色 紫色 猴子[ 猴子! 蓝莓(",
                "Locale": "zh-CN"
            },
            "StatusCodeValue": 6356992,
            "VariantValue": {
                "Type": "ExtensionObject",
                "Body": {
                    "TypeId": "http://test.org/UA/Data//Instance#g=a2a62a11-ee81-11e2-c797-f015f0dcc7bf",
                    "Body": "+ejk7JrPhOKfaxAk3LnqVYIbn5/Oh111kBH5HcAc46atRudt/iWP1h6eT3cBow=="
                }
            },
            "EnumerationValue": 0,
            "StructureValue": { "TypeId": null },
            "Number": {
                "Type": "Double",
                "Body": 1.0
            },
            "Integer": {
                "Type": "Int64",
                "Body": 1
            },
            "UInteger": {
                "Type": "UInt64",
                "Body": 1
            }
        }
    },
    {
        "TypeId": "http://test.org/UA/Data/#i=9440",
        "Encoding": "Json",
        "Body": {
            "BooleanValue": true,
            "SByteValue": -71,
            "ByteValue": 165,
            "Int16Value": -31474,
            "UInt16Value": 60031,
            "Int32Value": 1002303007,
            "UInt32Value": 2322690949,
            "Int64Value": -8682057831558849682,
            "UInt64Value": 1004227894202161316,
            "FloatValue": 4.1843192E-05,
            "DoubleValue": 32635254472704.0,
            "StringValue": "Голубика> Дракон@",
            "DateTimeValue": "2014-01-20T12:32:21.1556352Z",
            "GuidValue": "252c98f0-ad64-fd43-2056-044339a3fb6e",
            "ByteStringValue": "E3P8wU/iTsNcmseUhcjHs2z228AvXUXixBcwI448g6SHNFPFKEwN8n/uLZBxf4/6s2ljkAsraA==",
            "XmlElementValue": null,
            "NodeIdValue": "http://samples.org/UA/memorybuffer/Instance#i=4010681507",
            "ExpandedNodeIdValue": "http://samples.org/UA/memorybuffer#g=979bd1d7-6e82-4d4e-813c-715d76a51cc9",
            "QualifiedNameValue": "http://samples.org/UA/memorybuffer#%e8%8a%92%e6%9e%9c",
            "LocalizedTextValue": {
                "Text": "黄色 猿, ヒツジ@ 黒 ドラゴン 猿< ラット% ラット* 猿 パイナップル< 白い 黄色 赤い 黄色< 赤い ブタ マンゴ 猫= 象 緑 ブタ",
                "Locale": "jp"
            },
            "StatusCodeValue": 1441792,
            "VariantValue": {
                "Type": "UInt16",
                "Body": 36671
            },
            "EnumerationValue": 0,
            "StructureValue": { "TypeId": null },
            "Number": {
                "Type": "Double",
                "Body": 1.0
            },
            "Integer": {
                "Type": "Int64",
                "Body": 1
            },
            "UInteger": {
                "Type": "UInt64",
                "Body": 1
            }
        }
    },
    {
        "TypeId": "http://test.org/UA/Data/#i=9440",
        "Encoding": "Json",
        "Body": {
            "BooleanValue": false,
            "SByteValue": -113,
            "ByteValue": 42,
            "Int16Value": -14982,
            "UInt16Value": 59442,
            "Int32Value": 85049805,
            "UInt32Value": 2602718263,
            "Int64Value": 3649290182186472621,
            "UInt64Value": 4161862115548090842,
            "FloatValue": -5.763605E-32,
            "DoubleValue": 3.4576486746766018E-34,
            "StringValue": "Виноградина Слон:",
            "DateTimeValue": "1923-03-18T00:11:38.731972Z",
            "GuidValue": "82622490-4f77-4562-6290-1295bf97c2e1",
            "ByteStringValue": "g6SHNFPFKEwN8n/uLZBxf4/6s2ljkAsraA==",
            "XmlElementValue": "PG4wOum7hOiJsiDjg5bjgr/jg6Ljg6I9IlZhY2EiIOOBhOOBoeOBlD0iQ2VyZG8iIOefs+eBsD0iQXLDoW5kYW5vIiDppqw9IlBlcnJvIiB4bWxuczpuMD0iaHR0cDovL+efs+eBsCI+PG4wOue0q+iJsj5Nb25vIFZlcmRlIFV2YSBTZXJwaWVudGUgTW9ubyBBenVsIFBpw7FhIE92ZWphLiBNYW5nbyBMaW1hPC9uMDrntKvoibI+PG4wOuefs+eBsD5NZWxvY290w7NuOyBQZXJybyBBcsOhbmRhbm8gTGltw7NuJmd0OyBBbWFyaWxsbzwvbjA655+z54GwPjxuMDrjg5bjg4njgqY+T3ZlamF+IFBlcnJvIFDDunJwdXJhXiBMaW1hIFJhdGEhIEJsYW5jb18gUMO6cnB1cmE9IEdhdG88L24wOuODluODieOCpj48L24wOum7hOiJsj4=",
            "NodeIdValue": "s=%d0%93%d0%be%d0%bb%d1%83%d0%b1%d0%b8%d0%ba%d0%b0",
            "ExpandedNodeIdValue": "http://test.org/UA/Data/#s=%e9%a9%ac%e7%b4%ab%e8%89%b2",
            "QualifiedNameValue": "http://opcfoundation.org/UA/Diagnostics#Elephant",
            "LocalizedTextValue": {
                "Text": "猪 猴子~ 红色\" 黄色 红色 葡萄 芒果 香蕉 蓝莓 香蕉 芒果? 葡萄 马& 菠萝 白色< 白色 绿色 绿色= 鼠 白色 猪 蓝莓 草莓 猪 狗",
                "Locale": "zh-CN"
            },
            "StatusCodeValue": {
                "Symbol": "GoodShutdownEvent",
                "Code": 11010048
            },
            "VariantValue": {
                "Type": "Int64",
                "Body": 3678050018011977630
            },
            "EnumerationValue": 1,
            "StructureValue": { "TypeId": null },
            "Number": {
                "Type": "Double",
                "Body": 1.0
            },
            "Integer": {
                "Type": "Int64",
                "Body": 1
            },
            "UInteger": {
                "Type": "UInt64",
                "Body": 1
            }
        }
    },
    {
        "TypeId": "http://test.org/UA/Data/#i=9440",
        "Encoding": "Json",
        "Body": {
            "BooleanValue": false,
            "SByteValue": 82,
            "ByteValue": 198,
            "Int16Value": 9215,
            "UInt16Value": 44960,
            "Int32Value": 1970614820,
            "UInt32Value": 4087763535,
            "Int64Value": 3156392098576755738,
            "UInt64Value": 1179071999846299015,
            "FloatValue": -1.2796896E-06,
            "DoubleValue": -2.4084619380135754E-35,
            "StringValue": "ヘビ~ 猫* 緑) マンゴ< レモン ブタ\" 石灰 石灰{ 黒! ブタ 猿 馬 ブタ@ 牛 ヘビ' 犬 犬\" 牛$",
            "DateTimeValue": "1949-12-22T18:46:59.3619463Z",
            "GuidValue": "bfa4b0cc-483b-8dcf-f31c-be1ab6a22373",
            "ByteStringValue": "5k3/MiwysaJQb0S+h/ZadiHED6kKXOEV505s59Gg",
            "XmlElementValue": "PG4wOum7hOiJsiDjg5bjgr/jg6Ljg6I9IlZhY2EiIOOBhOOBoeOBlD0iQ2VyZG8iIOefs+eBsD0iQXLDoW5kYW5vIiDppqw9IlBlcnJvIiB4bWxuczpuMD0iaHR0cDovL+efs+eBsCI+PG4wOue0q+iJsj5Nb25vIFZlcmRlIFV2YSBTZXJwaWVudGUgTW9ubyBBenVsIFBpw7FhIE92ZWphLiBNYW5nbyBMaW1hPC9uMDrntKvoibI+PG4wOuefs+eBsD5NZWxvY290w7NuOyBQZXJybyBBcsOhbmRhbm8gTGltw7NuJmd0OyBBbWFyaWxsbzwvbjA655+z54GwPjxuMDrjg5bjg4njgqY+T3ZlamF+IFBlcnJvIFDDunJwdXJhXiBMaW1hIFJhdGEhIEJsYW5jb18gUMO6cnB1cmE9IEdhdG88L24wOuODluODieOCpj48L24wOum7hOiJsj4=",
            "NodeIdValue": "http://opcfoundation.org/UA/Diagnostics#i=407765665",
            "ExpandedNodeIdValue": "http://opcfoundation.org/UA/Boiler/#g=f16b1f33-7701-a037-4b9b-c936ae51bc40",
            "QualifiedNameValue": "http://opcfoundation.org/UA/Boiler//Instance#%ec%bd%94%eb%81%bc%eb%a6%ac",
            "LocalizedTextValue": {
                "Text": "Персик Пурпурово\" Змейка` Овцы Крыса Пурпурово* Голубо< Бело& Крыса Змейка",
                "Locale": "ru"
            },
            "StatusCodeValue": 4980736,
            "VariantValue": {
                "Type": "SByte",
                "Body": -114
            },
            "EnumerationValue": 1,
            "StructureValue": { "TypeId": null },
            "Number": {
                "Type": "Double",
                "Body": 1.0
            },
            "Integer": {
                "Type": "Int64",
                "Body": 5
            },
            "UInteger": {
                "Type": "UInt64",
                "Body": 1
            }
        }
    },
    {
        "TypeId": "http://test.org/UA/Data/#i=9440",
        "Encoding": "Json",
        "Body": {
            "BooleanValue": false,
            "SByteValue": -97,
            "ByteValue": 121,
            "Int16Value": -29579,
            "UInt16Value": 52214,
            "Int32Value": 150448275,
            "UInt32Value": 2074081332,
            "Int64Value": -1011618571483371166,
            "UInt64Value": 3946747598058890327,
            "FloatValue": 7.858336E+35,
            "DoubleValue": 10017916.0,
            "StringValue": "яблоко Ананас~ Овцы Корова Пурпурово_ Банан Крыса Собака Кот Бело( Корова'",
            "DateTimeValue": "2034-12-05T15:52:28.675232Z",
            "GuidValue": "0500899c-1c30-8180-cbc7-333928152ed2",
            "ByteStringValue": "5xRa2IKDWkNPnQk0znSUOxE=",
            "XmlElementValue": "PG4wOum7hOiJsiDjg5bjgr/jg6Ljg6I9IlZhY2EiIOOBhOOBoeOBlD0iQ2VyZG8iIOefs+eBsD0iQXLDoW5kYW5vIiDppqw9IlBlcnJvIiB4bWxuczpuMD0iaHR0cDovL+efs+eBsCI+PG4wOue0q+iJsj5Nb25vIFZlcmRlIFV2YSBTZXJwaWVudGUgTW9ubyBBenVsIFBpw7FhIE92ZWphLiBNYW5nbyBMaW1hPC9uMDrntKvoibI+PG4wOuefs+eBsD5NZWxvY290w7NuOyBQZXJybyBBcsOhbmRhbm8gTGltw7NuJmd0OyBBbWFyaWxsbzwvbjA655+z54GwPjxuMDrjg5bjg4njgqY+T3ZlamF+IFBlcnJvIFDDunJwdXJhXiBMaW1hIFJhdGEhIEJsYW5jb18gUMO6cnB1cmE9IEdhdG88L24wOuODluODieOCpj48L24wOum7hOiJsj4=",
            "NodeIdValue": "http://opcfoundation.org/UA/Boiler//Instance#s=%e3%83%98%e3%83%93",
            "ExpandedNodeIdValue": "http://opcfoundation.org/UA/Boiler/#i=3489247698",
            "QualifiedNameValue": "DataAccess#%e9%a9%ac",
            "LocalizedTextValue": {
                "Text": "ブタ モモ 緑 いちご ドラゴン 犬; 青い~ モモ 黒; 緑 レモン} 猿% 馬 白い% 馬 牛 象 白い+ 象# いちご< 紫色: レモン~ モモ~ ブタ# マンゴ モモ",
                "Locale": "jp"
            },
            "StatusCodeValue": 1245184,
            "VariantValue": {
                "Type": "ExpandedNodeId",
                "Body": "http://opcfoundation.org/UA/Boiler//Instance#b=4Ncr5uADYkU88S45mg%3d%3d"
            },
            "EnumerationValue": 1,
            "StructureValue": { "TypeId": null },
            "Number": {
                "Type": "Double",
                "Body": 1.0
            },
            "Integer": {
                "Type": "Int64",
                "Body": 1
            },
            "UInteger": {
                "Type": "UInt64",
                "Body": 88
            }
        }
    },
    {
        "TypeId": "http://test.org/UA/Data/#i=9669",
        "Encoding": "Json",
        "Body": {
            "BooleanValue": [ false, false, false, false, true ],
            "SByteValue": [ 120, 27, 27, 57, 117, 61, -42, 106 ],
            "ByteValue": [ 105, 241, 196, 82 ],
            "Int16Value": [ 22001, -1270, 27022, -11160 ],
            "UInt16Value": [ 31406, 22379, 11459, 18140 ],
            "Int32Value": [ 1147924132, 937096171, 293419963, 1355723363, 1682226035, 921241048, 946417831, 483648971, 1150550410 ],
            "UInt32Value": [ 405022290, 3763626854, 2219565007, 635093313, 1150728258 ],
            "Int64Value": [ -1689618757610414770, -1598013270992443575, -6068487195887049228, 3886489167998855712 ],
            "UInt64Value": [ 1434838700748177518, 579235881671951863, 1080167929345915653, 1330943213770414543 ],
            "FloatValue": [ 4.311362E-33, 6.620173E-35, -8.877828E-29, -1.2760576E+29, -1.646687E-22, 1.802464E-21 ],
            "DoubleValue": [ -6011373486080.0, 108132739055616.0, 8.3547184787473483E-36, 5.2422583022226784E+27, 1.0392232580248937E-32, 1.0094077198242765E+33, -8.35414627813762E-39 ],
            "StringValue": [ "녹색 들쭉 들쭉 돼지 녹색% 녹색 암소 원숭이 딸기+ 들쭉 암소~ 망고 망고 딸기 녹색 녹색 돼지 들쭉) 석회 개} 검정 쥐~ 쥐 코끼리= 들쭉", "Обезьяна Красно Зеленыйцвет Крыса", "Красно, Желтыйцвет Манго= Ананас Бело&", "Голубика Желтыйцвет", "蓝莓 大象~ 绵羊 柠檬 母牛 母牛 红色", "狗\" 马 紫色` 葡萄@ 柠檬 芒果 猪 菠萝 龙^ 黑色* 马 绿色 绵羊 大象 红色) 蓝莓 蛇# 狗 香蕉 草莓 黑色@ 红色 鼠~ 蓝色 香蕉 猫 红色% 黑色", "Чернота- Собака Пурпурово# Голубика Чернота Голубо: Дракон яблоко Бело Зеленыйцвет" ],
            "DateTimeValue": [ "1916-05-09T17:48:30.6223191Z" ],
            "GuidValue": [ "842d41a6-6123-30ce-5970-6c26b28dd4de", "9aa488f4-bf70-5b49-848a-9197639e0990", "63306802-aacd-cf0e-20eb-70b1d72bdbe2", "5763cae3-358e-e7ef-f7d1-0098d037cdbb", "4924dc67-715c-4910-79d4-7a844f978358", "99b3afae-b13b-cc01-7949-6bfbaa75ffe9", "fb4ab41e-9107-7285-b919-deb9bb6a975f", "20eaa74e-3383-b67f-da57-d01305159e03" ],
            "ByteStringValue": [ "ZppNiFEdKUHgItJIEQ+yC6wDi99l6zWUIa/Bcm2jetrkKQP9EsZVzPdCU1zjkUbBYPlpm3j1LHtkuGaiXLfUPQ==", "+GToC7X45q6+5yOY2bGPaf8RczrfYe79iJhaX7JwP20VteotXbarAYLtuQ0I44s=", "1jk=", "pWEDx5Z16oIcnof7Tqe1giTGYgtJZXK38qjg9KUNU4g=", "FJoMEu1Tt3Mzj2L78Q==", "ZVNgZ0B0LhI/7kvV7pX23A9L/oI5DahvNnOqmBbWD7wAPHqgRKUT", "SgFaHcYXZSJ8Vn8X/G8xWKvwMMzKlvxp34/UsRpVmGk36zc3soqpHg2HG79W98CRCyL1U3VGSbQF8T43Q7MIJ74=", "3xA3+aUgRxG/Q3o8EufOQqb4YETz8aKCMsFMcdtZfvQAQBivWhE=", "1AxjhwY5yd9WaQANEMd6Iu1utMfj1NY1ZcSGO9HPH+iUe4s3kGqbSGni9QjbTG4thh4qQKVKmAA2LSFBs40Nh0kXeZQ7QpKD0mteAd/NWhlVWbWz", "3kJK4osBYkhaldvUbb7D0tnxQ4unbTnrlyBo0wjsWQ==" ],
            "XmlElementValue": [
                "PG4wOum7hOiJsiDjg5bjgr/jg6Ljg6I9IlZhY2EiIOOBhOOBoeOBlD0iQ2VyZG8iIOefs+eBsD0iQXLDoW5kYW5vIiDppqw9IlBlcnJvIiB4bWxuczpuMD0iaHR0cDovL+efs+eBsCI+PG4wOue0q+iJsj5Nb25vIFZlcmRlIFV2YSBTZXJwaWVudGUgTW9ubyBBenVsIFBpw7FhIE92ZWphLiBNYW5nbyBMaW1hPC9uMDrntKvoibI+PG4wOuefs+eBsD5NZWxvY290w7NuOyBQZXJybyBBcsOhbmRhbm8gTGltw7NuJmd0OyBBbWFyaWxsbzwvbjA655+z54GwPjxuMDrjg5bjg4njgqY+T3ZlamF+IFBlcnJvIFDDunJwdXJhXiBMaW1hIFJhdGEhIEJsYW5jb18gUMO6cnB1cmE9IEdhdG88L24wOuODluODieOCpj48L24wOum7hOiJsj4=",
                "PG4wOum7hOiJsiDjg5bjgr/jg6Ljg6I9IlZhY2EiIOOBhOOBoeOBlD0iQ2VyZG8iIOefs+eBsD0iQXLDoW5kYW5vIiDppqw9IlBlcnJvIiB4bWxuczpuMD0iaHR0cDovL+efs+eBsCI+PG4wOue0q+iJsj5Nb25vIFZlcmRlIFV2YSBTZXJwaWVudGUgTW9ubyBBenVsIFBpw7FhIE92ZWphLiBNYW5nbyBMaW1hPC9uMDrntKvoibI+PG4wOuefs+eBsD5NZWxvY290w7NuOyBQZXJybyBBcsOhbmRhbm8gTGltw7NuJmd0OyBBbWFyaWxsbzwvbjA655+z54GwPjxuMDrjg5bjg4njgqY+T3ZlamF+IFBlcnJvIFDDunJwdXJhXiBMaW1hIFJhdGEhIEJsYW5jb18gUMO6cnB1cmE9IEdhdG88L24wOuODluODieOCpj48L24wOum7hOiJsj4="
            ],
            "NodeIdValue": [ "http://samples.org/UA/memorybuffer#g=8c9312a3-b893-ea53-91d1-2382907eca95", "http://test.org/UA/Data//Instance#b=mZWnGBQiqm%2fQtuce1kejQM%2bdwkrCBDsAWl6ZeX3GfNZshJIz%2fPp%2fauhIgjOqs0w6", "nsu=DataAccess;s=파인애플", "http://test.org/UA/Data//Instance#s=%e8%9b%87%e7%8c%ab%e9%a6%99%e8%95%89", "http://opcfoundation.org/UA/Boiler//Instance#i=272173553", "nsu=DataAccess;b=b0PVHldheYEHVqYSX40/y4R9IYv92lU7yuG4V3n6mgH5hHz6JtoB6X4TUlAXoiijsj61kpDGuJXumVN2qSIIDbul" ],
            "ExpandedNodeIdValue": [ "http://samples.org/UA/memorybuffer/Instance#g=7ca9a545-0c37-87ea-0423-27b914a43b44", "i=2349590220", "urn:manipc1:OPCFoundation:CoreSampleServer#g=94450a5c-8972-934d-6c99-0c1659b9ce0e", "http://test.org/UA/Data//Instance#s=%e6%a1%83%e5%ad%90", "http://opcfoundation.org/UA/Diagnostics#g=290ee634-1839-f122-f6b0-df426fb19e6b", "urn:manipc1:OPCFoundation:CoreSampleServer#i=2014900536", "http://opcfoundation.org/UA/Boiler//Instance#s=%ec%84%9d%ed%9a%8c", "http://test.org/UA/Data//Instance#b=4D2jPmkygekkYgnuy3rDjlEURSuQwxxtEVEYAMgjS9Cjxg%3d%3d", "http://opcfoundation.org/UA/Diagnostics#g=2005172c-cc4e-6fb5-0e0c-a653cb7c979a", "i=405616161" ],
            "QualifiedNameValue": [ "DataAccess#%eb%b0%94%eb%82%98%eb%82%98", "http://samples.org/UA/memorybuffer#%d0%9a%d0%be%d1%80%d0%be%d0%b2%d0%b0", "%eb%b0%b1%ec%83%89", "http://samples.org/UA/memorybuffer/Instance#%e3%83%90%e3%83%8a%e3%83%8a", "DataAccess#%e3%83%91%e3%82%a4%e3%83%8a%e3%83%83%e3%83%97%e3%83%ab", "http://opcfoundation.org/UA/Boiler//Instance#Mango" ],
            "LocalizedTextValue": [
                {
                    "Text": "Black~ Pig' Red Black' Lime! Black} Purple Blue Cat Strawberry:",
                    "Locale": "en-US"
                },
                {
                    "Text": "蓝色 芒果 猫 紫色. 鼠; 紫色 紫色 蛇 芒果 葡萄 狗' 母牛",
                    "Locale": "zh-CN"
                },
                {
                    "Text": "草莓, 绵羊 龙 白色{ 白色} 大象, 绿色% 葡萄 菠萝) 蛇 香蕉} 蓝色' 猪 大象' 大象` 芒果^ 猫= 黄色 母牛(",
                    "Locale": "zh-CN"
                },
                {
                    "Text": "绵羊< 石灰/ 母牛: 大象",
                    "Locale": "zh-CN"
                },
                {
                    "Text": "Овцы яблоко# Желтыйцвет Лимон( Змейка Собака Корова? Крыса Змейка> Лошадь Лошадь",
                    "Locale": "ru"
                },
                {
                    "Text": "菠萝' 草莓. 狗 红色: 蛇, 菠萝 龙 猴子/ 菠萝$ 柠檬# 草莓. 蓝莓= 猫 菠萝< 柠檬: 狗 大象 石灰 马= 葡萄( 芒果/ 鼠;",
                    "Locale": "zh-CN"
                },
                {
                    "Text": "Dog) Cat( Strawberry` Cat Monkey Elephant Horse! Grape- Peach Monkey} Blueberry! Red",
                    "Locale": "en-US"
                },
                {
                    "Text": "Snake Grape Mango",
                    "Locale": "en-US"
                },
                {
                    "Text": "蛇, 大象@ 红色 桃子+ 鼠 红色 紫色 草莓 菠萝",
                    "Locale": "zh-CN"
                },
                {
                    "Text": "Кот Крыса Слон Свинья' Голубика Пурпурово@ Дракон- Обезьяна? Бело(",
                    "Locale": "ru"
                }
            ],
            "StatusCodeValue": [ 6225920, 7995392, 5832704, 6553600, 3997696, 1900544, 8519680 ],
            "VariantValue": [
                {
                    "Type": "Byte",
                    "Body": 82
                }
            ],
            "EnumerationValue": [],
            "StructureValue": [],
            "Number": [],
            "Integer": [],
            "UInteger": []
        }
    },
    {
        "TypeId": "http://test.org/UA/Data/#i=9440",
        "Encoding": "Json",
        "Body": {
            "BooleanValue": true,
            "SByteValue": -56,
            "ByteValue": 104,
            "Int16Value": 3814,
            "UInt16Value": 38042,
            "Int32Value": 535350820,
            "UInt32Value": 3693060540,
            "Int64Value": -2577172637598593213,
            "UInt64Value": 1118748778070163278,
            "FloatValue": 1.931257E+17,
            "DoubleValue": -0.00033564501791261137,
            "StringValue": "ラット} 馬` いちご 青い 白い 象 レモン. パイナップル",
            "DateTimeValue": "1985-11-03T22:42:37.1296614Z",
            "GuidValue": "5b9f4a59-1a25-042a-e156-e9e08f8eed3d",
            "ByteStringValue": "wEYr6R2tv2YG6q2Z",
            "XmlElementValue": "PG4wOum7hOiJsiDjg5bjgr/jg6Ljg6I9IlZhY2EiIOOBhOOBoeOBlD0iQ2VyZG8iIOefs+eBsD0iQXLDoW5kYW5vIiDppqw9IlBlcnJvIiB4bWxuczpuMD0iaHR0cDovL+efs+eBsCI+PG4wOue0q+iJsj5Nb25vIFZlcmRlIFV2YSBTZXJwaWVudGUgTW9ubyBBenVsIFBpw7FhIE92ZWphLiBNYW5nbyBMaW1hPC9uMDrntKvoibI+PG4wOuefs+eBsD5NZWxvY290w7NuOyBQZXJybyBBcsOhbmRhbm8gTGltw7NuJmd0OyBBbWFyaWxsbzwvbjA655+z54GwPjxuMDrjg5bjg4njgqY+T3ZlamF+IFBlcnJvIFDDunJwdXJhXiBMaW1hIFJhdGEhIEJsYW5jb18gUMO6cnB1cmE9IEdhdG88L24wOuODluODieOCpj48L24wOum7hOiJsj4=",
            "NodeIdValue": "http://test.org/UA/Data//Instance#i=2103396786",
            "ExpandedNodeIdValue": "http://test.org/UA/Data//Instance#i=577318642",
            "QualifiedNameValue": "DataAccess#%e7%8a%ac%e3%83%96%e3%82%bf",
            "LocalizedTextValue": {
                "Text": "Red Green Lemon# Elephant Dog Horse Monkey: Lime' Strawberry Monkey",
                "Locale": "en-US"
            },
            "StatusCodeValue": 1638400,
            "VariantValue": {
                "Type": "ExpandedNodeId",
                "Body": "http://samples.org/UA/memorybuffer#i=1429871234"
            },
            "EnumerationValue": 1,
            "StructureValue": { "TypeId": null },
            "Number": {
                "Type": "Double",
                "Body": 1.0
            },
            "Integer": {
                "Type": "Int64",
                "Body": 33
            },
            "UInteger": {
                "Type": "UInt64",
                "Body": 1
            }
        }
    }
]

""");
            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "ExtensionObject"
            }, ct).ConfigureAwait(false);

            // Assert

            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayNumberValueVariableTest1Async(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10324";

            var values = _generator.GetRandomArray<sbyte>();
            var expected = _serializer.FromObject(values
                .Select(v => new
                {
                    Type = "SByte",
                    Body = v
                }));

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Number"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, _serializer.FromObject(values), result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayNumberValueVariableTest2Async(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10324";
            var values = _generator.GetRandomArray<sbyte>();
            var expected = _serializer.FromObject(values);

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Number"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayIntegerValueVariableTest1Async(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10325";

            var values = _generator.GetRandomArray<int>();
            var expected = _serializer.FromObject(values
                .Select(v => new
                {
                    Type = "Int32",
                    Body = v
                }));

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Integer"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, _serializer.FromObject(values), result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayIntegerValueVariableTest2Async(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10325";
            var values = _generator.GetRandomArray<int>();
            var expected = _serializer.FromObject(values);

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "Integer"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayUIntegerValueVariableTest1Async(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10326";

            var values = _generator.GetRandomArray<ushort>();
            var expected = _serializer.FromObject(values
                .Select(v => new
                {
                    Type = "UInt16",
                    Body = v
                }));

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "UInteger"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, _serializer.FromObject(values), result).ConfigureAwait(false);
        }

        public async Task NodeWriteStaticArrayUIntegerValueVariableTest2Async(CancellationToken ct = default)
        {
            var browser = _services();
            const string node = "http://test.org/UA/Data/#i=10326";
            var values = _generator.GetRandomArray<ushort>();
            var expected = _serializer.FromObject(values);

            // Act
            var result = await browser.ValueWriteAsync(_connection, new ValueWriteRequestModel
            {
                NodeId = node,
                Value = expected,
                DataType = "UInteger"
            }, ct).ConfigureAwait(false);

            // Assert
            await AssertResultAsync(node, expected, result).ConfigureAwait(false);
        }

        private async Task AssertResultAsync(string node, VariantValue expected,
            ValueWriteResponseModel result)
        {
            var value = await _readExpected(_connection, node, _serializer).ConfigureAwait(false);
            Assert.NotNull(value);
            Assert.Null(result.ErrorInfo);
            Assert.True(expected.Equals(value), $"{expected} != {value}");
            Assert.Equal(expected, value);
        }

        private readonly T _connection;
        private readonly Func<T, string, IJsonSerializer, Task<VariantValue>> _readExpected;
        private readonly Func<INodeServices<T>> _services;
        private readonly DefaultJsonSerializer _serializer;
        private readonly Opc.Ua.Test.TestDataGenerator _generator = new();
    }
}
