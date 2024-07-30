// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Netcap;

using var cts = new CancellationTokenSource();

Console.WriteLine($@"
   ____  _____   _____   _   _      _
  / __ \|  __ \ / ____| | \ | |    | |
 | |  | | |__) | |      |  \| | ___| |_ ___ __ _ _ __
 | |  | |  ___/| |      | . ` |/ _ \ __/ __/ _` | '_ \
 | |__| | |    | |____  | |\  |  __/ || (_| (_| | |_) |
  \____/|_|     \_____| |_| \_|\___|\__\___\__,_| .__/
                                                | |
                                                |_| {typeof(Extensions).Assembly.GetVersion()}
");

using var cmdLine = await App.RunAsync(args, cts.Token).ConfigureAwait(false);
