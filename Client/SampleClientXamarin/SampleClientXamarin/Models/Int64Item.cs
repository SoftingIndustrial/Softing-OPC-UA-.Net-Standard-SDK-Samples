/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 * 
 * ======================================================================*/

using System;
using SampleClientXamarin.Helpers;

namespace SampleClientXamarin.Models
{
    /// <summary>
    /// Model class for an Int64 item
    /// </summary>
    class Int64Item : ObservableObject
    {
        #region Private Fields

        private Int64 m_value;

        #endregion

        #region Properties

        /// <summary>
        /// Value propertu
        /// </summary>
        public long Value
        {
            get { return m_value; }
            set { SetProperty(ref m_value, value); }
        }

        #endregion
    }
}
