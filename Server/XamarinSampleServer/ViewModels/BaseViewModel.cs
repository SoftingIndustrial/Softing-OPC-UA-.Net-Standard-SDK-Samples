﻿/* ========================================================================
 * Copyright © 2011-2019 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * ======================================================================*/

using XamarinSampleServer.Helpers;

namespace XamarinSampleServer.ViewModels
{
	public class BaseViewModel : ObservableObject
	{
		bool m_isBusy = false;
	    /// <summary>
	    /// Private backing field to hold the title
	    /// </summary>
	    string m_title = string.Empty;

        /// <summary>
        /// Public property to set and get indicator if item is busy
        /// </summary>
        public bool IsBusy
		{
			get { return m_isBusy; }
			set { SetProperty(ref m_isBusy, value); }
		}
		
		/// <summary>
		/// Public property to set and get the title of the item
		/// </summary>
		public string Title
		{
			get { return m_title; }
			set { SetProperty(ref m_title, value); }
		}
	}
}

