using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit
{
    /// <summary>
    /// UA Exception class that wraps the exceptions raised from the lower layers
    /// </summary>
    [Serializable]
    public class BaseException : Exception
    {
        #region Fields
        private StatusCode m_statusCode;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the status code.
        /// </summary>
        public StatusCode StatusCode
        {
            get
            {
                return m_statusCode;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseException"/> class.
        /// </summary>
        public BaseException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public BaseException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="innerException">The inner exception.</param>
        public BaseException(string message, Exception innerException)
            : base(message, innerException)
        {
            ServiceResultException sre = innerException as ServiceResultException;

            if (sre != null)
            {
                m_statusCode = new StatusCode(sre.StatusCode);
            }
        }       

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="sre">The Service Result Exception.</param>
        public BaseException(string message, ServiceResultException sre)
            : base(message, sre)
        {
            if (sre != null)
            {
                m_statusCode = new StatusCode(sre.StatusCode);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="code">The code.</param>
        public BaseException(string message, uint code)
            : base(message)
        {
            m_statusCode = new StatusCode(code);
        }
        #endregion
        
    }
}
