using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit.Trace
{
    /// <summary>
    /// Source format provider - helps specify personalized source string in the logs
    /// </summary>
    public interface ITraceSourceFormatProvider
    {
        /// <summary>
        /// Gets the trace source string.
        /// </summary>
        /// <param name="traceSource">The trace source.</param>
        /// <returns></returns>
        string GetTraceSourceString(TraceSources traceSource);
    }
}
