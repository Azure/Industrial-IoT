
using Opc.Ua;
using Opc.Ua.Server;
using System.Collections.Generic;

namespace Publisher
{
    public partial class PublisherServer : StandardServer
    {
        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            List<INodeManager> nodeManagers = new List<INodeManager>();
            nodeManagers.Add(new PublisherNodeManager(server, configuration));
            return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
        }

        protected override ServerProperties LoadServerProperties()
        {
            ServerProperties properties = new ServerProperties();
            properties.ManufacturerName = "Contoso";
            properties.ProductName      = "IoT Edge OPC Publisher";
            properties.ProductUri       = "";
            properties.SoftwareVersion  = Utils.GetAssemblySoftwareVersion();
            properties.BuildNumber      = Utils.GetAssemblyBuildNumber();
            properties.BuildDate        = Utils.GetAssemblyTimestamp();
            return properties; 
        }
    }
}
