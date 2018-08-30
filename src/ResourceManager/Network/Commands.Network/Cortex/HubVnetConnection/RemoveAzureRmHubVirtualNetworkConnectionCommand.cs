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

    [Cmdlet(VerbsCommon.Remove,
        "AzureRmHubVirtualNetworkConnection",
        DefaultParameterSetName = CortexParameterSetNames.ByHubVirtualNetworkConnectionName,
        SupportsShouldProcess = true),
        OutputType(typeof(PSHubVirtualNetworkConnection))]
    public class RemoveHubVirtualNetworkConnectionCommand : HubVnetConnectionBaseCmdlet
    {
        [Alias("ResourceName", "HubVirtualNetworkConnectionName")]
        [Parameter(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CortexParameterSetNames.ByHubVirtualNetworkConnectionName,
            HelpMessage = "The resource name.")]
        [ValidateNotNullOrEmpty]
        public virtual string Name { get; set; }

        [Parameter(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CortexParameterSetNames.ByHubVirtualNetworkConnectionName,
            HelpMessage = "The resource group name.")]
        [ResourceGroupCompleter]
        [ValidateNotNullOrEmpty]
        public virtual string ResourceGroupName { get; set; }

        [Alias("VirtualHubName", "ParentVirtualHubName")]
        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CortexParameterSetNames.ByHubVirtualNetworkConnectionName,
            HelpMessage = "The parent resource name.")]
        public string ParentResourceName { get; set; }

        [Alias("HubVirtualNetworkConnection")]
        [Parameter(
            Mandatory = false,
            ValueFromPipeline = true,
            ParameterSetName = CortexParameterSetNames.ByHubVirtualNetworkConnectionObject,
            HelpMessage = "The hubvirtualnetworkconnection resource to modify.")]
        public PSHubVirtualNetworkConnection InputObject { get; set; }

        [Alias("HubVirtualNetworkConnectionId")]
        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = CortexParameterSetNames.ByHubVirtualNetworkConnectionResourceId,
            HelpMessage = "The resource id of the hubvirtualnetworkconnection resource to modify.")]
        public string ResourceId { get; set; }

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
            //// Resolve the paramters
            if (ParameterSetName.Equals(CortexParameterSetNames.ByHubVirtualNetworkConnectionObject, StringComparison.OrdinalIgnoreCase))
            {
                var parsedResourceId = new ResourceIdentifier(this.InputObject.Id);
                this.ResourceGroupName = parsedResourceId.ResourceGroupName;
                this.ParentResourceName = parsedResourceId.ParentResource.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last();
                this.Name = parsedResourceId.ResourceName;
            }
            else if (ParameterSetName.Equals(CortexParameterSetNames.ByHubVirtualNetworkConnectionResourceId, StringComparison.OrdinalIgnoreCase))
            {
                var parsedResourceId = new ResourceIdentifier(this.ResourceId);
                this.ResourceGroupName = parsedResourceId.ResourceGroupName;
                this.ParentResourceName = parsedResourceId.ParentResource.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last();
                this.Name = parsedResourceId.ResourceName;
            }

            //// Get the virtual hub - this will throw not found if the resource is invalid
            PSVirtualHub parentVirtualHub = this.GetVirtualHub(this.ResourceGroupName, this.ParentResourceName);

            if (parentVirtualHub == null)
            {
                throw new PSArgumentException("The parent hub object could not be found");
            }

            var connectionToRemove = parentVirtualHub.VirtualNetworkConnections.FirstOrDefault(connection => connection.Name.Equals(this.Name, StringComparison.OrdinalIgnoreCase));
            if (connectionToRemove != null)
            {
                base.Execute();

                ConfirmAction(
                    Force.IsPresent,
                    string.Format(Properties.Resources.RemovingResource, Name),
                    Properties.Resources.RemoveResourceMessage,
                    this.Name,
                    () =>
                    {
                        parentVirtualHub.VirtualNetworkConnections.Remove(connectionToRemove);
                        this.CreateOrUpdateVirtualHub(this.ResourceGroupName, this.ParentResourceName, parentVirtualHub, parentVirtualHub.Tag);
                        WriteObject(true);
                    });
            }
        }
    }
}