/* ========================================================================
 * Copyright © 2011-2021 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA_SIA_EN
 * 
 * ======================================================================*/

using Android;
using Android.App;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Android.Runtime;

namespace XamarinSampleServer.Droid
{
    [Activity(Label = "Opc UA Server", Theme = "@style/splashscreen", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {           
            // Name of the MainActivity theme you had there before.
            // Or you can use global::Android.Resource.Style.ThemeHoloLight
            base.SetTheme(Resource.Style.MyTheme);


            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);

            CheckAppPermissions();
        }

        private void CheckAppPermissions()
        {
            if ((int)Build.VERSION.SdkInt < 23)
            {
                return;
            }
            else
            {
                if (PackageManager.CheckPermission(Manifest.Permission.ReadExternalStorage, PackageName) != Permission.Granted
                    && PackageManager.CheckPermission(Manifest.Permission.WriteExternalStorage, PackageName) != Permission.Granted)
                {
                    var permissions = new string[] { Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage };
                    RequestPermissions(permissions, 1);
                }
                else
                {
                    LoadApplication(new App());
                }
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (PackageManager.CheckPermission(Manifest.Permission.ReadExternalStorage, PackageName) == Permission.Granted
                    && PackageManager.CheckPermission(Manifest.Permission.WriteExternalStorage, PackageName) == Permission.Granted)
            {
                LoadApplication(new App());
            }
            else
            {
                AlertDialog.Builder alertDiag = new AlertDialog.Builder(this);
                alertDiag.SetTitle("Warning");
                alertDiag.SetMessage("The Sample Client cannot execute without the requested permissions!");

                alertDiag.SetPositiveButton("OK", (senderAlert, args) => {
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                });

                Dialog diag = alertDiag.Create();
                diag.Show();
            }
        }
    }
}