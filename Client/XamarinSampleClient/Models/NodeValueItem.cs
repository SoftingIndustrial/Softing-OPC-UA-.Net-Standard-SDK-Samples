/* ========================================================================
 * Copyright © 2011-2022 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Text;
using XamarinSampleClient.Helpers;

namespace XamarinSampleClient.Models
{
    /// <summary>
    /// Model class for an node value item
    /// </summary>
    [Xamarin.Forms.Internals.Preserve(AllMembers = true)]
    class NodeValueItem : ObservableObject
    {
        #region Private Fields
        private object m_value;

        #endregion

        #region Properties
        /// <summary>
        /// Node Id
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Type name
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Flag that indicates if field is editable
        /// </summary>
        public bool IsEditable { get; set; }
        /// <summary>
        /// Value property
        /// </summary>
        public object Value
        {
            get { return m_value; }
            set { SetProperty(ref m_value, value); }
        }

        #endregion
    }
}
