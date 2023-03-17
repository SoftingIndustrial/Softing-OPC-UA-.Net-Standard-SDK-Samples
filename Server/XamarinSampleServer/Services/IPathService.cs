/* ========================================================================
 * Copyright © 2011-2023 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

namespace XamarinSampleServer.Services
{
    public interface IPathService
    {
        string InternalFolder { get; }
        string PublicExternalFolder { get; }
        string PrivateExternalFolder { get; }
    }
}
