/* ========================================================================
 * Copyright © 2011-2019 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 * 
 * ======================================================================*/

using System.Collections.Generic;
using XamarinSampleClient.Helpers;

namespace XamarinSampleClient.Models
{
    /// <summary>
    /// Display object for a Complex value object read from server
    /// </summary>
    [Xamarin.Forms.Internals.Preserve(AllMembers = true)]
    class ComplexValueItem : ObservableObject
    {
        /// <summary>
        /// Create new instance of ComplexValueItem
        /// </summary>
        public ComplexValueItem()
        {
            Fields = new List<ComplexValueFieldItem>();
        }

        #region Properties

        /// <summary>
        /// List of fields
        /// </summary>
        public List<ComplexValueFieldItem> Fields { get; }

        #endregion
    }
}
