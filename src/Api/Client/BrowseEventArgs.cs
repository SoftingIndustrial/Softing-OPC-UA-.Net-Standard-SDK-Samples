using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Opc.Ua.Toolkit.Client
{
    /// <summary>
    /// Represents the event arguments provided by the browse operation that returns a continuation point.
    /// </summary>
    public class BrowseEventArgs : CancelEventArgs
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BrowseEventArgs"></see> class.
        /// </summary>
        internal BrowseEventArgs()
        {
            Cancel = false;
        }

        #endregion Constructors
    }
}
