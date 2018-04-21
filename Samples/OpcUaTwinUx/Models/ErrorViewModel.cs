using System;

namespace Microsoft.Azure.IIoT.Browser.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public string ErrorHeader { get; set; }
        public string EndpointId { get; set; }
        public string ErrorMessage { get; set; }
    }
}