/* ========================================================================
* Copyright © 2011-2025 Softing Industrial Automation GmbH. 
* All rights reserved.
* 
* The Software is subject to the Softing Industrial Automation GmbH’s 
* license agreement, which can be found here:
* https://industrial.softing.com/LA-SDK-en
*  
* ======================================================================*/

using Opc.Ua;
using Opc.Ua.Client;
using Softing.Opc.Ua.Client;
using Softing.Opc.Ua.Client.DurableSubscriptions.Models;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SampleClient.DurableSubscriptions.Models
{
    /// <summary>
    /// Custom Client Monitored Item Model to be kept after the client stops.
    /// </summary>
    [DataContract(Namespace = Namespaces.OpcUaXsd)]
    [KnownType(typeof(MonitoredItem))]
    [KnownType(typeof(ClientMonitoredItemModel))]
    public class CustomClientMonitoredItemModel : ClientMonitoredItemModel
    {
        #region Constructor(s)

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomClientMonitoredItemModel"/> class using an instance of the monitored item object.
        /// </summary>
        public CustomClientMonitoredItemModel(ClientMonitoredItem monitoredItem) : base(monitoredItem)
        {
            ConnectedIsSampling = monitoredItem.ConnectedIsSampling;
        }
        #endregion

        #region Public Properties

        /// <summary>
        ///  Gets or sets a value indicating whether the MonitoredItem is Sampling or not when in connected state.
        /// </summary>
        [DataMember(IsRequired = true, Order = 1)]
        public bool ConnectedIsSampling { get; private set; }

        #endregion
    }

    /// <summary>
    /// Model used for CustomClientMonitoredItemModel collection serialization/deserialization process to be kept after the client stops.
    /// </summary>
    [CollectionDataContract(Name = "ListOfCustomMonitoredItem", Namespace = Namespaces.OpcUaXsd, ItemName = "CustomMonitoredItemModel")]
    public class ListOfCustomMonitoredItemModel : List<CustomClientMonitoredItemModel>
    {
        #region Constructor(s)

        /// <summary>
        /// Initializes a new instance of the <see cref="ListOfCustomMonitoredItemModel"/> class.
        /// </summary>
        public ListOfCustomMonitoredItemModel() { }

        #endregion
    }
}