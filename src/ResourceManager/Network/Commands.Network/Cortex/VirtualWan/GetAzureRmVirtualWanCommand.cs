﻿namespace Microsoft.Azure.Commands.Network
{
    using Microsoft.Azure.Commands.Network.Models;
    using Microsoft.Azure.Commands.ResourceManager.Common.ArgumentCompleters;
    using Microsoft.Azure.Management.Network.Models;
    using System.Collections.Generic;
    using System.Management.Automation;

    [Cmdlet(VerbsCommon.Get,
        "AzureRmVirtualWan",
        SupportsShouldProcess = true),
        OutputType(typeof(PSVirtualWan))]
    public class GetAzureRmVirtualWanCommand : VirtualWanBaseCmdlet
    {
        [Alias("ResourceName")]
        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The resource name.")]
        [ValidateNotNullOrEmpty]
        public virtual string Name { get; set; }

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "The resource group name.")]
        [ResourceGroupCompleter]
        [ValidateNotNullOrEmpty]
        public virtual string ResourceGroupName { get; set; }

        public override void Execute()
        {
            base.Execute();
            WriteWarning("The output object type of this cmdlet will be modified in a future release.");

            if (!string.IsNullOrEmpty(this.Name))
            {
                if (string.IsNullOrWhiteSpace(this.ResourceGroupName))
                {
                    throw new PSArgumentException("ResourceGroupName must be specified if ResourceName is specified.");
                }

                var virtualWan = this.GetVirtualWan(this.ResourceGroupName, this.Name);
                WriteObject(virtualWan);
            }
            else
            {
                WriteObject(this.ListVirtualWans(this.ResourceGroupName));
            }
        }
    }
}