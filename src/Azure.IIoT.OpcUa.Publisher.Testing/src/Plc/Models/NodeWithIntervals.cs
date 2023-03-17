namespace Plc.PluginNodes.Models
{
    public class NodeWithIntervals
    {
        public string NodeId { get; set; }
        public string Namespace { get; set; }
        public uint PublishingInterval { get; set; }
        public uint SamplingInterval { get; set; }
    }
}