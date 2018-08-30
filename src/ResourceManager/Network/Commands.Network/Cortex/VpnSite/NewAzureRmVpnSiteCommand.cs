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

    [Cmdlet(VerbsCommon.New,
        "AzureRmVpnSite",
        DefaultParameterSetName = CortexParameterSetNames.ByVirtualWanName,
        SupportsShouldProcess = true),
        OutputType(typeof(PSVpnSite))]
    public class NewAzureRmVpnSiteCommand : VpnSiteBaseCmdlet
    {
        [Alias("ResourceName", "VpnSiteName")]
        [Parameter(Mandatory = true, 
            ValueFromPipelineByPropertyName = true, 
            HelpMessage = "The resource name.")]
        [ValidateNotNullOrEmpty]
        public virtual string Name { get; set; }

        [Parameter(Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The resource name.")]
        [ResourceGroupCompleter]
        [ValidateNotNullOrEmpty]
        public virtual string ResourceGroupName { get; set; }

        [Parameter(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The resource location.")]
        [LocationCompleter]
        [ValidateNotNullOrEmpty]
        public virtual string Location { get; set; }

        [Parameter(
            Mandatory = true,
            ParameterSetName = CortexParameterSetNames.ByVirtualWanName,
            HelpMessage = "The resource group name of the VirtualWan this VpnSite needs to be connected to.")]
        public string VirtualWanResourceGroupName { get; set; }

        [Parameter(
            Mandatory = true,
            ParameterSetName = CortexParameterSetNames.ByVirtualWanName,
            HelpMessage = "The name of the VirtualWan this VpnSite needs to be connected to.")]
        public string VirtualWanName { get; set; }

        [Parameter(
            Mandatory = true,
            ParameterSetName = CortexParameterSetNames.ByVirtualWanObject,
            HelpMessage = "The VirtualWan this VpnSite needs to be connected to.")]
        public PSVirtualWan VirtualWan { get; set; }

        [Parameter(
            Mandatory = true,
            ParameterSetName = CortexParameterSetNames.ByVirtualWanResourceId,
            HelpMessage = "The ResourceId VirtualWan this VpnSite needs to be connected to.")]
        [ResourceIdCompleter("Microsot.Network/virtualWans")]
        public string VirtualWanId { get; set; }

        [Parameter(Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The resource name.")]
        public string IpAddress { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "The address prefixes of the virtual network. Use this or AddressSpaceObject but not both.")]
        [ValidateNotNullOrEmpty]
        public List<string> AddressSpace { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "The device model of the remote vpn device.")]
        public string DeviceModel { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "The device vendor of the remote vpn device.")]
        public string DeviceVendor { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "The device model of the remote vpn device.")]
        public uint LinkSpeedInMbps { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "The BGP ASN for this VpnSite.")]
        public uint BgpAsn { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "The BGP Peering Address for this VpnSite.")]
        public string BgpPeeringAddress { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "The BGP Peering weight for this VpnSite.")]
        public uint BgpPeeringWeight { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "The SiteKey for this VpnSite.")]
        public SecureString SiteKey { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Is this VpnSite a security site")]
        public SwitchParameter IsSecuritySite { get; set; }

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

            PSVpnSite vpnSiteToCreate = new PSVpnSite();
            vpnSiteToCreate.ResourceGroupName = this.ResourceGroupName;
            vpnSiteToCreate.Name = this.Name;
            vpnSiteToCreate.IsSecuritySite = this.IsSecuritySite.IsPresent;
            vpnSiteToCreate.Location = this.Location;

            //// Resolve the virtual wan
            if (ParameterSetName.Equals(CortexParameterSetNames.ByVirtualWanObject, StringComparison.OrdinalIgnoreCase))
            {
                this.VirtualWanResourceGroupName = this.VirtualWan.ResourceGroupName;
                this.VirtualWanName = this.VirtualWan.Name;
            }
            else if (ParameterSetName.Equals(CortexParameterSetNames.ByVirtualWanResourceId, StringComparison.OrdinalIgnoreCase))
            {
                var parsedWanResourceId = new ResourceIdentifier(this.VirtualWanId);
                this.VirtualWanResourceGroupName = parsedWanResourceId.ResourceGroupName;
                this.VirtualWanName = parsedWanResourceId.ResourceName;
            }

            PSVirtualWan resolvedVirtualWan = new VirtualWanBaseCmdlet().GetVirtualWan(this.VirtualWanResourceGroupName, this.VirtualWanName);

            if (resolvedVirtualWan == null)
            {
                throw new PSArgumentException("The referenced virtual wan cannot be resolved.");
            }

            vpnSiteToCreate.VirtualWan = new PSResourceId() { Id = resolvedVirtualWan.Id };

            //// Bgp Settings
            if (this.BgpAsn > 0 || this.BgpPeeringWeight > 0 || !string.IsNullOrWhiteSpace(this.BgpPeeringAddress))
            {
                vpnSiteToCreate.BgpSettings = this.ValidateAndCreatePSBgpSettings(this.BgpAsn, this.BgpPeeringWeight, this.BgpPeeringAddress);
            }

            //// VpnSite device settings
            if (!string.IsNullOrWhiteSpace(this.DeviceModel) || string.IsNullOrWhiteSpace(this.DeviceVendor))
            {
                vpnSiteToCreate.DeviceProperties = this.ValidateAndCreateVpnSiteDeviceProperties(this.DeviceModel, this.DeviceVendor, this.LinkSpeedInMbps);
            }

            //// IpAddress
            System.Net.IPAddress ipAddress;
            if (!System.Net.IPAddress.TryParse(this.IpAddress, out ipAddress))
            {
                throw new PSArgumentException("The IPAddress specified is invalid.");
            }

            vpnSiteToCreate.IpAddress = this.IpAddress;

            //// Adress spaces
            if (this.AddressSpace.Any())
            {
                vpnSiteToCreate.AddressSpace = new PSAddressSpace();
                vpnSiteToCreate.AddressSpace.AddressPrefixes = new List<string>();
                vpnSiteToCreate.AddressSpace.AddressPrefixes.AddRange(this.AddressSpace);
            }

            //// SiteKey
            if (this.SiteKey != null)
            {
                vpnSiteToCreate.SiteKey = SecureStringExtensions.ConvertToString(this.SiteKey);
            }

            bool shouldProcess = this.Force.IsPresent;

            if (!shouldProcess)
            {
                shouldProcess = ShouldProcess(this.Name, Properties.Resources.CreatingResourceMessage);
            }

            if (shouldProcess)
            {
                WriteObject(this.CreateOrUpdateVpnSite(this.ResourceGroupName, this.Name, vpnSiteToCreate, this.Tag));
            }
        }
    }
}