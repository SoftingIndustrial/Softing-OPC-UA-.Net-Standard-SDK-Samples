/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * ======================================================================*/

using Android.App;
using Android.Content.PM;
using Android.Net;
using Android.OS;

namespace XamarinSampleServer.Droid
{
    [Activity(Label = "Opc UA Server", Theme = "@style/splashscreen", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            #region force usage of wifi

            try
            {
                ConnectivityManager connectivityManager = ConnectivityManager.FromContext(Application.Context);
                var networks = connectivityManager.GetAllNetworks();
                foreach (Network network in networks)
                {
                    NetworkInfo networkInfo = connectivityManager.GetNetworkInfo(network);
                    if (networkInfo.Type == ConnectivityType.Wifi)
                    {
                        connectivityManager.BindProcessToNetwork(network);
                        break;
                    }
                }
            }
            catch { }

            #endregion
            // Name of the MainActivity theme you had there before.
            // Or you can use global::Android.Resource.Style.ThemeHoloLight
            base.SetTheme(Resource.Style.MyTheme);


            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);

            LoadApplication(new App());
        }
    }
}