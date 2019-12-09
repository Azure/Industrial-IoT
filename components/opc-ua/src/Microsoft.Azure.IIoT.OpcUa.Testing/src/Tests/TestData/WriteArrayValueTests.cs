// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Testing.Tests {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Services;
    using Microsoft.Azure.IIoT.OpcUa.Twin;
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json.Linq;
    using Opc.Ua;
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Xml;
    using Xunit;

    public class WriteArrayValueTests<T> {

        /// <summary>
        /// Create node services tests
        /// </summary>
        /// <param name="services"></param>
        /// <param name="endpoint"></param>
        /// <param name="readExpected"></param>
        public WriteArrayValueTests(Func<INodeServices<T>> services, T endpoint,
            Func<T, string, Task<JToken>> readExpected) {
            _services = services;
            _endpoint = endpoint;
            _readExpected = readExpected;
        }

        public async Task NodeWriteStaticArrayBooleanValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10300";

            var expected = JToken.Parse(
                "[true,true,true,false,false,false,true,true,true,false,true," +
                "false,false,false,true,false,false,false,false,true,false,true," +
                "true,true,true,false]");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Boolean"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArraySByteValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10301";

            var expected = JToken.Parse(
                "[-94,94,62,22,-50,36,105,103,-60,56,-102,-14,-59,-83,119,-101," +
                "-39,85,-9,-14,-7,-100,64,122,-107,-61,13,-10,-19,81,-52,57," +
                "-32,-90,27,-128,92,44,-32,13,-93,-10,46,9,-38,55,116,-11,-43," +
                "63,-45,-103,2]");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "SByte"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayByteValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10302";

            var expected = JToken.Parse(
                "\"jgYexIAKF3N6c2tgEh6R9j+tdOlOAm43n15OFyGtfjI2VhgVYpis1fYvfL" +
                "qdeiRVY94AJSUZ\"");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "ByteString"
                    // TODO: Assert.Equal("Byte", result.DataType);
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }



        public async Task NodeWriteStaticArrayInt16ValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10303";

            var expected = JToken.FromObject(_generator.GetRandomArray<short>());

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Int16"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayUInt16ValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10304";

            var expected = JToken.FromObject(_generator.GetRandomArray<ushort>());

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "UInt16"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayInt32ValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10305";

            var expected = JToken.FromObject(_generator.GetRandomArray<int>());

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Int32"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayUInt32ValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10306";

            var expected = JToken.FromObject(_generator.GetRandomArray<uint>());

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "UInt32"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayInt64ValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10307";

            var expected = JToken.FromObject(_generator.GetRandomArray<long>());

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Int64"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayUInt64ValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10308";

            var expected = JToken.FromObject(_generator.GetRandomArray<ulong>());

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "UInt64"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayFloatValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10309";

            var expected = JToken.FromObject(new float[] {
                float.NaN,
                0.0f,
                1.0f,
                0.0034f,
                2543.354f,
                float.MaxValue,
                float.MinValue
            });

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Float"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayDoubleValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10310";

            var expected = JToken.FromObject(new double[] {
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
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Double"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayStringValueVariableTest1Async() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10311";

            var expected = JToken.FromObject(new string[] {
                "test",
                "test2",
                "test3",
                "test4,",
                "Test",
                "TEST"
            });

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "String"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayStringValueVariableTest2Async() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10311";

            var expected = JToken.FromObject(_generator.GetRandomArray<string>());

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "String"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayDateTimeValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10312";

            var expected = JToken.FromObject(_generator.GetRandomArray<DateTime>());

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "DateTime"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayGuidValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10313";

            var expected = JToken.FromObject(_generator.GetRandomArray<Guid>());

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Guid"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayByteStringValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10314";

            var expected = JToken.Parse(
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
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "ByteString"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayXmlElementValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10315";

            var expected = JToken.FromObject(_generator.GetRandomArray<XmlElement>());

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "XmlElement"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayNodeIdValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10316";

            var expected = JToken.Parse(
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
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "NodeId"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayExpandedNodeIdValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10317";

            var expected = JToken.Parse(
                "[\"http://samples.org/UA/memorybuffer/Instance#i=2144658193\"," +
                "\"http://samples.org/UA/memorybuffer#b=c9PGBcMJ%2fXaDHbdQAdVi15q4Vd" +
                    "F0s64Z2BzAQUguTWwH3T4OSRPSoA%2fZs0gCG%2fs8gfzkzVk8yr8krC2nSLstV" +
                    "dBLSCSWVI2H5rTBm%2f9mFrwhhMA%3d\", " +
                "\"http://opcfoundation.org/UA/Boiler/#g=7e12cb12-9cea-2be5-5753-ab5e78b7d3d7\"]");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "ExpandedNodeId"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayQualifiedNameValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10318";

            var expected = JToken.FromObject(new string[] {
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
                "afsdff",
            });

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "QualifiedName"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayLocalizedTextValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10319";

            var expected = JToken.Parse("[" +
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
            var result = await browser.NodeValueWriteAsync(_endpoint,
            new ValueWriteRequestModel {
                NodeId = node,
                Value = expected,
                DataType = "LocalizedText"
            });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayStatusCodeValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10320";

            var expected = JToken.Parse("[2555904,9306112]");

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "StatusCode"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayVariantValueVariableTest1Async() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10321";

            var encoder = new JsonVariantEncoder();
            var values = _generator.GetRandomArray<string>();
            var expected = new JArray(values
                .Select(delegate (object v) {
                    var body = encoder.Encode(new Variant(v), out var t, null);
                    return JToken.FromObject(new {
                        Type = t.ToString(),
                        Body = body
                    });
                }));

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Variant"
                });

            // Assert
            await AssertResultAsync(node, new JArray(values), result);
        }


        public async Task NodeWriteStaticArrayEnumerationValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10322";

            var expected = JToken.Parse(
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
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Int32"
                    // Assert.Equal("Enumeration", result.DataType);
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayStructureValueVariableTestAsync() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10323";

            var val = JToken.Parse(
                "[{\"TypeId\":\"http://test.org/UA/Data/#i=11437\",\"Body\":\"" +
                "Adpudx/+2bGKSg8czglDBHYSsICXC1fz3LN1x1FoeRUjL24AAADgbpNLwCY" +
                "AAADroIjrqqwg65S46riwXiDqs6DslpHsnbQpIO2MjOyduOyVoO2UjO9rQ8m" +
                "4mg8C4mQbL2yb+Z+by2gaUDCRCwwAAABOGpBvKOFRt6UEBtGTAgAAPG4wOue" +
                "3keeKrOODieODqeOCtOODsyDjg5bjg6vjg7zjg5njg6rjg7w9IuyVlOyGjOu" +
                "nkCIg6Z2S44GEPSLtmansg4nqsJwiIOmmrOmmrD0i7JWU7IaMIiDjg5Djg4r" +
                "jg4o9IuuTpOytiSIg44OR44Kk44OK44OD44OX44OrPSLrlLjquLDsvZTrgbz" +
                "rpqwiIOixoT0i67GAIiB4bWxuczpuMD0iaHR0cDovL+eMqyI+PG4wOuODqeO" +
                "Dg+ODiD7sm5DsiK3snbQpIOqygOyglSsg7Zmp7IOJPSDrp53qs6Ag66CI66q" +
                "sKiDsnpDso7zsg4kkIOugiOuqrCs8L24wOuODqeODg+ODiD48bjA644OW44O" +
                "r44O844OZ44Oq44O8PuuwlOuCmOuCmCDsm5DsiK3snbQ7IOuxgD0g7JaRKCD" +
                "slpEg7ISd7ZqMIO2ZqeyDiSEg7YyM656APyDspZAjIOuxgCDrp53qs6BAIOu" +
                "wlOuCmOuCmCDrs7XsiK3slYRgIOuzteyIreyVhCDslpFdPC9uMDrjg5bjg6v" +
                "jg7zjg5njg6rjg7w+PG4wOuOBhOOBoeOBlD7rs7XsiK3slYQg64W57IOJIOu" +
                "5qOqwhCZndDsg7KWQXyDqs6DslpHsnbQg64+87KeAIOuFueyDiScg64W57IO" +
                "JPyDrhbnsg4lbIOuwlOuCmOuCmCDroIjrqqwhIOqwnF8g64W57IOJYCDqsoD" +
                "soJUg7KWQIOyaqSZndDsg67mo6rCEIOuxgCZhbXA7IOunneqzoD8g7Y+s64+" +
                "EIOugiOuqrCDroIjrqqwg6rKA7KCVKiDrp53qs6A8L24wOuOBhOOBoeOBlD4" +
                "8L24wOue3keeKrOODieODqeOCtOODsz4FBwAMAAAA672G6bOkm2X9OQ4VhAA" +
                "Ah9lpuGo5GFB+TVVtXlkVhxwAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmc" +
                "vVUEvBgAGAAAARHJhZ29uAwUAAAB6aC1DTmcAAADmr43niZtAIOibhyDpqaw" +
                "tIOiTneiOkyDnjLTlrZA8IOe7v+iJsiDom4d7IOeZveiJsiQg57u1576KIOe" +
                "7tee+iiDntKvoibIg57Sr6ImyIOeMtOWtkFsg54y05a2QISDok53ojpMoAAB" +
                "hABYEAwARKqaige7iEceX8BXw3Me/AS4AAAD56OTsms+E4p9rECTcuepVghu" +
                "fn86HXXWQEfkdwBzjpq1G523+JY/WHp5PdwGjAAAAAAAAAAsAAAAAAAAAAAg" +
                "AAAAAAAAAAAkAAAAAAAAAAA==\"},{\"TypeId\":\"http://test.org/U" +
                "A/Data/#i=11437\",\"Body\":\"AbmlDoV/6h/uvTuFc3GKbluJ1mYig4d" +
                "pdpXSfVddi8mALzgAAADAfK69Qh8AAADQk9C+0LvRg9Cx0LjQutCwPiDQlNG" +
                "A0LDQutC+0L1AALgFqtsVzwHwmCwlZK1D/SBWBEM5o/tuUgAAABNz/MFP4k7" +
                "DXJrHlIXIx7Ns9tvAL11F4sQXMCOOPD+UWtLsRYAKoLbeYggTe2iajTyQzK0" +
                "grgS2LRn9OLEqNB9bMzqy24IzYVUk4c1VMmdbfKL6AwAAPG4wOuuzteyIrey" +
                "VhCDqsoDsoJXtmansg4k9ItCa0L7RgiIg66eQ7JaRPSLQntCx0LXQt9GM0Y/" +
                "QvdCwIiDrp53qs6Drk6TsrYk9ItCe0LLRhtGLIiDshJ3tmow9ItCb0L7RiNC" +
                "w0LTRjCIg7Y+s64+EPSLQl9C10LvQtdC90YvQudGG0LLQtdGCIiB4bWxuczp" +
                "uMD0iaHR0cDovL+uPvOyngOunkCI+PG4wOuyVlOyGjD7Ql9C80LXQudC60LA" +
                "7INCh0L7QsdCw0LrQsCDQmtC+0YDQvtCy0LAg0KHQstC40L3RjNGPKSDQmNC" +
                "30LLQtdGB0YLQutCwINCT0L7Qu9GD0LHQuNC60LAqPC9uMDrslZTshow+PG4" +
                "wOuuTpOytiT7QltC10LvRgtGL0LnRhtCy0LXRgmAg0JTRgNCw0LrQvtC9QCD" +
                "QmtC+0YIhINCX0LXQu9C10L3Ri9C50YbQstC10YIg0JLQuNC90L7Qs9GA0LD" +
                "QtNC40L3QsEAg0JrQvtGA0L7QstCwINCX0LzQtdC50LrQsCDQmtC+0YI8L24" +
                "wOuuTpOytiT48bjA667Cx7IOJPtCa0YDRi9GB0LAg0JrQvtGCINCS0LjQvdC" +
                "+0LPRgNCw0LTQuNC90LAg0KHQu9C+0L0uINCb0LjQvNC+0L0mZ3Q7INCf0LX" +
                "RgNGB0LjQuiDQntCx0LXQt9GM0Y/QvdCwOiDRj9Cx0LvQvtC60L4g0KHQstC" +
                "40L3RjNGPLSDQp9C10YDQvdC+0YLQsCDQktC40L3QvtCz0YDQsNC00LjQvdC" +
                "wINCQ0L3QsNC90LDRgV4g0Y/QsdC70L7QutC+ITwvbjA667Cx7IOJPjxuMDr" +
                "rsLHsg4nrk6TsrYk+0JvQuNC80L7QvSDQn9GD0YDQv9GD0YDQvtCy0L4g0JD" +
                "QvdCw0L3QsNGBJmFtcDsg0JfQvNC10LnQutCwINCf0LXRgNGB0LjQujog0JT" +
                "RgNCw0LrQvtC9INCh0LvQvtC9INCY0LfQstC10YHRgtC60LAlINCc0LDQvdC" +
                "z0L4jINCU0YDQsNC60L7QvSk8L24wOuuwseyDieuTpOytiT48bjA66rKA7KC" +
                "VPtCT0L7Qu9GD0LHQuNC60LAuINCa0YDRi9GB0LAg0JvQuNC80L7QvX0g0Jb" +
                "QtdC70YLRi9C50YbQstC10YJ9INCR0LDQvdCw0L0g0JrQvtGCINCa0L7RgNC" +
                "+0LLQsC0g0J7QstGG0Ysg0JHQsNC90LDQvS4g0JLQuNC90L7Qs9GA0LDQtNC" +
                "40L3QsCDQntCx0LXQt9GM0Y/QvdCwfiDQkdCw0L3QsNC9fjwvbjA66rKA7KC" +
                "VPjwvbjA667O17Iit7JWEPgIIAKMkDu+EAADX0ZuXgm5OTYE8cV12pRzJIgA" +
                "AAGh0dHA6Ly9zYW1wbGVzLm9yZy9VQS9tZW1vcnlidWZmZXIHAAYAAADoipL" +
                "mnpwDAgAAAGpwowAAAOm7hOiJsiDnjL8sIOODkuODhOOCuEAg6buSIOODieO" +
                "DqeOCtOODsyDnjL88IOODqeODg+ODiCUg44Op44OD44OIKiDnjL8g44OR44K" +
                "k44OK44OD44OX44OrPCDnmb3jgYQg6buE6ImyIOi1pOOBhCDpu4ToibI8IOi" +
                "1pOOBhCDjg5bjgr8g44Oe44Oz44K0IOeMqz0g6LGhIOe3kSDjg5bjgr8AABY" +
                "ABT+PAAAAAAAAAAsAAAAAAAAAAAgAAAAAAAAAAAkAAAAAAAAAAA==\"},{\"" +
                "TypeId\":\"http://test.org/UA/Data/#i=11437\",\"Body\":\"AI8" +
                "qesUy6M3BEQU3VCKbrZSL/xXipDLaUWYpi+fBObqhlYsAAAAAm7n8OCAAAAD" +
                "QktC40L3QvtCz0YDQsNC00LjQvdCwINCh0LvQvtC9OqhJq+O1O2kBkCRignd" +
                "PYkVikBKVv5fC4RkAAACDpIc0U8UoTA3yf+4tkHF/j/qzaWOQCyto+AUAADx" +
                "uMDrnn7PngbAgeG1sbnM6bjA9Imh0dHA6Ly/ni5fnu7XnvooiPjxuMDrpvpn" +
                "nu7Xnvoo+54yrKiDjg6njg4Pjg4gmbHQ7IOeJmyDnmb3jgYReIOODqeODg+O" +
                "DiCDpu5InIOeJm1sg6buSfiDjgYTjgaHjgZQg44Os44Oi44OzQCDppqwg44O" +
                "s44Oi44OzKSDjg57jg7PjgrQg55+z54GwWyDjg5bjgr9bIOefs+eBsCkg6aa" +
                "sIOe3kSDnn7PngbAg44OW44Or44O844OZ44Oq44O8IOODmOODkyDjg57jg7P" +
                "jgrQg57Sr6ImyLzwvbjA66b6Z57u1576KPjxuMDrmoYPlrZA+44OW44OJ44K" +
                "mIOODnuODs+OCtCZndDsg6buE6ImyKCDnjL8g44OR44Kk44OK44OD44OX44O" +
                "rIOODieODqeOCtOODsyDjg4njg6njgrTjg7MqIOODieODqeOCtOODs0Ag44O" +
                "J44Op44K044OzIOODkuODhOOCuCDjg5HjgqTjg4rjg4Pjg5fjg6sg44OJ44O" +
                "p44K044OzIOe0q+iJsl0g44OR44Kk44OK44OD44OX44OrJmd0OyDjg5HjgqT" +
                "jg4rjg4Pjg5fjg6s8L24wOuahg+WtkD48bjA66aaZ6JWJPuixoT8g57eRIOi" +
                "xoSkg44OJ44Op44K044OzIOixoSDjg4njg6njgrTjg7MjIOeJmyog44Os44O" +
                "i44OzIOODmOODkyDppqwsIOm7kigg44OR44Kk44OK44OD44OX44OrIOeMqyU" +
                "g55m944GEWzwvbjA66aaZ6JWJPjxuMDrojYnojpM+55+z54GwIOeMqyDnjL9" +
                "dIOeKrCDpu5Ig6buSIOODmOODk1sg6buE6ImyfiDpu5IvIOm7hOiJsiDjg6n" +
                "jg4Pjg4goIOODkeOCpOODiuODg+ODl+ODqyDnjL9dIOeMq2Ag55+z54GwIOm" +
                "7hOiJskAg44OW44Or44O844OZ44Oq44O8WyDjg5Ljg4Tjgrgg6aasOiDnjKs" +
                "pIOeJmyDpu5Ig44Os44Oi44OzKDwvbjA66I2J6I6TPjxuMDrok53ojpM+55m" +
                "944GEIOe3kV8g44OY44OTJmd0OyDpu4ToibIg44GE44Gh44GUIOeMvyDniZs" +
                "g54yrIOODluODieOCpiDniqx7IOeZveOBhHs8L24wOuiTneiOkz48bjA66bu" +
                "E6ImyPui1pOOBhCUg55m944GEIiDniqwg44OY44OTJmd0OyDjg4njg6njgrT" +
                "jg7MjIOe0q+iJsiDniZsg44OW44Or44O844OZ44Oq44O8IOODmOODkyDnn7P" +
                "ngbAg44OQ44OK44OKYCDniqwlIOODmOODk34g44OW44OJ44KmIOm7hOiJsi0" +
                "g44OQ44OK44OKQCDnn7PngbB7IOODluODq+ODvOODmeODquODvCDjg5Ljg4T" +
                "jgrgg44OJ44Op44K044OzIOixoTwvbjA66buE6ImyPjxuMDroj6DokJ3lpKf" +
                "osaE+6aasIOm7hOiJsiDjg5bjgr8g44Oi44OiPyDjg5Djg4rjg4o7PC9uMDr" +
                "oj6DokJ3lpKfosaE+PG4wOummmeiViT7otaTjgYQg44OW44K/IOODluOCvyD" +
                "njKssIOeJm10g44OW44K/IOm7ki4g44OQ44OK44OKJyDppqwmbHQ7IOODkuO" +
                "DhOOCuCDjg5jjg5Mg6aasIOODouODoiDjg57jg7PjgrRbIOe0q+iJsiDjg5b" +
                "jg6vjg7zjg5njg6rjg7xeIOeKrCDjg5HjgqTjg4rjg4Pjg5fjg6tfPC9uMDr" +
                "pppnolYk+PG4wOue6ouiJsj7pnZLjgYQg44Oi44OiOiDjg5bjg4njgqYg44O" +
                "Y44OTIOefs+eBsEAg44OS44OE44K4IOODmOODk10g6aasIOODqeODg+ODiCD" +
                "jg5bjgr8g54yrIOODkuODhOOCuCDniqwtIOm7kicg44Oi44OiXyDjg5Ljg4T" +
                "jgrgg6Z2S44GEIOe0q+iJsiU8L24wOue6ouiJsj48L24wOuefs+eBsD4DAAA" +
                "QAAAA0JPQvtC70YPQsdC40LrQsIMAAAkAAADpqazntKvoibIYAAAAaHR0cDo" +
                "vL3Rlc3Qub3JnL1VBL0RhdGEvBgAIAAAARWxlcGhhbnQDBQAAAHpoLUNOogA" +
                "AAOeMqiDnjLTlrZB+IOe6ouiJsiIg6buE6ImyIOe6ouiJsiDokaHokIQg6Iq" +
                "S5p6cIOmmmeiViSDok53ojpMg6aaZ6JWJIOiKkuaenD8g6JGh6JCEIOmprCY" +
                "g6I+g6JCdIOeZveiJsjwg55m96ImyIOe7v+iJsiDnu7/oibI9IOm8oCDnmb3" +
                "oibIg54yqIOiTneiOkyDojYnojpMg54yqIOeLlwAAqAAInhe5FQEPCzMAAAA" +
                "AAAAACwAAAAAAAAAACAAAAAAAAAAACQAAAAAAAAAA\"},{\"TypeId\":\"h" +
                "ttp://test.org/UA/Data/#i=11437\",\"Body\":\"AFLG/yOgryQydXV" +
                "PUqbzGmyE7+PBzStKF3tiXg2ho83Bq7UAAAAgyQHAuHMAAADjg5jjg5N+IOe" +
                "Mqyog57eRKSDjg57jg7PjgrQ8IOODrOODouODsyDjg5bjgr8iIOefs+eBsCD" +
                "nn7PngbB7IOm7kiEg44OW44K/IOeMvyDppqwg44OW44K/QCDniZsg44OY44O" +
                "TJyDniqwg54qsIiDniZskBw7NWVU+hwHMsKS/O0jPjfMcvhq2oiNzNQAAAOZ" +
                "N/zIsMrGiUG9Evof2WnYhxA+pClzhFedObOfRoN9B2pEhwERJoH7yHOrZi85" +
                "sJmOklN0U1AEAADxuMDrpppnolYnoipLmnpwg6IqS5p6cPSJEb2ciIOm7hOi" +
                "Jsj0iQ2F0IiDom4foipLmnpw9IlBlYWNoIiDoj6DokJ3nuqLoibI9IlNuYWt" +
                "lIiDpppnolYk9Ik1hbmdvIiDojYnojpPntKvoibI9IkRvZyIg5q+N54mb5qG" +
                "D5a2QPSJIb3JzZSIg55m96Imy6IqS5p6cPSJTaGVlcCIgeG1sbnM6bjA9Imh" +
                "0dHA6Ly/pqazpu5HoibIiPjxuMDrni5fnn7PngbA+RG9nIFB1cnBsZX4gTW9" +
                "ua2V5XyBEb2cpIFllbGxvdyBMZW1vbi8gQ293OyBDYXQgTGltZT88L24wOue" +
                "Ll+efs+eBsD48bjA66ams57Sr6ImyPkJsdWViZXJyeSsgUGVhY2h+IFB1cnB" +
                "sZSBCYW5hbmEgQmx1ZWJlcnJ5IEdyYXBlIEdyZWVuLCBNYW5nbyBCYW5hbmE" +
                "gU2hlZXAuIE1vbmtleS0gWWVsbG93JzwvbjA66ams57Sr6ImyPjxuMDroipL" +
                "mnpw+Q293IENvdyBQaW5lYXBwbGVgIERvZyZsdDsgSG9yc2UgQ2F0Jmx0Ozw" +
                "vbjA66IqS5p6cPjwvbjA66aaZ6JWJ6IqS5p6cPgIGAKECThiEAAAzH2vxAXc" +
                "3oEubyTauUbxAIwAAAGh0dHA6Ly9vcGNmb3VuZGF0aW9uLm9yZy9VQS9Cb2l" +
                "sZXIvBAAJAAAA7L2U64G866asAwIAAABydYYAAADQn9C10YDRgdC40Log0J/" +
                "Rg9GA0L/Rg9GA0L7QstC+IiDQl9C80LXQudC60LBgINCe0LLRhtGLINCa0YD" +
                "Ri9GB0LAg0J/Rg9GA0L/Rg9GA0L7QstC+KiDQk9C+0LvRg9Cx0L48INCR0LX" +
                "Qu9C+JiDQmtGA0YvRgdCwINCX0LzQtdC50LrQsAAATAACjgAAAAAAAAALAAA" +
                "AAAAAAAAIAAAAAAAAAAAJAAAAAAAAAAA=\"},{\"TypeId\":\"http://te" +
                "st.org/UA/Data/#i=11437\",\"Body\":\"AJ95dYz2y5Oo9wg0+J97Yqm" +
                "L3kQC9vFXJGHdiTspfJZYF3sAAACAjxtjQYYAAADRj9Cx0LvQvtC60L4g0JD" +
                "QvdCw0L3QsNGBfiDQntCy0YbRiyDQmtC+0YDQvtCy0LAg0J/Rg9GA0L/Rg9G" +
                "A0L7QstC+XyDQkdCw0L3QsNC9INCa0YDRi9GB0LAg0KHQvtCx0LDQutCwINC" +
                "a0L7RgiDQkdC10LvQvigg0JrQvtGA0L7QstCwJ0DObbzufOYBnIkABTAcgIH" +
                "LxzM5KBUu0hEAAADnFFrYgoNaQ0+dCTTOdJQ7EYIDAAA8bjA60JrQvtGA0L7" +
                "QstCwINCW0LXQu9GC0YvQudGG0LLQtdGCPSJTdHJhd2JlcnJ5IiDQk9C+0Lv" +
                "Rg9Cx0LjQutCwPSJQaWciINCn0LXRgNC90L7RgtCwPSJZZWxsb3ciINCa0L7" +
                "RgNC+0LLQsD0iU2hlZXAiINCe0LLRhtGLPSJHcmVlbiIg0Y/QsdC70L7QutC" +
                "+PSJCbHVlIiB4bWxuczpuMD0iaHR0cDovL9Cc0LDQvdCz0L4iPjxuMDrQmtG" +
                "A0LDRgdC90L4+Qmx1ZSBZZWxsb3d+IE1hbmdvIFJhdCBSYXQgRG9nIExpbWU" +
                "7IEhvcnNlIERvZyo8L24wOtCa0YDQsNGB0L3Qvj48bjA60JHQsNC90LDQvT5" +
                "CbGFjaycgQ2F0ISBFbGVwaGFudCBQaW5lYXBwbGV+IFNuYWtlIFBpbmVhcHB" +
                "sZT8gQmx1ZSQgU2hlZXAqIEVsZXBoYW50IiBSYXQ8L24wOtCR0LDQvdCw0L0" +
                "+PG4wOtCb0LjQvNC+0L0+UGluZWFwcGxlXiBSYXQgQmFuYW5hJmd0OyBHcmF" +
                "wZSBDb3cgQmxhY2sjIFNuYWtlPC9uMDrQm9C40LzQvtC9PjxuMDrQn9GD0YD" +
                "Qv9GD0YDQvtCy0L4+UHVycGxlIEhvcnNlIE1vbmtleSBTdHJhd2JlcnJ5Jmx" +
                "0OyBFbGVwaGFudHsgU3RyYXdiZXJyeVsgV2hpdGV7IFJhdF4gV2hpdGVgIFN" +
                "0cmF3YmVycnk/PC9uMDrQn9GD0YDQv9GD0YDQvtCy0L4+PG4wOtCa0YDRi9G" +
                "B0LA+TWFuZ28/IEJsdWU7IENvdyZndDsgTGVtb24gSG9yc2UjIEJsdWUgRHJ" +
                "hZ29ufiBQdXJwbGUgU25ha2UmbHQ7IFB1cnBsZSBZZWxsb3deIExpbWUgUmF" +
                "0IEdyYXBlfjwvbjA60JrRgNGL0YHQsD48bjA60JrQvtGCPlNoZWVwXSBIb3J" +
                "zZVsgQmxhY2snIENhdDsgU3RyYXdiZXJyeSBMaW1lXyBNb25rZXkmZ3Q7IFN" +
                "oZWVwIFBpbmVhcHBsZTogU2hlZXAgUGlnPSBSZWQgUmVkJSBTaGVlcCQ8L24" +
                "wOtCa0L7Rgj48bjA60JPQvtC70YPQsdC40LrQsD5HcmVlbn48L24wOtCT0L7" +
                "Qu9GD0LHQuNC60LA+PC9uMDrQmtC+0YDQvtCy0LA+AwQABgAAAOODmOODk4I" +
                "AANKx+c8jAAAAaHR0cDovL29wY2ZvdW5kYXRpb24ub3JnL1VBL0JvaWxlci8" +
                "FAAMAAADpqawDAgAAAGpwuQAAAOODluOCvyDjg6Ljg6Ig57eRIOOBhOOBoeO" +
                "BlCDjg4njg6njgrTjg7Mg54qsOyDpnZLjgYR+IOODouODoiDpu5I7IOe3kSD" +
                "jg6zjg6Ljg7N9IOeMvyUg6aasIOeZveOBhCUg6aasIOeJmyDosaEg55m944G" +
                "EKyDosaEjIOOBhOOBoeOBlDwg57Sr6ImyOiDjg6zjg6Ljg7N+IOODouODon4" +
                "g44OW44K/IyDjg57jg7PjgrQg44Oi44OiAAATABKFAAANAAAA4Ncr5uADYkU" +
                "88S45miwAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvQm9pbGVyLy9" +
                "JbnN0YW5jZQAAAAAAAAALAAAAAAAAAAAIAAAAAAAAAAAJAAAAAAAAAAA=\"}" +
                ",{\"TypeId\":\"http://test.org/UA/Data/#i=11438\",\"Body\":\"" +
                "BQAAAAAAAAABCAAAAHgbGzl1PdZqBAAAAGnxxFIEAAAA8VUK+45paNQEAAA" +
                "ArnprV8Ms3EYJAAAApO5rROvz2je7O30RY7LOUHO7RGTYBek2pzBpOMvl0xy" +
                "KAZREBQAAAFImJBhmY1Tgz99LhEHB2iVCuJZEBAAAAE5dyyKxRI3oSacijGW" +
                "30un0ZSUtDGfIqyCq+YlOle81BAAAAE8EyFj2tR/H94U3KPfbCQg7TlNwFkb" +
                "nlR8CBkoDmrS4BgAAAEUWswmQ/q8GZhThkIIozu+BEkebxDAIHQcAAAAAAAA" +
                "gh96VwgAAAGAnlthCAAAAQO01pjgAAABgS/CwRQAAAMDS+ko5AAAAIELiyEY" +
                "AAACA/b0GuAcAAACyAAAA64W57IOJIOuTpOytiSDrk6TsrYkg64+87KeAIOu" +
                "FueyDiSUg64W57IOJIOyVlOyGjCDsm5DsiK3snbQg65S46riwKyDrk6TsrYk" +
                "g7JWU7IaMfiDrp53qs6Ag66ed6rOgIOuUuOq4sCDrhbnsg4kg64W57IOJIOu" +
                "PvOyngCDrk6TsrYkpIOyEne2ajCDqsJx9IOqygOyglSDspZB+IOylkCDsvZT" +
                "rgbzrpqw9IOuTpOytiT8AAADQntCx0LXQt9GM0Y/QvdCwINCa0YDQsNGB0L3" +
                "QviDQl9C10LvQtdC90YvQudGG0LLQtdGCINCa0YDRi9GB0LBFAAAA0JrRgNC" +
                "w0YHQvdC+LCDQltC10LvRgtGL0LnRhtCy0LXRgiDQnNCw0L3Qs9C+PSDQkNC" +
                "90LDQvdCw0YEg0JHQtdC70L4mJQAAANCT0L7Qu9GD0LHQuNC60LAg0JbQtdC" +
                "70YLRi9C50YbQstC10YIxAAAA6JOd6I6TIOWkp+ixoX4g57u1576KIOafoOa" +
                "qrCDmr43niZsg5q+N54mbIOe6ouiJsrIAAADni5ciIOmprCDntKvoibJgIOi" +
                "RoeiQhEAg5p+g5qqsIOiKkuaenCDnjKog6I+g6JCdIOm+mV4g6buR6ImyKiD" +
                "pqawg57u/6ImyIOe7tee+iiDlpKfosaEg57qi6ImyKSDok53ojpMg6JuHIyD" +
                "ni5cg6aaZ6JWJIOiNieiOkyDpu5HoibJAIOe6ouiJsiDpvKB+IOiTneiJsiD" +
                "pppnolYkg54yrIOe6ouiJsiUg6buR6ImymAAAANCn0LXRgNC90L7RgtCwLSD" +
                "QodC+0LHQsNC60LAg0J/Rg9GA0L/Rg9GA0L7QstC+IyDQk9C+0LvRg9Cx0Lj" +
                "QutCwINCn0LXRgNC90L7RgtCwINCT0L7Qu9GD0LHQvjog0JTRgNCw0LrQvtC" +
                "9INGP0LHQu9C+0LrQviDQkdC10LvQviDQl9C10LvQtdC90YvQudGG0LLQtdG" +
                "CAQAAAFfgu62ijGEBCAAAAKZBLYQjYc4wWXBsJrKN1N70iKSacL9JW4SKkZd" +
                "jngmQAmgwY82qDs8g63Cx1yvb4uPKY1eONe/n99EAmNA3zbtn3CRJXHEQSXn" +
                "UeoRPl4NYrq+zmTuxAcx5SWv7qnX/6R60SvsHkYVyuRneubtql19Op+oggzN" +
                "/ttpX0BMFFZ4DCgAAAEAAAABmmk2IUR0pQeAi0kgRD7ILrAOL32XrNZQhr8F" +
                "ybaN62uQpA/0SxlXM90JTXOORRsFg+WmbePUse2S4ZqJct9Q9LwAAAPhk6Au" +
                "1+OauvucjmNmxj2n/EXM632Hu/YiYWl+ycD9tFbXqLV22qwGC7bkNCOOLAgA" +
                "AANY5IAAAAKVhA8eWdeqCHJ6H+06ntYIkxmILSWVyt/Ko4PSlDVOIDQAAABS" +
                "aDBLtU7dzM49i+/EnAAAAZVNgZ0B0LhI/7kvV7pX23A9L/oI5DahvNnOqmBb" +
                "WD7wAPHqgRKUTQQAAAEoBWh3GF2UifFZ/F/xvMVir8DDMypb8ad+P1LEaVZh" +
                "pN+s3N7KKqR4Nhxu/VvfAkQsi9VN1Rkm0BfE+N0OzCCe+JgAAAN8QN/mlIEc" +
                "Rv0N6PBLnzkKm+GBE8/GigjLBTHHbWX70AEAYr1oRVAAAANQMY4cGOcnfVmk" +
                "ADRDHeiLtbrTH49TWNWXEhjvRzx/olHuLN5Bqm0hp4vUI20xuLYYeKkClSpg" +
                "ANi0hQbONDYdJF3mUO0KSg9JrXgHfzVoZVVm1sx8AAADeQkriiwFiSFqV29R" +
                "tvsPS2fFDi6dtOeuXIGjTCOxZAgAAAAEEAAA8bjA6RWxlcGhhbnQgTW9ua2V" +
                "5PSLsm5DsiK3snbQiIEhvcnNlPSLshJ3tmowiIE1hbmdvPSLroIjrqqwiIEJ" +
                "sYWNrPSLqs6DslpHsnbQiIEdyYXBlPSLrk6TsrYnspZAiIHhtbG5zOm4wPSJ" +
                "odHRwOi8vQmxhY2siPjxuMDpCbHVlPuylkCDrhbnsg4kkIO2ZqeyDiX4g7L2" +
                "U64G866asLCDqsJwmZ3Q7IOybkOyIreydtCog7L2U64G866asIyDsm5DsiK3" +
                "snbQ6IOunneqzoCgg67GAIOuTpOytiS0g64+87KeALCDrs7XsiK3slYQg67G" +
                "AIO2MjOyduOyVoO2UjH0g7YyM7J247JWg7ZSMIOylkC4g66CI66qsIOuxgCD" +
                "tmansg4ktIOuTpOytiSDspZAhIOyaqX0g66eQJSDrs7XsiK3slYQg7L2U64G" +
                "866asfSDrj7zsp4A8L24wOkJsdWU+PG4wOlNoZWVwPuyekOyjvOyDiS8g7L2" +
                "U64G866asIOy9lOuBvOumrCDrj7zsp4Ag67GAIOybkOyIreydtCDtmansg4l" +
                "7IOuFueyDiSZhbXA7IOuzteyIreyVhCDrsYAg7JWU7IaMIOqzoOyWkeydtCk" +
                "g6rOg7JaR7J20IOu5qOqwhCDrp5BdIOuzteyIreyVhCZsdDsg6rOg7JaR7J2" +
                "0IOuPvOyngF8g7L2U64G866asKCDsmqkg7L2U64G866asIOuTpOytiSDrp5B" +
                "9IOuPvOyngCDshJ3tmowg7JuQ7Iit7J20JzwvbjA6U2hlZXA+PG4wOlBpbmV" +
                "hcHBsZT7slZTshowg64+87KeAfSDspZAkIOuTpOytiS0g7Zmp7IOJIOuxgCD" +
                "qs6DslpHsnbQg64W57IOJLiDrsJTrgpjrgpgjIOy9lOuBvOumrD8g6rOg7Ja" +
                "R7J20IOunkCDsvZTrgbzrpqwqIO2MjOuegCDtjIzsnbjslaDtlIw8L24wOlB" +
                "pbmVhcHBsZT48bjA6Qmx1ZT7qsJwg6rKA7KCVIyDrk6TsrYkg66ed6rOgIOu" +
                "zteyIreyVhCDrp53qs6AkIOuFueyDiSDtj6zrj4QmZ3Q7IOylkCDshJ3tmow" +
                "8L24wOkJsdWU+PG4wOkRvZz7tjIzsnbjslaDtlIwiIOy9lOuBvOumrCDslZT" +
                "showg67O17Iit7JWEIO2PrOuPhCDshJ3tmowiIOunkCDtj6zrj4Qg7J6Q7KO" +
                "87IOJXiDtjIzsnbjslaDtlIxgIO2PrOuPhD8g6rOg7JaR7J20IOqygOyglSo" +
                "g67mo6rCEQCDrsJTrgpjrgpgpIOuUuOq4sCZhbXA7IOuTpOytiSk8L24wOkR" +
                "vZz48L24wOkVsZXBoYW50Pt4BAAA8bjA655+z54Gw6JGh6JCEIOiTneiJsj0" +
                "i6JGh6JCE6amsIiDnjKrpvpnnu7/oibI9IuavjeeJmyIg54y05a2QPSLmoYP" +
                "lrZDnjKsiIOmprOeLlz0i6JuH6IqS5p6cIiDok53ojpM9IuiPoOiQnSIg54y" +
                "qPSLntKvoibLmoYPlrZAiIHhtbG5zOm4wPSJodHRwOi8v6I+g6JCd6JGh6JC" +
                "EIj48bjA654yq6IqS5p6cPuiPoOiQnSDom4cqIOeMqiEg6aaZ6JWJLzwvbjA" +
                "654yq6IqS5p6cPjxuMDrnjKs+57u/6ImyIOm7hOiJsicg6amsIOm8oCDntKv" +
                "oibIvIOiKkuaenCZsdDsg57u/6ImyJSDpu5HoibIg6buR6ImyPyDnn7PngbA" +
                "g5p+g5qqsIOWkp+ixoSDni5cg6aaZ6JWJIOiPoOiQnSDpppnolYkmZ3Q7IOW" +
                "kp+ixoScg57qi6ImyKjwvbjA654yrPjxuMDroipLmnpznuqLoibI+55+z54G" +
                "wLDwvbjA66IqS5p6c57qi6ImyPjxuMDrntKvoibLnu7/oibI+57u1576KIOa" +
                "hg+WtkHsg5qGD5a2QPC9uMDrntKvoibLnu7/oibI+PC9uMDrnn7PngbDokaH" +
                "okIQ+BgAAAAQHAKMSk4yTuFPqkdEjgpB+ypUFAwAwAAAAmZWnGBQiqm/Qtuc" +
                "e1kejQM+dwkrCBDsAWl6ZeX3GfNZshJIz/Pp/auhIgjOqs0w6AwUADAAAAO2" +
                "MjOyduOyVoO2UjAMDAAwAAADom4fnjKvpppnolYkCBADxCTkQBQUAQgAAAG9" +
                "D1R5XYXmBB1amEl+NP8uEfSGL/dpVO8rhuFd5+poB+YR8+ibaAel+E1JQF6I" +
                "oo7I+tZKQxriV7plTdqkiCA27pQoAAACEAABFpal8NwzqhwQjJ7kUpDtEKwA" +
                "AAGh0dHA6Ly9zYW1wbGVzLm9yZy9VQS9tZW1vcnlidWZmZXIvSW5zdGFuY2W" +
                "CAADM5guMHAAAAGh0dHA6Ly9vcGNmb3VuZGF0aW9uLm9yZy9VQS+EAABcCkW" +
                "UcolNk2yZDBZZuc4OKgAAAHVybjptYW5pcGMxOk9QQ0ZvdW5kYXRpb246Q29" +
                "yZVNhbXBsZVNlcnZlcoMAAAYAAADmoYPlrZAhAAAAaHR0cDovL3Rlc3Qub3J" +
                "nL1VBL0RhdGEvL0luc3RhbmNlhAAANOYOKTkYIvH2sN9Cb7GeaycAAABodHR" +
                "wOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvRGlhZ25vc3RpY3OCAAA48Rh4KgA" +
                "AAHVybjptYW5pcGMxOk9QQ0ZvdW5kYXRpb246Q29yZVNhbXBsZVNlcnZlcoM" +
                "AAAYAAADshJ3tmowsAAAAaHR0cDovL29wY2ZvdW5kYXRpb24ub3JnL1VBL0J" +
                "vaWxlci8vSW5zdGFuY2WFAAAiAAAA4D2jPmkygekkYgnuy3rDjlEURSuQwxx" +
                "tEVEYAMgjS9CjxiEAAABodHRwOi8vdGVzdC5vcmcvVUEvRGF0YS8vSW5zdGF" +
                "uY2WEAAAsFwUgTsy1bw4MplPLfJeaJwAAAGh0dHA6Ly9vcGNmb3VuZGF0aW9" +
                "uLm9yZy9VQS9EaWFnbm9zdGljc4IAACE2LRgcAAAAaHR0cDovL29wY2ZvdW5" +
                "kYXRpb24ub3JnL1VBLwcAAAABAAwAAADQl9C80LXQudC60LAFAAkAAADrsJT" +
                "rgpjrgpgHAAwAAADQmtC+0YDQvtCy0LAAAAYAAADrsLHsg4kIAAkAAADjg5D" +
                "jg4rjg4oFABIAAADjg5HjgqTjg4rjg4Pjg5fjg6sEAAUAAABNYW5nbwoAAAA" +
                "DBQAAAGVuLVVTPwAAAEJsYWNrfiBQaWcnIFJlZCBCbGFjaycgTGltZSEgQmx" +
                "hY2t9IFB1cnBsZSBCbHVlIENhdCBTdHJhd2JlcnJ5OgMFAAAAemgtQ05KAAA" +
                "A6JOd6ImyIOiKkuaenCDnjKsg57Sr6ImyLiDpvKA7IOe0q+iJsiDntKvoibI" +
                "g6JuHIOiKkuaenCDokaHokIQg54uXJyDmr43niZsDBQAAAHpoLUNOhQAAAOi" +
                "NieiOkywg57u1576KIOm+mSDnmb3oibJ7IOeZveiJsn0g5aSn6LGhLCDnu7/" +
                "oibIlIOiRoeiQhCDoj6DokJ0pIOibhyDpppnolYl9IOiTneiJsicg54yqIOW" +
                "kp+ixoScg5aSn6LGhYCDoipLmnpxeIOeMqz0g6buE6ImyIOavjeeJmygDBQA" +
                "AAHpoLUNOHgAAAOe7tee+ijwg55+z54GwLyDmr43niZs6IOWkp+ixoQMCAAA" +
                "AcnWSAAAA0J7QstGG0Ysg0Y/QsdC70L7QutC+IyDQltC10LvRgtGL0LnRhtC" +
                "y0LXRgiDQm9C40LzQvtC9KCDQl9C80LXQudC60LAg0KHQvtCx0LDQutCwINC" +
                "a0L7RgNC+0LLQsD8g0JrRgNGL0YHQsCDQl9C80LXQudC60LA+INCb0L7RiNC" +
                "w0LTRjCDQm9C+0YjQsNC00YwDBQAAAHpoLUNOkwAAAOiPoOiQnScg6I2J6I6" +
                "TLiDni5cg57qi6ImyOiDom4csIOiPoOiQnSDpvpkg54y05a2QLyDoj6DokJ0" +
                "kIOafoOaqrCMg6I2J6I6TLiDok53ojpM9IOeMqyDoj6DokJ08IOafoOaqrDo" +
                "g54uXIOWkp+ixoSDnn7PngbAg6amsPSDokaHokIQoIOiKkuaenC8g6bygOwM" +
                "FAAAAZW4tVVNUAAAARG9nKSBDYXQoIFN0cmF3YmVycnlgIENhdCBNb25rZXk" +
                "gRWxlcGhhbnQgSG9yc2UhIEdyYXBlLSBQZWFjaCBNb25rZXl9IEJsdWViZXJ" +
                "yeSEgUmVkAwUAAABlbi1VUxEAAABTbmFrZSBHcmFwZSBNYW5nbwMFAAAAemg" +
                "tQ047AAAA6JuHLCDlpKfosaFAIOe6ouiJsiDmoYPlrZArIOm8oCDnuqLoibI" +
                "g57Sr6ImyIOiNieiOkyDoj6DokJ0DAgAAAHJ1dwAAANCa0L7RgiDQmtGA0Yv" +
                "RgdCwINCh0LvQvtC9INCh0LLQuNC90YzRjycg0JPQvtC70YPQsdC40LrQsCD" +
                "Qn9GD0YDQv9GD0YDQvtCy0L5AINCU0YDQsNC60L7QvS0g0J7QsdC10LfRjNG" +
                "P0L3QsD8g0JHQtdC70L4oBwAAAAAAXwAAAHoAAABZAAAAZAAAAD0AAAAdAAA" +
                "AggABAAAAA1IAAAAAAAAAAAAAAAAAAAAAAAAAAA==\"},{\"TypeId\":\"h" +
                "ttp://test.org/UA/Data/#i=11437\",\"Body\":\"Acho5g6alCTO6B+" +
                "8oR/cQ08U+yMLPNwPeb7Z3PBBm72HK1wAAABgMP81v0kAAADjg6njg4Pjg4h" +
                "9IOmmrGAg44GE44Gh44GUIOmdkuOBhCDnmb3jgYQg6LGhIOODrOODouODsy4" +
                "g44OR44Kk44OK44OD44OX44OrZjX+n3x0rwFZSp9bJRoqBOFW6eCPju09DAA" +
                "AAMBGK+kdrb9mBuqtmVACAAA8bjA657u1576KIOeMq+iPoOiQnT0i6aasIiD" +
                "pu5HoibI9IueMvyIg6JOd6ImyPSLppqznt5Hjg57jg7PjgrQiIOm+meWkp+i" +
                "xoT0i44OQ44OK44OKIiDoj6DokJ3nmb3oibI9IumdkuOBhCIgeG1sbnM6bjA" +
                "9Imh0dHA6Ly/ok53oibIiPjxuMDrom4fntKvoibI+44GE44Gh44GUIiDnjL8" +
                "g6buSJmx0OyDjg57jg7PjgrQg6aasIOeMqzog44OY44OTPyDpu5Ig54yrKSD" +
                "jg6Ljg6IlIOODluODieOCpiDosaEg6buE6ImyeyDjg6zjg6Ljg7Mg57Sr6Im" +
                "yIOODkuODhOOCuDsg44OW44OJ44KmIOeMqygg6LWk44GELyDjg57jg7PjgrQ" +
                "nIOODkOODiuODiiDjg5bjg6vjg7zjg5njg6rjg7w8L24wOuibh+e0q+iJsj4" +
                "8bjA657u/6ImyPuefs+eBsCDpu4ToibIuIOODieODqeOCtOODsyEg44OY44O" +
                "TIOODluODq+ODvOODmeODquODvCDjg6njg4Pjg4gmbHQ7IOmmrCDpu5Ig57e" +
                "RKSDppqwg44Op44OD44OIIOeMqyDjg6Ljg6I8L24wOue7v+iJsj48bjA65p+" +
                "g5qqsPuODluOCvzog6LWk44GEIOefs+eBsCMg44Oi44OiPC9uMDrmn6Dmqqw" +
                "+PG4wOumprD7jg6njg4Pjg4gkIOeKrFsg54mbIOm7knsg6Z2S44GEIOm7kiD" +
                "nt5F9PC9uMDrpqaw+PC9uMDrnu7Xnvoo+AgMAsklffYIAAPIuaSIhAAAAaHR" +
                "0cDovL3Rlc3Qub3JnL1VBL0RhdGEvL0luc3RhbmNlBQAJAAAA54qs44OW44K" +
                "/AwUAAABlbi1VU0MAAABSZWQgR3JlZW4gTGVtb24jIEVsZXBoYW50IERvZyB" +
                "Ib3JzZSBNb25rZXk6IExpbWUnIFN0cmF3YmVycnkgTW9ua2V5AAAZABKCAAC" +
                "CGjpVIgAAAGh0dHA6Ly9zYW1wbGVzLm9yZy9VQS9tZW1vcnlidWZmZXIAAAA" +
                "AAAAACwAAAAAAAAAACAAAAAAAAAAACQAAAAAAAAAA\"}]");

            var expected = JToken.Parse(@"
[
    {
        ""TypeId"": ""http://test.org/UA/Data/#i=11437"",
        ""Encoding"": ""Json"",
        ""Body"": {
            ""BooleanValue"": true,
            ""SByteValue"": -38,
            ""ByteValue"": 110,
            ""Int16Value"": 8055,
            ""UInt16Value"": 55806,
            ""Int32Value"": 256543409,
            ""UInt32Value"": 1124716060,
            ""Int64Value"": 6272273485009155588,
            ""UInt64Value"": 8748332193282252019,
            ""FloatValue"": 1.35505721E+28,
            ""DoubleValue"": -55.151821136474609,
            ""StringValue"": ""레몬 딸기^ 고양이) 파인애플"",
            ""DateTimeValue"": ""2071-08-08T14:25:16.7814639Z"",
            ""GuidValue"": ""2f1b64e2-9b6c-9ff9-9bcb-681a5030910b"",
            ""ByteStringValue"": [ 78, 26, 144, 111, 40, 225, 81, 183, 165, 4, 6, 209 ],
            ""XmlElementValue"": {
                ""n0:緑犬ドラゴン"": {
                    ""@ブルーベリー"": ""암소말"",
                    ""@青い"": ""황색개"",
                    ""@馬馬"": ""암소"",
                    ""@バナナ"": ""들쭉"",
                    ""@パイナップル"": ""딸기코끼리"",
                    ""@象"": ""뱀"",
                    ""@xmlns:n0"": ""http://猫"",
                    ""n0:ラット"": ""원숭이) 검정+ 황색= 망고 레몬* 자주색$ 레몬+"",
                    ""n0:ブルーベリー"": ""바나나 원숭이; 뱀= 양( 양 석회 황색! 파란? 쥐# 뱀 망고@ 바나나 복숭아` 복숭아 양]"",
                    ""n0:いちご"": ""복숭아 녹색 빨간> 쥐_ 고양이 돼지 녹색' 녹색? 녹색[ 바나나 레몬! 개_ 녹색` 검정 쥐 용> 빨간 뱀& 망고? 포도 레몬 레몬 검정* 망고""
                }
            },
            ""NodeIdValue"": ""http://samples.org/UA/memorybuffer#b=672G6bOkm2X9OQ4V"",
            ""ExpandedNodeIdValue"": ""http://opcfoundation.org/UA/#g=b869d987-396a-5018-7e4d-556d5e591587"",
            ""QualifiedNameValue"": ""http://opcfoundation.org/UA/Diagnostics#Dragon"",
            ""LocalizedTextValue"": {
                ""Text"": ""母牛@ 蛇 马- 蓝莓 猴子< 绿色 蛇{ 白色$ 绵羊 绵羊 紫色 紫色 猴子[ 猴子! 蓝莓("",
                ""Locale"": ""zh-CN""
            },
            ""StatusCodeValue"": 6356992,
            ""VariantValue"": {
                ""Type"": ""ExtensionObject"",
                ""Body"": {
                    ""TypeId"": ""http://test.org/UA/Data//Instance#g=a2a62a11-ee81-11e2-c797-f015f0dcc7bf"",
                    ""Body"": ""+ejk7JrPhOKfaxAk3LnqVYIbn5/Oh111kBH5HcAc46atRudt/iWP1h6eT3cBow==""
                }
            },
            ""EnumerationValue"": 0,
            ""StructureValue"": { ""TypeId"": null },
            ""Number"": {
                ""Type"": ""Double"",
                ""Body"": 0.0
            },
            ""Integer"": {
                ""Type"": ""Int64"",
                ""Body"": 0
            },
            ""UInteger"": {
                ""Type"": ""UInt64"",
                ""Body"": 0
            }
        }
    },
    {
        ""TypeId"": ""http://test.org/UA/Data/#i=11437"",
        ""Encoding"": ""Json"",
        ""Body"": {
            ""BooleanValue"": true,
            ""SByteValue"": -71,
            ""ByteValue"": 165,
            ""Int16Value"": -31474,
            ""UInt16Value"": 60031,
            ""Int32Value"": 1002303007,
            ""UInt32Value"": 2322690949,
            ""Int64Value"": -8682057831558849682,
            ""UInt64Value"": 10042278942021613161,
            ""FloatValue"": 4.18431919E-05,
            ""DoubleValue"": 32635254472704.0,
            ""StringValue"": ""Голубика> Дракон@"",
            ""DateTimeValue"": ""2014-01-20T12:32:21.1556352Z"",
            ""GuidValue"": ""252c98f0-ad64-fd43-2056-044339a3fb6e"",
            ""ByteStringValue"": [ 19, 115, 252, 193, 79, 226, 78, 195, 92, 154, 199, 148, 133, 200, 199, 179, 108, 246, 219, 192, 47, 93, 69, 226, 196, 23, 48, 35, 142, 60, 63, 148, 90, 210, 236, 69, 128, 10, 160, 182, 222, 98, 8, 19, 123, 104, 154, 141, 60, 144, 204, 173, 32, 174, 4, 182, 45, 25, 253, 56, 177, 42, 52, 31, 91, 51, 58, 178, 219, 130, 51, 97, 85, 36, 225, 205, 85, 50, 103, 91, 124, 162 ],
            ""XmlElementValue"": {
                ""n0:복숭아"": {
                    ""@검정황색"": ""Кот"",
                    ""@말양"": ""Обезьяна"",
                    ""@망고들쭉"": ""Овцы"",
                    ""@석회"": ""Лошадь"",
                    ""@포도"": ""Зеленыйцвет"",
                    ""@xmlns:n0"": ""http://돼지말"",
                    ""n0:암소"": ""Змейка; Собака Корова Свинья) Известка Голубика*"",
                    ""n0:들쭉"": ""Желтыйцвет` Дракон@ Кот! Зеленыйцвет Виноградина@ Корова Змейка Кот"",
                    ""n0:백색"": ""Крыса Кот Виноградина Слон. Лимон> Персик Обезьяна: яблоко Свинья- Чернота Виноградина Ананас^ яблоко!"",
                    ""n0:백색들쭉"": ""Лимон Пурпурово Ананас& Змейка Персик: Дракон Слон Известка% Манго# Дракон)"",
                    ""n0:검정"": ""Голубика. Крыса Лимон} Желтыйцвет} Банан Кот Корова- Овцы Банан. Виноградина Обезьяна~ Банан~""
                }
            },
            ""NodeIdValue"": ""http://samples.org/UA/memorybuffer/Instance#i=4010681507"",
            ""ExpandedNodeIdValue"": ""http://samples.org/UA/memorybuffer#g=979bd1d7-6e82-4d4e-813c-715d76a51cc9"",
            ""QualifiedNameValue"": ""http://samples.org/UA/memorybuffer#%e8%8a%92%e6%9e%9c"",
            ""LocalizedTextValue"": {
                ""Text"": ""黄色 猿, ヒツジ@ 黒 ドラゴン 猿< ラット% ラット* 猿 パイナップル< 白い 黄色 赤い 黄色< 赤い ブタ マンゴ 猫= 象 緑 ブタ"",
                ""Locale"": ""jp""
            },
            ""StatusCodeValue"": 1441792,
            ""VariantValue"": {
                ""Type"": ""UInt16"",
                ""Body"": 36671
            },
            ""EnumerationValue"": 0,
            ""StructureValue"": { ""TypeId"": null },
            ""Number"": {
                ""Type"": ""Double"",
                ""Body"": 0.0
            },
            ""Integer"": {
                ""Type"": ""Int64"",
                ""Body"": 0
            },
            ""UInteger"": {
                ""Type"": ""UInt64"",
                ""Body"": 0
            }
        }
    },
    {
        ""TypeId"": ""http://test.org/UA/Data/#i=11437"",
        ""Encoding"": ""Json"",
        ""Body"": {
            ""BooleanValue"": false,
            ""SByteValue"": -113,
            ""ByteValue"": 42,
            ""Int16Value"": -14982,
            ""UInt16Value"": 59442,
            ""Int32Value"": 85049805,
            ""UInt32Value"": 2602718263,
            ""Int64Value"": 3649290182186472621,
            ""UInt64Value"": 4161862115548090842,
            ""FloatValue"": -5.763605E-32,
            ""DoubleValue"": 3.4576486746766018E-34,
            ""StringValue"": ""Виноградина Слон:"",
            ""DateTimeValue"": ""1923-03-18T00:11:38.731972Z"",
            ""GuidValue"": ""82622490-4f77-4562-6290-1295bf97c2e1"",
            ""ByteStringValue"": [ 131, 164, 135, 52, 83, 197, 40, 76, 13, 242, 127, 238, 45, 144, 113, 127, 143, 250, 179, 105, 99, 144, 11, 43, 104 ],
            ""XmlElementValue"": {
                ""n0:石灰"": {
                    ""@xmlns:n0"": ""http://狗绵羊"",
                    ""n0:龙绵羊"": ""猫* ラット< 牛 白い^ ラット 黒' 牛[ 黒~ いちご レモン@ 馬 レモン) マンゴ 石灰[ ブタ[ 石灰) 馬 緑 石灰 ブルーベリー ヘビ マンゴ 紫色/"",
                    ""n0:桃子"": ""ブドウ マンゴ> 黄色( 猿 パイナップル ドラゴン ドラゴン* ドラゴン@ ドラゴン ヒツジ パイナップル ドラゴン 紫色] パイナップル> パイナップル"",
                    ""n0:香蕉"": [ ""象? 緑 象) ドラゴン 象 ドラゴン# 牛* レモン ヘビ 馬, 黒( パイナップル 猫% 白い["", ""赤い ブタ ブタ 猫, 牛] ブタ 黒. バナナ' 馬< ヒツジ ヘビ 馬 モモ マンゴ[ 紫色 ブルーベリー^ 犬 パイナップル_"" ],
                    ""n0:草莓"": ""石灰 猫 猿] 犬 黒 黒 ヘビ[ 黄色~ 黒/ 黄色 ラット( パイナップル 猿] 猫` 石灰 黄色@ ブルーベリー[ ヒツジ 馬: 猫) 牛 黒 レモン("",
                    ""n0:蓝莓"": ""白い 緑_ ヘビ> 黄色 いちご 猿 牛 猫 ブドウ 犬{ 白い{"",
                    ""n0:黄色"": ""赤い% 白い\"" 犬 ヘビ> ドラゴン# 紫色 牛 ブルーベリー ヘビ 石灰 バナナ` 犬% ヘビ~ ブドウ 黄色- バナナ@ 石灰{ ブルーベリー ヒツジ ドラゴン 象"",
                    ""n0:菠萝大象"": ""馬 黄色 ブタ モモ? バナナ;"",
                    ""n0:红色"": ""青い モモ: ブドウ ヘビ 石灰@ ヒツジ ヘビ] 馬 ラット ブタ 猫 ヒツジ 犬- 黒' モモ_ ヒツジ 青い 紫色%""
                }
            },
            ""NodeIdValue"": ""s=%d0%93%d0%be%d0%bb%d1%83%d0%b1%d0%b8%d0%ba%d0%b0"",
            ""ExpandedNodeIdValue"": ""http://test.org/UA/Data/#s=%e9%a9%ac%e7%b4%ab%e8%89%b2"",
            ""QualifiedNameValue"": ""http://opcfoundation.org/UA/Diagnostics#Elephant"",
            ""LocalizedTextValue"": {
                ""Text"": ""猪 猴子~ 红色\"" 黄色 红色 葡萄 芒果 香蕉 蓝莓 香蕉 芒果? 葡萄 马& 菠萝 白色< 白色 绿色 绿色= 鼠 白色 猪 蓝莓 草莓 猪 狗"",
                ""Locale"": ""zh-CN""
            },
            ""StatusCodeValue"": {
                ""Symbol"": ""GoodShutdownEvent"",
                ""Code"": 11010048
            },
            ""VariantValue"": {
                ""Type"": ""Int64"",
                ""Body"": 3678050018011977630
            },
            ""EnumerationValue"": 0,
            ""StructureValue"": { ""TypeId"": null },
            ""Number"": {
                ""Type"": ""Double"",
                ""Body"": 0.0
            },
            ""Integer"": {
                ""Type"": ""Int64"",
                ""Body"": 0
            },
            ""UInteger"": {
                ""Type"": ""UInt64"",
                ""Body"": 0
            }
        }
    },
    {
        ""TypeId"": ""http://test.org/UA/Data/#i=11437"",
        ""Encoding"": ""Json"",
        ""Body"": {
            ""BooleanValue"": false,
            ""SByteValue"": 82,
            ""ByteValue"": 198,
            ""Int16Value"": 9215,
            ""UInt16Value"": 44960,
            ""Int32Value"": 1970614820,
            ""UInt32Value"": 4087763535,
            ""Int64Value"": 3156392098576755738,
            ""UInt64Value"": 11790719998462990154,
            ""FloatValue"": -1.27968963E-06,
            ""DoubleValue"": -2.4084619380135754E-35,
            ""StringValue"": ""ヘビ~ 猫* 緑) マンゴ< レモン ブタ\"" 石灰 石灰{ 黒! ブタ 猿 馬 ブタ@ 牛 ヘビ' 犬 犬\"" 牛$"",
            ""DateTimeValue"": ""1949-12-22T18:46:59.3619463Z"",
            ""GuidValue"": ""bfa4b0cc-483b-8dcf-f31c-be1ab6a22373"",
            ""ByteStringValue"": [ 230, 77, 255, 50, 44, 50, 177, 162, 80, 111, 68, 190, 135, 246, 90, 118, 33, 196, 15, 169, 10, 92, 225, 21, 231, 78, 108, 231, 209, 160, 223, 65, 218, 145, 33, 192, 68, 73, 160, 126, 242, 28, 234, 217, 139, 206, 108, 38, 99, 164, 148, 221, 20 ],
            ""XmlElementValue"": {
                ""n0:香蕉芒果"": {
                    ""@芒果"": ""Dog"",
                    ""@黄色"": ""Cat"",
                    ""@蛇芒果"": ""Peach"",
                    ""@菠萝红色"": ""Snake"",
                    ""@香蕉"": ""Mango"",
                    ""@草莓紫色"": ""Dog"",
                    ""@母牛桃子"": ""Horse"",
                    ""@白色芒果"": ""Sheep"",
                    ""@xmlns:n0"": ""http://马黑色"",
                    ""n0:狗石灰"": ""Dog Purple~ Monkey_ Dog) Yellow Lemon/ Cow; Cat Lime?"",
                    ""n0:马紫色"": ""Blueberry+ Peach~ Purple Banana Blueberry Grape Green, Mango Banana Sheep. Monkey- Yellow'"",
                    ""n0:芒果"": ""Cow Cow Pineapple` Dog< Horse Cat<""
                }
            },
            ""NodeIdValue"": ""http://opcfoundation.org/UA/Diagnostics#i=407765665"",
            ""ExpandedNodeIdValue"": ""http://opcfoundation.org/UA/Boiler/#g=f16b1f33-7701-a037-4b9b-c936ae51bc40"",
            ""QualifiedNameValue"": ""http://opcfoundation.org/UA/Boiler//Instance#%ec%bd%94%eb%81%bc%eb%a6%ac"",
            ""LocalizedTextValue"": {
                ""Text"": ""Персик Пурпурово\"" Змейка` Овцы Крыса Пурпурово* Голубо< Бело& Крыса Змейка"",
                ""Locale"": ""ru""
            },
            ""StatusCodeValue"": 4980736,
            ""VariantValue"": {
                ""Type"": ""SByte"",
                ""Body"": -114
            },
            ""EnumerationValue"": 0,
            ""StructureValue"": { ""TypeId"": null },
            ""Number"": {
                ""Type"": ""Double"",
                ""Body"": 0.0
            },
            ""Integer"": {
                ""Type"": ""Int64"",
                ""Body"": 0
            },
            ""UInteger"": {
                ""Type"": ""UInt64"",
                ""Body"": 0
            }
        }
    },
    {
        ""TypeId"": ""http://test.org/UA/Data/#i=11437"",
        ""Encoding"": ""Json"",
        ""Body"": {
            ""BooleanValue"": false,
            ""SByteValue"": -97,
            ""ByteValue"": 121,
            ""Int16Value"": -29579,
            ""UInt16Value"": 52214,
            ""Int32Value"": 150448275,
            ""UInt32Value"": 2074081332,
            ""Int64Value"": -1011618571483371166,
            ""UInt64Value"": 8946747598058890327,
            ""FloatValue"": 7.858336E+35,
            ""DoubleValue"": 10017916.0,
            ""StringValue"": ""яблоко Ананас~ Овцы Корова Пурпурово_ Банан Крыса Собака Кот Бело( Корова'"",
            ""DateTimeValue"": ""2034-12-05T15:52:28.675232Z"",
            ""GuidValue"": ""0500899c-1c30-8180-cbc7-333928152ed2"",
            ""ByteStringValue"": [ 231, 20, 90, 216, 130, 131, 90, 67, 79, 157, 9, 52, 206, 116, 148, 59, 17 ],
            ""XmlElementValue"": {
                ""n0:Корова"": {
                    ""@Желтыйцвет"": ""Strawberry"",
                    ""@Голубика"": ""Pig"",
                    ""@Чернота"": ""Yellow"",
                    ""@Корова"": ""Sheep"",
                    ""@Овцы"": ""Green"",
                    ""@яблоко"": ""Blue"",
                    ""@xmlns:n0"": ""http://Манго"",
                    ""n0:Красно"": ""Blue Yellow~ Mango Rat Rat Dog Lime; Horse Dog*"",
                    ""n0:Банан"": ""Black' Cat! Elephant Pineapple~ Snake Pineapple? Blue$ Sheep* Elephant\"" Rat"",
                    ""n0:Лимон"": ""Pineapple^ Rat Banana> Grape Cow Black# Snake"",
                    ""n0:Пурпурово"": ""Purple Horse Monkey Strawberry< Elephant{ Strawberry[ White{ Rat^ White` Strawberry?"",
                    ""n0:Крыса"": ""Mango? Blue; Cow> Lemon Horse# Blue Dragon~ Purple Snake< Purple Yellow^ Lime Rat Grape~"",
                    ""n0:Кот"": ""Sheep] Horse[ Black' Cat; Strawberry Lime_ Monkey> Sheep Pineapple: Sheep Pig= Red Red% Sheep$"",
                    ""n0:Голубика"": ""Green~""
                }
            },
            ""NodeIdValue"": ""http://opcfoundation.org/UA/Boiler//Instance#s=%e3%83%98%e3%83%93"",
            ""ExpandedNodeIdValue"": ""http://opcfoundation.org/UA/Boiler/#i=3489247698"",
            ""QualifiedNameValue"": ""DataAccess#%e9%a9%ac"",
            ""LocalizedTextValue"": {
                ""Text"": ""ブタ モモ 緑 いちご ドラゴン 犬; 青い~ モモ 黒; 緑 レモン} 猿% 馬 白い% 馬 牛 象 白い+ 象# いちご< 紫色: レモン~ モモ~ ブタ# マンゴ モモ"",
                ""Locale"": ""jp""
            },
            ""StatusCodeValue"": 1245184,
            ""VariantValue"": {
                ""Type"": ""ExpandedNodeId"",
                ""Body"": ""http://opcfoundation.org/UA/Boiler//Instance#b=4Ncr5uADYkU88S45mg%3d%3d""
            },
            ""EnumerationValue"": 0,
            ""StructureValue"": { ""TypeId"": null },
            ""Number"": {
                ""Type"": ""Double"",
                ""Body"": 0.0
            },
            ""Integer"": {
                ""Type"": ""Int64"",
                ""Body"": 0
            },
            ""UInteger"": {
                ""Type"": ""UInt64"",
                ""Body"": 0
            }
        }
    },
    {
        ""TypeId"": ""http://test.org/UA/Data/#i=11438"",
        ""Encoding"": ""Json"",
        ""Body"": {
            ""BooleanValue"": [ false, false, false, false, true ],
            ""SByteValue"": [ 120, 27, 27, 57, 117, 61, -42, 106 ],
            ""ByteValue"": [ 105, 241, 196, 82 ],
            ""Int16Value"": [ 22001, -1270, 27022, -11160 ],
            ""UInt16Value"": [ 31406, 22379, 11459, 18140 ],
            ""Int32Value"": [ 1147924132, 937096171, 293419963, 1355723363, 1682226035, 921241048, 946417831, 483648971, 1150550410 ],
            ""UInt32Value"": [ 405022290, 3763626854, 2219565007, 635093313, 1150728258 ],
            ""Int64Value"": [ -1689618757610414770, -1598013270992443575, -6068487195887049228, 3886489167998855712 ],
            ""UInt64Value"": [ 14348387007481775183, 579235881671951863, 10801679293459156539, 13309432137704145439 ],
            ""FloatValue"": [ 4.31136247E-33, 6.620173E-35, -8.877828E-29, -1.27605766E+29, -1.64668731E-22, 1.80246614E-21 ],
            ""DoubleValue"": [ -6011373486080.0, 108132739055616.0, 8.3547184787473483E-36, 5.2422583022226784E+27, 1.0392232580248937E-32, 1.0094077198242765E+33, -8.35414627813762E-39 ],
            ""StringValue"": [ ""녹색 들쭉 들쭉 돼지 녹색% 녹색 암소 원숭이 딸기+ 들쭉 암소~ 망고 망고 딸기 녹색 녹색 돼지 들쭉) 석회 개} 검정 쥐~ 쥐 코끼리= 들쭉"", ""Обезьяна Красно Зеленыйцвет Крыса"", ""Красно, Желтыйцвет Манго= Ананас Бело&"", ""Голубика Желтыйцвет"", ""蓝莓 大象~ 绵羊 柠檬 母牛 母牛 红色"", ""狗\"" 马 紫色` 葡萄@ 柠檬 芒果 猪 菠萝 龙^ 黑色* 马 绿色 绵羊 大象 红色) 蓝莓 蛇# 狗 香蕉 草莓 黑色@ 红色 鼠~ 蓝色 香蕉 猫 红色% 黑色"", ""Чернота- Собака Пурпурово# Голубика Чернота Голубо: Дракон яблоко Бело Зеленыйцвет"" ],
            ""DateTimeValue"": [ ""1916-05-09T17:48:30.6223191Z"" ],
            ""GuidValue"": [ ""842d41a6-6123-30ce-5970-6c26b28dd4de"", ""9aa488f4-bf70-5b49-848a-9197639e0990"", ""63306802-aacd-cf0e-20eb-70b1d72bdbe2"", ""5763cae3-358e-e7ef-f7d1-0098d037cdbb"", ""4924dc67-715c-4910-79d4-7a844f978358"", ""99b3afae-b13b-cc01-7949-6bfbaa75ffe9"", ""fb4ab41e-9107-7285-b919-deb9bb6a975f"", ""20eaa74e-3383-b67f-da57-d01305159e03"" ],
            ""ByteStringValue"": [ ""ZppNiFEdKUHgItJIEQ+yC6wDi99l6zWUIa/Bcm2jetrkKQP9EsZVzPdCU1zjkUbBYPlpm3j1LHtkuGaiXLfUPQ=="", ""+GToC7X45q6+5yOY2bGPaf8RczrfYe79iJhaX7JwP20VteotXbarAYLtuQ0I44s="", ""1jk="", ""pWEDx5Z16oIcnof7Tqe1giTGYgtJZXK38qjg9KUNU4g="", ""FJoMEu1Tt3Mzj2L78Q=="", ""ZVNgZ0B0LhI/7kvV7pX23A9L/oI5DahvNnOqmBbWD7wAPHqgRKUT"", ""SgFaHcYXZSJ8Vn8X/G8xWKvwMMzKlvxp34/UsRpVmGk36zc3soqpHg2HG79W98CRCyL1U3VGSbQF8T43Q7MIJ74="", ""3xA3+aUgRxG/Q3o8EufOQqb4YETz8aKCMsFMcdtZfvQAQBivWhE="", ""1AxjhwY5yd9WaQANEMd6Iu1utMfj1NY1ZcSGO9HPH+iUe4s3kGqbSGni9QjbTG4thh4qQKVKmAA2LSFBs40Nh0kXeZQ7QpKD0mteAd/NWhlVWbWz"", ""3kJK4osBYkhaldvUbb7D0tnxQ4unbTnrlyBo0wjsWQ=="" ],
            ""XmlElementValue"": [
                {
                    ""n0:Elephant"": {
                        ""@Monkey"": ""원숭이"",
                        ""@Horse"": ""석회"",
                        ""@Mango"": ""레몬"",
                        ""@Black"": ""고양이"",
                        ""@Grape"": ""들쭉쥐"",
                        ""@xmlns:n0"": ""http://Black"",
                        ""n0:Blue"": [ ""쥐 녹색$ 황색~ 코끼리, 개> 원숭이* 코끼리# 원숭이: 망고( 뱀 들쭉- 돼지, 복숭아 뱀 파인애플} 파인애플 쥐. 레몬 뱀 황색- 들쭉 쥐! 용} 말% 복숭아 코끼리} 돼지"", ""개 검정# 들쭉 망고 복숭아 망고$ 녹색 포도> 쥐 석회"" ],
                        ""n0:Sheep"": ""자주색/ 코끼리 코끼리 돼지 뱀 원숭이 황색{ 녹색& 복숭아 뱀 암소 고양이) 고양이 빨간 말] 복숭아< 고양이 돼지_ 코끼리( 용 코끼리 들쭉 말} 돼지 석회 원숭이'"",
                        ""n0:Pineapple"": ""암소 돼지} 쥐$ 들쭉- 황색 뱀 고양이 녹색. 바나나# 코끼리? 고양이 말 코끼리* 파란 파인애플"",
                        ""n0:Dog"": ""파인애플\"" 코끼리 암소 복숭아 포도 석회\"" 말 포도 자주색^ 파인애플` 포도? 고양이 검정* 빨간@ 바나나) 딸기& 들쭉)""
                    }
                },
                {
                    ""n0:石灰葡萄"": {
                        ""@蓝色"": ""葡萄马"",
                        ""@猪龙绿色"": ""母牛"",
                        ""@猴子"": ""桃子猫"",
                        ""@马狗"": ""蛇芒果"",
                        ""@蓝莓"": ""菠萝"",
                        ""@猪"": ""紫色桃子"",
                        ""@xmlns:n0"": ""http://菠萝葡萄"",
                        ""n0:猪芒果"": ""菠萝 蛇* 猪! 香蕉/"",
                        ""n0:猫"": ""绿色 黄色' 马 鼠 紫色/ 芒果< 绿色% 黑色 黑色? 石灰 柠檬 大象 狗 香蕉 菠萝 香蕉> 大象' 红色*"",
                        ""n0:芒果红色"": ""石灰,"",
                        ""n0:紫色绿色"": ""绵羊 桃子{ 桃子""
                    }
                }
            ],
            ""NodeIdValue"": [ ""http://samples.org/UA/memorybuffer#g=8c9312a3-b893-ea53-91d1-2382907eca95"", ""http://test.org/UA/Data//Instance#b=mZWnGBQiqm%2fQtuce1kejQM%2bdwkrCBDsAWl6ZeX3GfNZshJIz%2fPp%2fauhIgjOqs0w6"", ""nsu=DataAccess;s=파인애플"", ""http://test.org/UA/Data//Instance#s=%e8%9b%87%e7%8c%ab%e9%a6%99%e8%95%89"", ""http://opcfoundation.org/UA/Boiler//Instance#i=272173553"", ""nsu=DataAccess;b=b0PVHldheYEHVqYSX40/y4R9IYv92lU7yuG4V3n6mgH5hHz6JtoB6X4TUlAXoiijsj61kpDGuJXumVN2qSIIDbul"" ],
            ""ExpandedNodeIdValue"": [ ""http://samples.org/UA/memorybuffer/Instance#g=7ca9a545-0c37-87ea-0423-27b914a43b44"", ""http://opcfoundation.org/UA/#i=2349590220"", ""urn:manipc1:OPCFoundation:CoreSampleServer#g=94450a5c-8972-934d-6c99-0c1659b9ce0e"", ""http://test.org/UA/Data//Instance#s=%e6%a1%83%e5%ad%90"", ""http://opcfoundation.org/UA/Diagnostics#g=290ee634-1839-f122-f6b0-df426fb19e6b"", ""urn:manipc1:OPCFoundation:CoreSampleServer#i=2014900536"", ""http://opcfoundation.org/UA/Boiler//Instance#s=%ec%84%9d%ed%9a%8c"", ""http://test.org/UA/Data//Instance#b=4D2jPmkygekkYgnuy3rDjlEURSuQwxxtEVEYAMgjS9Cjxg%3d%3d"", ""http://opcfoundation.org/UA/Diagnostics#g=2005172c-cc4e-6fb5-0e0c-a653cb7c979a"", ""http://opcfoundation.org/UA/#i=405616161"" ],
            ""QualifiedNameValue"": [ ""urn:" + Dns.GetHostName() + @":OPCFoundation:CoreSampleServer#%d0%97%d0%bc%d0%b5%d0%b9%d0%ba%d0%b0"", ""DataAccess#%eb%b0%94%eb%82%98%eb%82%98"", ""http://samples.org/UA/memorybuffer#%d0%9a%d0%be%d1%80%d0%be%d0%b2%d0%b0"", ""%eb%b0%b1%ec%83%89"", ""http://samples.org/UA/memorybuffer/Instance#%e3%83%90%e3%83%8a%e3%83%8a"", ""DataAccess#%e3%83%91%e3%82%a4%e3%83%8a%e3%83%83%e3%83%97%e3%83%ab"", ""http://opcfoundation.org/UA/Boiler//Instance#Mango"" ],
            ""LocalizedTextValue"": [
                {
                    ""Text"": ""Black~ Pig' Red Black' Lime! Black} Purple Blue Cat Strawberry:"",
                    ""Locale"": ""en-US""
                },
                {
                    ""Text"": ""蓝色 芒果 猫 紫色. 鼠; 紫色 紫色 蛇 芒果 葡萄 狗' 母牛"",
                    ""Locale"": ""zh-CN""
                },
                {
                    ""Text"": ""草莓, 绵羊 龙 白色{ 白色} 大象, 绿色% 葡萄 菠萝) 蛇 香蕉} 蓝色' 猪 大象' 大象` 芒果^ 猫= 黄色 母牛("",
                    ""Locale"": ""zh-CN""
                },
                {
                    ""Text"": ""绵羊< 石灰/ 母牛: 大象"",
                    ""Locale"": ""zh-CN""
                },
                {
                    ""Text"": ""Овцы яблоко# Желтыйцвет Лимон( Змейка Собака Корова? Крыса Змейка> Лошадь Лошадь"",
                    ""Locale"": ""ru""
                },
                {
                    ""Text"": ""菠萝' 草莓. 狗 红色: 蛇, 菠萝 龙 猴子/ 菠萝$ 柠檬# 草莓. 蓝莓= 猫 菠萝< 柠檬: 狗 大象 石灰 马= 葡萄( 芒果/ 鼠;"",
                    ""Locale"": ""zh-CN""
                },
                {
                    ""Text"": ""Dog) Cat( Strawberry` Cat Monkey Elephant Horse! Grape- Peach Monkey} Blueberry! Red"",
                    ""Locale"": ""en-US""
                },
                {
                    ""Text"": ""Snake Grape Mango"",
                    ""Locale"": ""en-US""
                },
                {
                    ""Text"": ""蛇, 大象@ 红色 桃子+ 鼠 红色 紫色 草莓 菠萝"",
                    ""Locale"": ""zh-CN""
                },
                {
                    ""Text"": ""Кот Крыса Слон Свинья' Голубика Пурпурово@ Дракон- Обезьяна? Бело("",
                    ""Locale"": ""ru""
                }
            ],
            ""StatusCodeValue"": [ 6225920, 7995392, 5832704, 6553600, 3997696, 1900544, 8519680 ],
            ""VariantValue"": [
                {
                    ""Type"": ""Byte"",
                    ""Body"": 82
                }
            ],
            ""EnumerationValue"": [],
            ""StructureValue"": [],
            ""Number"": [],
            ""Integer"": [],
            ""UInteger"": []
        }
    },
    {
        ""TypeId"": ""http://test.org/UA/Data/#i=11437"",
        ""Encoding"": ""Json"",
        ""Body"": {
            ""BooleanValue"": true,
            ""SByteValue"": -56,
            ""ByteValue"": 104,
            ""Int16Value"": 3814,
            ""UInt16Value"": 38042,
            ""Int32Value"": 535350820,
            ""UInt32Value"": 3693060540,
            ""Int64Value"": -2577172637598593213,
            ""UInt64Value"": 11187487780701632783,
            ""FloatValue"": 1.93125867E+17,
            ""DoubleValue"": -0.00033564501791261137,
            ""StringValue"": ""ラット} 馬` いちご 青い 白い 象 レモン. パイナップル"",
            ""DateTimeValue"": ""1985-11-03T22:42:37.1296614Z"",
            ""GuidValue"": ""5b9f4a59-1a25-042a-e156-e9e08f8eed3d"",
            ""ByteStringValue"": [ 192, 70, 43, 233, 29, 173, 191, 102, 6, 234, 173, 153 ],
            ""XmlElementValue"": {
                ""n0:绵羊"": {
                    ""@猫菠萝"": ""馬"",
                    ""@黑色"": ""猿"",
                    ""@蓝色"": ""馬緑マンゴ"",
                    ""@龙大象"": ""バナナ"",
                    ""@菠萝白色"": ""青い"",
                    ""@xmlns:n0"": ""http://蓝色"",
                    ""n0:蛇紫色"": ""いちご\"" 猿 黒< マンゴ 馬 猫: ヘビ? 黒 猫) モモ% ブドウ 象 黄色{ レモン 紫色 ヒツジ; ブドウ 猫( 赤い/ マンゴ' バナナ ブルーベリー"",
                    ""n0:绿色"": ""石灰 黄色. ドラゴン! ヘビ ブルーベリー ラット< 馬 黒 緑) 馬 ラット 猫 モモ"",
                    ""n0:柠檬"": ""ブタ: 赤い 石灰# モモ"",
                    ""n0:马"": ""ラット$ 犬[ 牛 黒{ 青い 黒 緑}""
                }
            },
            ""NodeIdValue"": ""http://test.org/UA/Data//Instance#i=2103396786"",
            ""ExpandedNodeIdValue"": ""http://test.org/UA/Data//Instance#i=577318642"",
            ""QualifiedNameValue"": ""DataAccess#%e7%8a%ac%e3%83%96%e3%82%bf"",
            ""LocalizedTextValue"": {
                ""Text"": ""Red Green Lemon# Elephant Dog Horse Monkey: Lime' Strawberry Monkey"",
                ""Locale"": ""en-US""
            },
            ""StatusCodeValue"": 1638400,
            ""VariantValue"": {
                ""Type"": ""ExpandedNodeId"",
                ""Body"": ""http://samples.org/UA/memorybuffer#i=1429871234""
            },
            ""EnumerationValue"": 0,
            ""StructureValue"": { ""TypeId"": null },
            ""Number"": {
                ""Type"": ""Double"",
                ""Body"": 0.0
            },
            ""Integer"": {
                ""Type"": ""Int64"",
                ""Body"": 0
            },
            ""UInteger"": {
                ""Type"": ""UInt64"",
                ""Body"": 0
            }
        }
    }
]
");
            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = val, // TODO: expected,
                    DataType = "ExtensionObject"
                });

            // Assert

            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayNumberValueVariableTest1Async() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10324";

            var encoder = new JsonVariantEncoder();
            var values = _generator.GetRandomArray<sbyte>();
            var expected = new JArray(values
                .Select(v => JToken.FromObject(new {
                    Type = "SByte",
                    Body = v
                })));

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Number"
                });

            // Assert
            await AssertResultAsync(node, JArray.FromObject(values), result);
        }


        public async Task NodeWriteStaticArrayNumberValueVariableTest2Async() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10324";

            var encoder = new JsonVariantEncoder();
            var values = _generator.GetRandomArray<sbyte>();
            var expected = JArray.FromObject(values);

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Number"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayIntegerValueVariableTest1Async() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10325";

            var encoder = new JsonVariantEncoder();
            var values = _generator.GetRandomArray<int>();
            var expected = new JArray(values
                .Select(v => JToken.FromObject(new {
                    Type = "Int32",
                    Body = v
                })));

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Integer"
                });

            // Assert
            await AssertResultAsync(node, new JArray(values), result);
        }


        public async Task NodeWriteStaticArrayIntegerValueVariableTest2Async() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10325";

            var encoder = new JsonVariantEncoder();
            var values = _generator.GetRandomArray<int>();
            var expected = new JArray(values);

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "Integer"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }


        public async Task NodeWriteStaticArrayUIntegerValueVariableTest1Async() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10326";

            var encoder = new JsonVariantEncoder();
            var values = _generator.GetRandomArray<ushort>();
            var expected = new JArray(values
                .Select(v => JToken.FromObject(new {
                    Type = "UInt16",
                    Body = v
                })));

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "UInteger"
                });

            // Assert
            await AssertResultAsync(node, new JArray(values), result);
        }


        public async Task NodeWriteStaticArrayUIntegerValueVariableTest2Async() {

            var browser = _services();
            var node = "http://test.org/UA/Data/#i=10326";

            var encoder = new JsonVariantEncoder();
            var values = _generator.GetRandomArray<ushort>();
            var expected = JToken.FromObject(values);

            // Act
            var result = await browser.NodeValueWriteAsync(_endpoint,
                new ValueWriteRequestModel {
                    NodeId = node,
                    Value = expected,
                    DataType = "UInteger"
                });

            // Assert
            await AssertResultAsync(node, expected, result);
        }

        private async Task AssertResultAsync(string node, JToken expected,
            ValueWriteResultModel result) {
            var value = await _readExpected(_endpoint, node);
            Assert.NotNull(value);
            Assert.Null(result.ErrorInfo);
            var asString = value.ToString(Newtonsoft.Json.Formatting.None);
            System.Diagnostics.Trace.WriteLine(asString);
            Assert.Equal(expected.ToString(Newtonsoft.Json.Formatting.None), asString);
        }

        private readonly T _endpoint;
        private readonly Func<T, string, Task<JToken>> _readExpected;
        private readonly Func<INodeServices<T>> _services;
        private readonly Opc.Ua.Test.TestDataGenerator _generator =
            new Opc.Ua.Test.TestDataGenerator();
    }
}
