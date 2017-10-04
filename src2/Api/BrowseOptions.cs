using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit
{
    /// <summary>
    /// The class that holds the properties for the browsing options, e.g. direction to browse, number of references to return, etc.
    /// </summary>
    public class BrowseOptions : BrowseDescription
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="BrowseOptions"></see> class.
        /// </summary>
        public BrowseOptions()
        {          
            ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences;
            MaxReferencesReturned = 1000;
        }

        #endregion        
        
        #region Properties

        /// <summary>
        /// Gets or sets the maximum number of references to return for a Browse() service call.
        /// The value 0 indicates that no limitation is imposed.
        /// </summary>
        public uint MaxReferencesReturned
        {
            get;
            set;
        }       

        /// <summary>
        /// Gets or sets a value indicating whether subsequent continuation points should be processed automatically.
        /// If there are alot of references to return then they will be returned in batches, each batch is marked by a continuation point.
        /// </summary>
        public bool ContinueUntilDone
        {
            get;
            set;
        }

        #endregion 
    }
}
