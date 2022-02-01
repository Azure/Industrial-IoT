namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Engine {
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Class to enable output to the console.
    /// </summary>
    public class PublisherDiagnosticInfo : IPublisherDiagnosticInfo {

        /// <summary>
        /// Constructor.
        /// </summary>
        public PublisherDiagnosticInfo() {

            //_diagnosticInfo = diagnosticInfo;
            //if (diagnosticInfo != null) {
            //    DiagnosticInfo.a;
            //}
            
        }

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public Dictionary<string, SessionDiagnosticInfo> DiagnosticInfo { get; set; }

        //private readonly ISessionDiagnosticInfo _diagnosticInfo;
    }
}
