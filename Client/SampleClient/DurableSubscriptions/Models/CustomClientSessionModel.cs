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
using System.Runtime.Serialization;

namespace SampleClient.DurableSubscriptions.Models
{
    /// <summary>
    /// Custom Client Session Model to be kept after the client stops 
    /// </summary>
    [DataContract(Namespace = Namespaces.OpcUaXsd)]
    [KnownType(typeof(ClientSessionModel))]
    [KnownType(typeof(ConfiguredEndpoint))]
    [KnownType(typeof(UserIdentity))]
    [KnownType(typeof(EndpointDescription))]
    [KnownType(typeof(EndpointDescriptionEx))]
    [KnownType(typeof(Subscription))]
    [KnownType(typeof(SubscriptionCollection))]
    [KnownType(typeof(MonitoredItem))]
    [KnownType(typeof(EventFilterEx))]
    [KnownType(typeof(SelectOperandEx))]
    [KnownType(typeof(ClientSessionModel))]
    [KnownType(typeof(CustomClientSessionModel))]
    public class CustomClientSessionModel : ClientSessionModel
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomClientSessionModel"/> class using client session object.
        /// </summary>
        /// <param name="clientSession">The client session.</param>
        public CustomClientSessionModel(ClientSession clientSession) : base(clientSession)
        {
            SessionName = clientSession.SessionName;

            foreach (ClientSubscription clientSubscription in clientSession.Subscriptions)
            {
                ClientSubscriptionModelList.Add(new CustomClientSubscriptionModel(clientSubscription));
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the session name.
        /// </summary>
        [DataMember(IsRequired = true, Order = 1)]
        public string SessionName { get; private set; }

        #endregion
    }
}