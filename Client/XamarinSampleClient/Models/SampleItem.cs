﻿/* ========================================================================
 * Copyright © 2011-2025 Softing Industrial Automation GmbH. 
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
    /// Model class for one sample available in this application
    /// </summary>
    [Xamarin.Forms.Internals.Preserve(AllMembers = true)]
    class SampleItem
    {
        /// <summary>
        /// Sample Display Name 
        /// </summary>
        public string SampleName { get; set; }

        /// <summary>
        /// Sample Command
        /// </summary>
        public SampleCommand Command { get; set; }

        /// <summary>
        /// Sample descriptive text
        /// </summary>
        public string Description { get; set; }
    }
}
