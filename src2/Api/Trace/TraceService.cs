using Opc.Ua.Toolkit.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit.Trace
{
    /// <summary>
    /// A class that provides trace functionality.
    /// </summary>
    /// <include file='Doc\TraceService.xml' path='class[@name="TraceService"]/*'/>  
    public static class TraceService
    {

        #region Public Events
        /// <summary>
        /// Occurs when a Log operation is performed.
        /// </summary>        
        public static event EventHandler<TraceEventArgs> TraceEvent;
        #endregion



        ///// <summary>
        ///// Logs the trace message with the specified parameters.
        ///// </summary>
        ///// <param name="traceSource">The trace mask.</param>
        ///// <param name="message">The message.</param>
        ///// <param name="args">The parameters for formatted message.</param>
        //public static void Log(TraceSources traceSource, , string objectId, string message, params object[] args)
        //{
        //    Utils.Trace((int)traceSource, message, args);
        //}

        ///// <summary>
        ///// Logs the trace message with the specified parameters.
        ///// </summary>
        ///// <param name="exception">The exception that will be logged.</param>
        ///// <param name="exception">The exception that will be logged.</param>
        ///// <param name="objectId">The object id issuing the log message.</param>
        ///// <param name="message">The message.</param>
        ///// <param name="args">The parameters for formatted message.</param>
        //public static void Log(TraceSources traceSource, Exception exception, string objectId, string message, params object[] args)
        //{
        //    if (objectId != null)
        //    {
        //        message = $"{objectId} {message} {GetTraceSourceString(traceSource)}";
        //    }
        //    Utils.Trace(exception, message, args);
        //}

        /// <summary>
        /// Logs the trace message with the specified parameters.
        /// </summary>
        /// <param name="traceMask">The trace mask.</param>
        /// <param name="traceSource">The trace source.</param>
        /// <param name="objectId">The object id.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">The parameters for formatted message.</param>
        public static void Log(TraceMasks traceMask, TraceSources traceSource, string objectId, string message, params object[] args)
        {
            if (message == null)
            {
                message = "";
            }
            if (objectId != null)
            {
                message = $"{GetTraceSourceString(traceSource)}: {objectId} {message} ";
            }
            Utils.Trace((int)traceMask, message, args);
        }


        /// <summary>
        /// Logs the trace message with the specified parameters.
        /// </summary>
        /// <param name="traceMask">The trace mask.</param>
        /// <param name="traceSource">The trace source.</param>
        /// <param name="objectId">The object id.</param>           
        /// <param name="exception">The exception.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">The parameters for formatted message.</param>
        public static void Log(TraceMasks traceMask, TraceSources traceSource, string objectId, Exception exception, string message = "", params object[] args)
        {            
            if (objectId != null)
            {
                message = $"{GetTraceSourceString(traceSource)}: {objectId} {message} ";
            }
            if (exception != null)
            {
                Utils.Trace(exception, message, args);
            }            
        }

        /// <summary>
        /// Gets the trace mask string.
        /// </summary>
        /// <param name="traceSource">The trace mask.</param>
        /// <returns></returns>
        private static string GetTraceSourceString(TraceSources traceSource)
        {
            switch (traceSource)
            {
                case TraceSources.ClientSDK:
                    return "CLTS";
                case TraceSources.Core:
                    return "CORE";
                case TraceSources.ServerSDK:
                    return "SRVS";
                case TraceSources.ConfigurationSDK:
                    return "CNFG";
                case TraceSources.StackTrace:
                    return "STTR";
                case TraceSources.OldTrace:
                    return "OLDT";
                case TraceSources.ClientAPI:
                    return "CLT_API";
                case TraceSources.ServerAPI:
                    return "SRV_API";
                case TraceSources.User3:
                    return "USR3";
                case TraceSources.User4:
                    return "USR4";
                case TraceSources.User5:
                    return "USR5";
                case TraceSources.User6:
                    return "USR6";
                case TraceSources.User7:
                    return "USR7";
                case TraceSources.User8:
                    return "USR8";
                default:
                    return ((UInt32)traceSource).ToString("X8");
            }
        }
        /*
        private static void HandleTraceEvent(object sender, TraceEventArgs e)
        {
            try
            {
                if (TraceEvent != null)
                {
                    // Apply the filters according to trace configuration
                    if (Configuration != null && Configuration.Trace != null &&
                        (TraceLevels)e.TraceLevel >= Configuration.Trace.Tracelevel &&
                        ((uint)e.TraceMask & Configuration.Trace.TraceMask) > 0)
                    {
                        TraceEventArgs eventArgs = new TraceEventArgs(e);

                        // Raise the trace event for all the subscribers (async)
                        foreach (EventHandler<TraceEventArgs> receiver in TraceEvent.GetInvocationList())
                        {
                            receiver.BeginInvoke(sender, eventArgs, null, null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TraceService.Log(TraceLevels.Error, TraceMasks.ClientAPI, "Application.HandleTraceEvent", ex.Message, ex);
            }
        }
        */
    }
}
