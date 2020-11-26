// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.Service.Models
{
    using BusinessLogic;
    using Enums;

    /// <summary>
    /// The model that contains the command to execute along with its configuration (only required with "Start"-Command).
    /// </summary>
    public class CommandModel
    {
        public CommandEnum CommandType { get; set; }

        public ValidatorConfiguration Configuration { get; set; }
    }
}