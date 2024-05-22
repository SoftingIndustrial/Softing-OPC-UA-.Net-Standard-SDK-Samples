/* ========================================================================
 * Copyright © 2011-2024 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using Xamarin.Forms;
using XamarinSampleServer.Services;

[assembly: Dependency(typeof(XamarinSampleServer.Droid.Services.PathService))]

namespace XamarinSampleServer.Droid.Services
{

    public class PathService : IPathService
    {
        public string InternalFolder
        {
            get
            {
                return Android.App.Application.Context.FilesDir.AbsolutePath;
            }
        }

        public string PublicExternalFolder
        {
            get
            {
                return Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/";
            }
        }

        public string PrivateExternalFolder
        {
            get
            {
                return Android.App.Application.Context.GetExternalFilesDir(null).AbsolutePath;
            }
        }
    }
}