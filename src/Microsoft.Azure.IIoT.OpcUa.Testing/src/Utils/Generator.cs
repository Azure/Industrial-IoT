// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Testing.Utils {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Microsoft.Azure.IIoT.OpcUa.History.Models;

    internal static class Generator {

        public static void WriteResponse(BrowseResultModel results) {
            var test = JsonConvert.SerializeObject(results, Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            var references = $@"
            Assert.{(results.ContinuationToken == null ? "Null" : "NotNull")}(results.ContinuationToken);
            Assert.Equal(""{results.Node.NodeId}"", results.Node.Id);
            Assert.Equal(""{results.Node.DataType}"", results.Node.DataType);
            Assert.Equal(""{results.Node.DisplayName}"", results.Node.DisplayName);
            Assert.Equal(NodeClass.{results.Node.NodeClass}, results.Node.NodeClass);
            Assert.Equal(NodeAccessLevel.{results.Node.AccessLevel}, results.Node.AccessLevel);
            Assert.Equal(NodeAccessLevel.{results.Node.UserAccessLevel}, results.Node.UserAccessLevel);
            Assert.Equal(NodeValueRank.{results.Node.ValueRank}, results.Node.ValueRank);
            Assert.Equal({results.Node.WriteMask}, results.Node.WriteMask);
            Assert.Equal({results.Node.UserWriteMask}, results.Node.UserWriteMask);
            Assert.Equal(NodeEventNotifier.{results.Node.EventNotifier}, results.Node.EventNotifier);
            Assert.{(results.Node.Executable ?? false ? "True" : "False")}(results.Node.Executable);
            Assert.{(results.Node.UserExecutable ?? false ? "True" : "False")}(results.Node.UserExecutable);
            Assert.{(results.Node.Children ?? false ? "True" : "False")}(results.Node.HasChildren);
            Assert.Collection(results.References, ";
            foreach (var reference in results.References) {
                references +=
$@"                reference => {{
                    Assert.Equal(""{reference.ReferenceTypeId}"", reference.Id);
                    Assert.Equal({reference.Direction}, reference.Direction);
                    Assert.Equal(NodeClass.{reference.Target.NodeClass}, reference.Target.NodeClass);
                    Assert.Equal(""{reference.Target.NodeId}"",
                        reference.Target.Id);
                    Assert.Equal(""{reference.Target.DataType}"", reference.Target.DataType);
                    Assert.Equal(""{reference.Target.DisplayName}"", reference.Target.DisplayName);
                    Assert.Equal(NodeAccessLevel.{reference.Target.AccessLevel},
                        reference.Target.AccessLevel);
                    Assert.Equal(NodeAccessLevel.{reference.Target.UserAccessLevel},
                        reference.Target.UserAccessLevel);
                    Assert.Equal(NodeValueRank.{reference.Target.ValueRank}, reference.Target.ValueRank);
                    Assert.Equal({reference.Target.ArrayDimensions}, reference.Target.ArrayDimensions);
                    Assert.Equal({reference.Target.WriteMask ?? 0}, reference.Target.WriteMask);
                    Assert.Equal({reference.Target.UserWriteMask ?? 0}, reference.Target.UserWriteMask);
                    Assert.Equal(NodeEventNotifier.{reference.Target.EventNotifier}, reference.Target.EventNotifier);
                    Assert.{(reference.Target.Executable ?? false ? "True" : "False")}(reference.Target.Executable);
                    Assert.{(reference.Target.UserExecutable ?? false ? "True" : "False")}(reference.Target.UserExecutable);
                    Assert.{(reference.Target.Children ?? false ? "True" : "False")}(reference.Target.HasChildren);
                }},";
            }
            references.TrimEnd().TrimEnd(',');
            references += ");";

            System.Diagnostics.Trace.WriteLine(references);
        }

        internal static void Write(HistoricValueModel[] history, [CallerMemberName] string methodname = null) {
            var test = JsonConvert.SerializeObject(history, Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            var args = $@"
            Assert.Collection(results.History, ";
            foreach (var arg in history) {
                args += $@"
                arg => {{
                    Assert.Equal({arg.StatusCode}, arg.StatusCode);
                    Assert.Equal({arg.Value}, arg.Value);
                }},";
            }
            args = args.TrimEnd().TrimEnd(',');


            System.Diagnostics.Trace.WriteLine(methodname);
            System.Diagnostics.Trace.WriteLine("");
            System.Diagnostics.Trace.WriteLine(args);
            System.Diagnostics.Trace.WriteLine("");
        }

        internal static void WriteResponse(MethodMetadataResultModel results, [CallerMemberName] string methodname = null) {
            var test = JsonConvert.SerializeObject(results, Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            var args = $@"
            Assert.Collection(result.InputArguments, ";
            foreach (var arg in results.InputArguments) {
                args +=$@"
                arg => {{
                    Assert.Equal(""{arg.Name}"", arg.Name);
                    Assert.Equal(NodeValueRank.{arg.ValueRank}, arg.ValueRank);
                    Assert.Equal(""{JsonConvert.SerializeObject(arg.ArrayDimensions)}"", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.{arg.Type.NodeClass}, arg.Type.NodeClass);
                    Assert.Equal(""{arg.Type.NodeId}"", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal(""{arg.Type.DisplayName}"", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                }},";
            }
            args = args.TrimEnd().TrimEnd(',');

            args += $@");
            Assert.Collection(result.OutputArguments, ";
            foreach (var arg in results.OutputArguments) {
                args += $@"
                arg => {{
                    Assert.Equal(""{arg.Name}"", arg.Name);
                    Assert.Equal(NodeValueRank.{arg.ValueRank}, arg.ValueRank);
                    Assert.Equal(""{JsonConvert.SerializeObject(arg.ArrayDimensions)}"", JsonConvert.SerializeObject(arg.ArrayDimensions));
                    Assert.Equal(NodeClass.{arg.Type.NodeClass}, arg.Type.NodeClass);
                    Assert.Equal(""{arg.Type.NodeId}"", arg.Type.Id);
                    Assert.Null(arg.Type.DataType);
                    Assert.Equal(""{arg.Type.DisplayName}"", arg.Type.DisplayName);
                    Assert.Null(arg.DefaultValue);
                }},";
            }
            args = args.TrimEnd().TrimEnd(',');
            args += ");";


            System.Diagnostics.Trace.WriteLine(methodname);
            System.Diagnostics.Trace.WriteLine("");
            System.Diagnostics.Trace.WriteLine(args);
            System.Diagnostics.Trace.WriteLine("");
        }

        internal static void WriteValue(Action context, JToken result) {

            var expected = result.ToString(Formatting.None);
            expected = expected.Replace("\\\"", "&quot");
            expected = expected.Replace("\"", "\\\"");

            var code = $@"
            var expected = JToken.Parse({expected}
";
            //            var code = $@"
            //            var expected = JToken.Parse(
            //";
            //            while (!string.IsNullOrEmpty(expected)) {
            //                var length = Math.Min(expected.Length, 60);
            //                var part = expected.Substring(0, length);
            //                expected = expected.Remove(0, length);
            //                code += "                \"" + part + "\"" + (string.IsNullOrEmpty(expected) ? ");\r\n" : " +\r\n");
            //            }

            var methodname = context.GetMethodInfo().Name;
            methodname = methodname.Split(new[] { '>' }, 2).First();
            methodname = methodname.Split(new[] { '<' }, 2).Last();
            System.Diagnostics.Trace.WriteLine(methodname);
            System.Diagnostics.Trace.WriteLine("");
            System.Diagnostics.Trace.WriteLine(code);
            System.Diagnostics.Trace.WriteLine("");
        }
    }
}
