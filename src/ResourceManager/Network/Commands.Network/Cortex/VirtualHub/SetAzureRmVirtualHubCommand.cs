﻿namespace Microsoft.Azure.Commands.Network
{
    using AutoMapper;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Management.Automation;
    using System.Security;
    using Microsoft.Azure.Commands.Network.Models;
    using Microsoft.Azure.Commands.ResourceManager.Common.Tags;
    using Microsoft.Azure.Management.Network;
    using Microsoft.WindowsAzure.Commands.Common;
    using MNM = Microsoft.Azure.Management.Network.Models;
    using Microsoft.Azure.Commands.ResourceManager.Common.ArgumentCompleters;
    using System.Linq;
    using Microsoft.Azure.Management.Internal.Resources.Utilities.Models;

    [Cmdlet(VerbsCommon.Set,
        "AzureRmVirtualHub",
        DefaultParameterSetName = CortexParameterSetNames.ByVirtualHubName,
        SupportsShouldProcess = true),
        OutputType(typeof(PSVirtualHub))]
    public class SetAzureRmVirtualHubCommand : VirtualHubBaseCmdlet
    {
        [Alias("ResourceName", "VirtualHubName")]
        [Parameter(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CortexParameterSetNames.ByVirtualHubName,
            HelpMessage = "The resource name.")]
        [ValidateNotNullOrEmpty]
        public virtual string Name { get; set; }

        [Parameter(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CortexParameterSetNames.ByVirtualHubName,
            HelpMessage = "The resource group name.")]
        [ResourceGroupCompleter]
        [ValidateNotNullOrEmpty]
        public virtual string ResourceGroupName { get; set; }

        [Alias("VirtualHubId")]
        [Parameter(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CortexParameterSetNames.ByVirtualHubResourceId,
            HelpMessage = "The resource id of the Virtual hub to be modified.")]
        [ResourceIdCompleter("Microsoft.Network/virtualHubs")]
        [ValidateNotNullOrEmpty]
        public virtual string ResourceId { get; set; }

        [Alias("VirtualHub")]
        [Parameter(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CortexParameterSetNames.ByVirtualHubObject,
            HelpMessage = "The Virtual hub object to be modified.")]
        [ValidateNotNullOrEmpty]
        public PSVirtualHub InputObject { get; set; }

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The virtual wan object this hub is linked to.")]
        public PSVirtualWan VirtualWan { get; set; }

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The id of virtual wan object this hub is linked to.")]
        [ResourceIdCompleter("Microsoft.Network/virtualWans")]
        public string VirtualWanId { get; set; }

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The address space string for this virtual hub.")]
        public string AddressPrefix { get; set; }

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The hub virtual network connections associated with this Virtual Hub.")]
        public List<PSHubVirtualNetworkConnection> HubVnetConnection { get; set; }

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "A hashtable which represents resource tags.")]
        public Hashtable Tag { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Run cmdlet in the background")]
        public SwitchParameter AsJob { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Do not ask for confirmation if you want to overrite a resource")]
        public SwitchParameter Force { get; set; }

        public override void Execute()
        {
            base.Execute();
            WriteWarning("The output object type of this cmdlet will be modified in a future release.");

            if (ParameterSetName.Equals(CortexParameterSetNames.ByVirtualHubObject, StringComparison.OrdinalIgnoreCase))
            {
                Name = InputObject.Name;
                ResourceGroupName = InputObject.ResourceGroupName;
            }
            else if (ParameterSetName.Equals(CortexParameterSetNames.ByVirtualHubResourceId, StringComparison.OrdinalIgnoreCase))
            {
                var parsedResourceId = new ResourceIdentifier(ResourceId);
                Name = parsedResourceId.ResourceName;
                ResourceGroupName = parsedResourceId.ResourceGroupName;
            }

            PSVirtualHub virtualHubToUpdate = this.GetVirtualHub(this.ResourceGroupName, this.Name);

            string virtualWanRGName = null;
            string virtualWanName = null;

            //// Resolve the virtual wan
            if (this.VirtualWan != null)
            {
                virtualWanRGName = this.VirtualWan.ResourceGroupName;
                virtualWanName = this.VirtualWan.Name;
            }
            else if (!string.IsNullOrWhiteSpace(this.VirtualWanId))
            {
                var parsedWanResourceId = new ResourceIdentifier(this.VirtualWanId);
                virtualWanName = parsedWanResourceId.ResourceName;
                virtualWanRGName = parsedWanResourceId.ResourceGroupName;
            }

            if (string.IsNullOrWhiteSpace(virtualWanRGName) || string.IsNullOrWhiteSpace(virtualWanName))
            {
                throw new PSArgumentException("A virtual hub cannot be created without a valid virtual wan");
            }

            PSVirtualWan resolvedVirtualWan = new VirtualWanBaseCmdlet().GetVirtualWan(virtualWanRGName, virtualWanName);
            virtualHubToUpdate.VirtualWan = new PSResourceId() { Id = resolvedVirtualWan.Id };


            //// Update address prefix, if specified
            if (!string.IsNullOrWhiteSpace(this.AddressPrefix))
            {
                virtualHubToUpdate.AddressPrefix = this.AddressPrefix;
            }

            //// HubVirtualNetworkConnections
            if (this.HubVnetConnection != null && this.HubVnetConnection.Any())
            {
                virtualHubToUpdate.VirtualNetworkConnections = new List<PSHubVirtualNetworkConnection>();
                virtualHubToUpdate.VirtualNetworkConnections.AddRange(this.HubVnetConnection);
            }

            //// Update the virtual hub
            ConfirmAction(
                    Force.IsPresent,
                    string.Format(Properties.Resources.SettingResourceMessage, this.Name),
                    Properties.Resources.SettingResourceMessage,
                    this.Name,
                    () =>
                    {
                        WriteObject(this.CreateOrUpdateVirtualHub(
                            this.ResourceGroupName,
                            this.Name,
                            virtualHubToUpdate,
                            this.Tag));
                    });
        }
    }
}