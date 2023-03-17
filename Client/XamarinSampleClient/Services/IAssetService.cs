/* ========================================================================
 * Copyright © 2011-2023 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

namespace XamarinSampleClient.Services
{
    public interface IAssetService
    {
        void SaveFile(string fileName, string destinationFilePath);
    }
}
