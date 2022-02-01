namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Class to enable output to the console.
    /// </summary>
    public interface IPublisherDiagnosticInfo {
        /// <summary>
        /// Sequence number of the event
        /// </summary>
        Dictionary<string, SessionDiagnosticInfo> DiagnosticInfo { get; set; }
    }
}
