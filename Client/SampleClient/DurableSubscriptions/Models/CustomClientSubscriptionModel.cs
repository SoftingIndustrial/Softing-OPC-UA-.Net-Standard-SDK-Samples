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
    /// Custom client subscription model to be kept after the client stops.
    /// </summary>
    [DataContract(Namespace = Namespaces.OpcUaXsd)]
    [KnownType(typeof(Subscription))]
    [KnownType(typeof(SubscriptionCollection))]
    [KnownType(typeof(EventFilterEx))]
    [KnownType(typeof(ClientSubscriptionModel))]
    public class CustomClientSubscriptionModel : ClientSubscriptionModel
    {
        #region Constructor(s)

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomClientSubscriptionModel"/> class using subscription object.
        /// </summary>
        /// <param name="clientSubscription">The client subscription.</param>
        /// <summary>
        public CustomClientSubscriptionModel(ClientSubscription clientSubscription) : base(clientSubscription)
        {
            SequentialPublishing = clientSubscription.SequentialPublishing;
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the behavior of waiting for sequential order in handling incoming messages.
        /// </summary>
        [DataMember(IsRequired = true, Order = 1)]
        internal bool SequentialPublishing { get; private set; }

        #endregion
    }

    /// <summary>
    /// Model used for CustomClientSubscriptionModel collection serialization/deserialization process to be kept after the client stops.
    /// </summary>
    [CollectionDataContract(Name = "ListOfCustomSubscription", Namespace = Namespaces.OpcUaXsd, ItemName = "CustomClientSubscriptionModel")]
    public class CustomListOfCustomSubscriptionModel : List<CustomClientSubscriptionModel>
    {
        #region Constructor(s)

        /// <summary>
        /// Initializes a new instance of the <see cref="ListOfCustomMonitoredItemModel"/> class.
        /// </summary>
        public CustomListOfCustomSubscriptionModel() { }

        #endregion
    }
}
