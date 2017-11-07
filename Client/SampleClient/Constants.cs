/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 *  
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Text;

namespace SampleClient
{
    public class Constants
    {
        public const string ServerDiscoveryUrl = "opc.tcp://localhost:4840";   //getdefaultdiscoveryurl???
        public const string ServerUrl = "opc.tcp://localhost:61510/SampleServer";
        public const string ServerUrlHttps = "https://localhost:61511/SampleServer";
    }
}
