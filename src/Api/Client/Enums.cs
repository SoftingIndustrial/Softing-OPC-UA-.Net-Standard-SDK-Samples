using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit.Client
{
    /// <summary>
    /// Represents the object state
    /// </summary>
    public enum State
    {
        /// <summary>
        /// The object is in Disconnected state
        /// </summary>
        Disconnected,
        /// <summary>
        /// The object is in Connected state
        /// </summary>
        Connected,
        /// <summary>
        /// The object is in Active state
        /// </summary>
        Active
    }

    /// <summary>
    /// Specifies the possible message encodings.
    /// </summary>
    public enum MessageEncoding
    {
        /// <summary>
        /// The default or native binary encoding.
        /// </summary>
        Binary,
        /// <summary>
        /// The default XML encoding.
        /// </summary>
        Xml
    }

    /// <summary>
    /// The list of attribute id
    /// </summary>
    public enum AttributeId : uint
    {
        /// <summary>
        /// The canonical identifier for the node.
        /// </summary>
        NodeId = 1,

        /// <summary>
        /// The class of the node.
        /// </summary>
        NodeClass = 2,

        /// <summary>
        /// A non-localized, human readable name for the node.
        /// </summary>
        BrowseName = 3,

        /// <summary>
        /// A localized, human readable name for the node.
        /// </summary>
        DisplayName = 4,

        /// <summary>
        /// A localized description for the node.
        /// </summary>
        Description = 5,

        /// <summary>
        /// Indicates which attributes are writeable.
        /// </summary>
        WriteMask = 6,

        /// <summary>
        /// Indicates which attributes are writeable by the current user.
        /// </summary>
        UserWriteMask = 7,

        /// <summary>
        /// Indicates that a type node may not be instantiated.
        /// </summary>
        IsAbstract = 8,

        /// <summary>
        /// Indicates that forward and inverse references have the same meaning.
        /// </summary>
        Symmetric = 9,

        /// <summary>
        /// The browse name for an inverse reference.
        /// </summary>
        InverseName = 10,

        /// <summary>
        /// Indicates that following forward references within a view will not cause a loop.
        /// </summary>
        ContainsNoLoops = 11,

        /// <summary>
        /// Indicates that the node can be used to subscribe to events.
        /// </summary>
        EventNotifier = 12,

        /// <summary>
        /// The value of a variable.
        /// </summary>
        Value = 13,

        /// <summary>
        /// The node id of the data type for the variable value.
        /// </summary>
        DataType = 14,

        /// <summary>
        /// The number of dimensions in the value.
        /// </summary>
        ValueRank = 15,

        /// <summary>
        /// The length for each dimension of an array value.
        /// </summary>
        ArrayDimensions = 16,

        /// <summary>
        /// How a variable may be accessed.
        /// </summary>
        AccessLevel = 17,

        /// <summary>
        /// How a variable may be accessed after taking the user's access rights into account.
        /// </summary>
        UserAccessLevel = 18,

        /// <summary>
        /// Specifies (in milliseconds) how fast the server can reasonably sample the value for changes.
        /// </summary>
        MinimumSamplingInterval = 19,

        /// <summary>
        /// Specifies whether the server is actively collecting historical data for the variable.
        /// </summary>
        Historizing = 20,

        /// <summary>
        /// Whether the method can be called.
        /// </summary>
        Executable = 21,

        /// <summary>
        /// Whether the method can be called by the current user.
        /// </summary>
        UserExecutable = 22
    }

    /// <summary>
    /// Value rank enumeration.
    /// </summary>
    public enum ValueRanks : int
    {
        /// <summary>
        /// The variable may be a scalar or a one dimensional array.
        /// </summary>
        ScalarOrOneDimension = -3,

        /// <summary>
        /// The variable may be a scalar or an array of any dimension.
        /// </summary>
        Any = -2,

        /// <summary>
        /// The variable is always a scalar.
        /// </summary>
        Scalar = -1,

        /// <summary>
        /// The variable is always an array with one or more dimensions.
        /// </summary>
        OneOrMoreDimensions = 0,

        /// <summary>
        /// The variable is always one dimensional array.
        /// </summary>
        OneDimension = 1,

        /// <summary>
        /// The variable is always an array with two or more dimensions.
        /// </summary>
        TwoDimensions = 2
    }
}
