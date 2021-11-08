/* ========================================================================
 * Copyright © 2011-2021 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA_SIA_EN
 * 
 * ======================================================================*/


using System;
using System.Globalization;
using Xamarin.Forms;

namespace XamarinSampleClient.Converters
{
    /// <summary>
    /// Converts null value to false
    /// </summary>
    class NullToFalseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value == null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
