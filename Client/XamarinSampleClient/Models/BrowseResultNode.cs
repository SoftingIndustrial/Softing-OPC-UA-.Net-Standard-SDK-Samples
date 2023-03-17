/* ========================================================================
 * Copyright © 2011-2023 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/
 
namespace XamarinSampleClient.Models
{
    /// <summary>
    /// Model class for the Browse result
    /// </summary>
    [Xamarin.Forms.Internals.Preserve(AllMembers = true)]
    class BrowseResultNode
    {
        #region Public Properties

        /// <summary>
        /// Text property
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Additional information
        /// </summary>
        public string Info { get; set; }

        #endregion
    }
}
