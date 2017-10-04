namespace TestServer
{
    public class NodeIdInfo
    {
        public ulong StartIndex { get; set; }
        public ulong EndIndex { get; set; }
        public bool StringIds { get; set; }
        public string Pattern { get; set; }
        public uint RepeatCount { get; set; }
        public uint Interval { get; set; }
        public double Increment { get; set; }
        public NodeIdType NodeType { get; set; }
        public VarType VariableDataType { get; set; }

        public bool isValid() 
	    {
            if (StartIndex >= EndIndex)
            {
                return false;
            }

		    if (NodeType == NodeIdType.StringNodeId)
		    {
                if (Pattern.Length == 0)
                {
                    return false;
                }
		    }

            if (NodeType < NodeIdType.StringNodeId || NodeType > NodeIdType.NumericNodeId)
            {
                return false;
            }

            if (VariableDataType < VarType.Uint8 || VariableDataType > VarType.Double)
            {
                return false;
            }

		    return true;
	    }

        public override string ToString()
        {
            string result = "NodeId of type ";
		    if (NodeType == NodeIdType.NumericNodeId)
		    {
			    result += "numeric, ";
		    }
		    else
		    {
			    if (NodeType == NodeIdType.StringNodeId)
			    {
				    result += "string, ";
				    result += Pattern;
				    result += " ";
			    }
		    }
		    
		    result += string.Format("start index: {0}, end index: {1}\r\n", StartIndex, EndIndex);
		    return result;
        }

        public bool AreEqual(NodeIdInfo nodeIdInfo) 
	    {
		    bool result = false;
		    if (NodeType == nodeIdInfo.NodeType)
		    {
			    if (StartIndex == nodeIdInfo.StartIndex && 
				    EndIndex == nodeIdInfo.EndIndex)
			    {
				    if (NodeType == NodeIdType.NumericNodeId)
				    {
					    result = true;
				    }
				    else
				    {
                        if (NodeType == NodeIdType.StringNodeId)
					    {
                            if (string.Compare(Pattern, nodeIdInfo.Pattern) == 0)
						    {
							    result = true;
						    }
					    }
				    }
			    }
		    }
		    return result;
	    }

        public bool AreIntersecting(NodeIdInfo nodeIdInfo)
        {
            if (NodeType != nodeIdInfo.NodeType)
            {
                return false;
            }
            if (NodeType == NodeIdType.StringNodeId)
            {
                if (string.Compare(Pattern, nodeIdInfo.Pattern) != 0)
                {
                    return false;
                }
            }
            return !((StartIndex < nodeIdInfo.StartIndex && EndIndex < nodeIdInfo.StartIndex) ||
                (EndIndex > nodeIdInfo.EndIndex && StartIndex > nodeIdInfo.EndIndex));
        }

        public bool IsString
        {
            get
            {
                return NodeType == NodeIdType.StringNodeId;
            }
        }
    }

    public enum NodeIdType
    {
        StringNodeId,
        NumericNodeId
    }

    public enum VarType
    {
        Unknown = 0,
        Uint8 = 1,
        Int8 = 2,
        Uint16 = 3,
        Int16 = 4,
        Uint32 = 5,
        Int32 = 6,
        Uint64 = 7,
        Int64 = 8,
        Float = 9,
        Double = 10,
    }
}