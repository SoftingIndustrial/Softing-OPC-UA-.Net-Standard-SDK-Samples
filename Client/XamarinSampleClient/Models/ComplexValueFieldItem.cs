/* ========================================================================
 * Copyright © 2011-2022 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 * 
 * ======================================================================*/

using XamarinSampleClient.Helpers;

namespace XamarinSampleClient.Models
{
    /// <summary>
    /// Display object for a complex value field
    /// </summary>
    [Xamarin.Forms.Internals.Preserve(AllMembers = true)]
    class ComplexValueFieldItem : ObservableObject
    {

        #region Private Fields

        private bool m_isEditable;
        private object m_value;
        private string m_typeName;
        private string m_fieldName;

        #endregion

        #region Constructor
        /// <summary>
        /// Create new instance of ComplexValueFieldItem
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="typeName"></param>
        /// <param name="value"></param>
        public ComplexValueFieldItem(string fieldName, string typeName, object value)
        {
            m_fieldName = fieldName;
            m_typeName = typeName;
            m_value = value;
        }
        #endregion


        #region Properties
        /// <summary>
        /// Type name 
        /// </summary>
        public string TypeName
        {
            get { return m_typeName; }
            set { SetProperty(ref m_typeName, value); }
        }

        /// <summary>
        /// Field name
        /// </summary>
        public string FieldName
        {
            get { return m_fieldName; }
            set { SetProperty(ref m_fieldName, value); }
        }

        /// <summary>
        /// Field value
        /// </summary>
        public object Value
        {
            get { return m_value; }
            set { SetProperty(ref m_value, value); }
        }

        /// <summary>
        /// Flag that indicates if item value is editable
        /// </summary>
        public bool IsEditable
        {
            get { return m_isEditable; }
            set { SetProperty(ref m_isEditable, value); }
        }
        #endregion
    }
}
