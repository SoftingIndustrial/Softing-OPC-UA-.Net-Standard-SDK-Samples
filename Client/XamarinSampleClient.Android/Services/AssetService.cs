/* ========================================================================
 * Copyright © 2011-2020 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 * 
 * ======================================================================*/

using Android.Content.Res;
using System.IO;
using Xamarin.Forms;
using XamarinSampleClient.Services;

[assembly: Dependency(typeof(XamarinSampleClient.Droid.Services.AssetService))]
namespace XamarinSampleClient.Droid.Services
{
    public class AssetService : IAssetService
    {
        public void SaveFile(string fileName, string destinationFilePath)
        {
            if (fileName == null)
            {
                return;
            }

            // Read the contents of our asset and save it
            AssetManager assets = Android.App.Application.Context.Assets;
            using (var br = new BinaryReader(assets.Open(fileName)))
            {
                using (var bw = new BinaryWriter(new FileStream(destinationFilePath, FileMode.Create)))
                {
                    byte[] buffer = new byte[2048];
                    int length = 0;
                    while ((length = br.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        bw.Write(buffer, 0, length);
                    }
                }
            }

        }
    }
}