/* ========================================================================
 * Copyright © 2011-2022 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using Android.Content.Res;
using System.IO;
using Xamarin.Forms;
using XamarinSampleServer.Services;

[assembly: Dependency(typeof(XamarinSampleServer.Droid.Services.AssetService))]
namespace XamarinSampleServer.Droid.Services
{
    public class AssetService : IAssetService
    {
        public string LoadFile(string fileName)
        {
            if (fileName == null)
            {
                return null;
            }

            // Read the contents of our asset
            string content;
            AssetManager assets = Android.App.Application.Context.Assets;
            using (StreamReader sr = new StreamReader(assets.Open(fileName)))
            {
                content = sr.ReadToEnd();
            }
            return content;
        }
    }
}