/* ========================================================================
 * Copyright © 2011-2022 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

namespace XamarinSampleServer.Model
{
    [Xamarin.Forms.Internals.Preserve(AllMembers = true)]
    class ConnectedSession
    {
        public string SessionId { get; set; }
        public string SessionName { get; set; }

        public uint SubscriptionsCount { get; set; }
    }
}
