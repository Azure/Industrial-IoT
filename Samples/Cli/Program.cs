// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Cli {
    using System;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Sample program to run imports
    /// </summary>
    public class Program {

        enum Op {
            None,
            TestClient
        }

        /// <summary>
        /// Importer entry point
        /// </summary>
        /// <param name="args">command-line arguments</param>
        public static void Main(string[] args) {
            string source = null;
            string graph = null;
            var op = Op.None;

            var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
            try {
                for (var i = 0; i < args.Length; i++) {
                    switch (args[i]) {
                        case "-s":
                        case "--server":
                            i++;
                            if (op != Op.None) {
                                throw new ArgumentException("Operations are mutual exclusive");
                            }
                            op = Op.TestClient;
                            if (i < args.Length) {
                                source = args[i];
                            }
                            break;
                   //    case "-n":
                   //    case "--nodeset":
                   //        i++;
                   //        if (importer != null) {
                   //            throw new ArgumentException("Operations are mutual exclusive");
                   //        }
                   //        if (i < args.Length) {
                   //            importer = new NodeSet2FileImporter();
                   //            source = args[i];
                   //        }
                   //        else {
                   //            throw new ArgumentException("Missing nodeset file name.");
                   //        }
                   //        break;
                   //    case "-m":
                   //    case "--default-nodesets":
                   //        if (importer != null) {
                   //            throw new ArgumentException("Operations are mutual exclusive");
                   //        }
                   //
                   //        importer = new DefaultModelImporter();
                   //        graph = "http://opcfoundation.org/";
                   //        break;
                   //    case "-o":
                   //    case "--output":
                   //        i++;
                   //        if (target != null) {
                   //            throw new ArgumentException("Only one target");
                   //        }
                   //        if (i < args.Length) {
                   //            target = new TextWriterFactory(args[i]);
                   //        }
                   //        else {
                   //            throw new ArgumentException("Missing output file name.");
                   //        }
                   //        break;
                   //    case "-r":
                   //    case "--repeat":
                   //        repeat = true;
                   //        break;
                   //    case "-b":
                   //    case "--blazegraph":
                   //        i++;
                   //        if (target != null) {
                   //            throw new ArgumentException("Only one target");
                   //        }
                   //        if (i < args.Length) {
                   //            target = new BlazegraphHandlerFactory(args[i]);
                   //        }
                   //        else {
                   //            throw new ArgumentException("Missing server name.");
                   //        }
                   //        break;
                   //    case "-d":
                   //    case "--documents":
                   //        i++;
                   //        if (target != null) {
                   //            throw new ArgumentException("Only one target");
                   //        }
                   //        if (i < args.Length) {
                   //            target = new GraphServerHandlerFactory(args[i], configuration);
                   //        }
                   //        else {
                   //            throw new ArgumentException("Missing collection name.");
                   //        }
                   //        break;
                   //    case "-c":
                   //    case "--console":
                   //        if (target != null) {
                   //            throw new ArgumentException("Only one target");
                   //        }
                   //        target = new TextWriterFactory(Console.Out);
                   //        break;
                   //    case "--null":
                   //        if (target != null) {
                   //            throw new ArgumentException("Only one target");
                   //        }
                   //        target = new NullHandlerFactory();
                   //        break;
                        case "-?":
                        case "-h":
                        case "--help":
                            throw new ArgumentException("Help");
                        default:
                            throw new ArgumentException($"Unknown {args[i]}");
                    }
                }
                if (op == Op.None) {
                    throw new ArgumentException("Missing operation.");
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine(
                    @"
Importer - Imports into graph.
usage:       Import [options] target operation [args]

Options:

    --help
     -?
     -h      Prints out this help.

Target (Mutually exclusive):

     -c
    --console 
             Output to console using n3 formatting.
     -o
    --output 
             Output to named file.
     -b
    --blazegraph
             Write to blazegraph rdf database specified by following url.
     -d
    --documents
             Write to Cosmos DB graph collection with specified name.

Operations (Mutually exclusive):

     -s
    --server
             Import from server url.  The server is queried or browsed to
             gather all nodes to import.
     -n
    --nodeset
             Import from specified nodeset file. Only .nodeset2.xml files
             are supported. 

   --default-nodesets
             Import all default nodesets into a single graph.
"
                    );
                return;
            }

            if (graph == null) {
                graph = source;
            }
            try {
                Console.WriteLine($"Importing {graph}...");
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }

            Console.WriteLine("Press key to exit...");
            Console.ReadKey();
        }
    }
}
