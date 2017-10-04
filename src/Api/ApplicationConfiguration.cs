
/* ========================================================================
 * Copyright © 2011-2017 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 *  
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Text;
using Opc.Ua;

namespace Opc.Ua.Toolkit
{
    /// <summary>
    /// Represents the Application Configuration, sets default properties(ex: OperationTimeout, MaxByteStringLength) and validates the application
    /// </summary>
    public class ApplicationConfiguration
    {

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationConfiguration"/> class.
        ///  Initializes an Internal Application Configuration, a Security configuration and a Trace Configuration for logging events.
        /// </summary>
        public ApplicationConfiguration()
        {
            InternalApplicationConfiguration = new Opc.Ua.ApplicationConfiguration();
            InternalApplicationConfiguration.TransportQuotas = new Opc.Ua.TransportQuotas();

            InternalApplicationConfiguration.ApplicationType = Opc.Ua.ApplicationType.Client;

            Client = new Opc.Ua.ClientConfiguration();
            Security = new Opc.Ua.SecurityConfiguration();
            Trace = new TraceConfiguration();
        }
        #endregion

        #region Internal Properties
        internal Opc.Ua.ApplicationConfiguration InternalApplicationConfiguration
        {
            get;
            set;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the name of the application.
        /// </summary>
        public string ApplicationName
        {
            get
            {
                return InternalApplicationConfiguration.ApplicationName;
            }

            set
            {
                InternalApplicationConfiguration.ApplicationName = value;
            }
        }

        /// <summary>
        /// Gets or sets the unique identifier of the product.
        /// </summary>
        public string ProductUri
        {
            get
            {
                return InternalApplicationConfiguration.ProductUri;
            }

            set
            {
                InternalApplicationConfiguration.ProductUri = value;
            }
        }

        /// <summary>
        /// Gets or sets the type of the application.
        /// </summary>
        /// <value>
        /// The type of the application.
        /// </value>
        public ApplicationType ApplicationType
        {
            get
            {
                return (ApplicationType)InternalApplicationConfiguration.ApplicationType;
            }
            set
            {
                InternalApplicationConfiguration.ApplicationType = (Opc.Ua.ApplicationType)value;
            }
        }

        /// <summary>
        /// Gets or sets the default timeout to use when sending requests.
        /// </summary>
        public int OperationTimeout
        {
            get
            {
                return InternalApplicationConfiguration.TransportQuotas.OperationTimeout;
            }
            set
            {
                InternalApplicationConfiguration.TransportQuotas.OperationTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum length of string encoded in a message body.
        /// </summary>
        public int MaxStringLength
        {
            get
            {
                return InternalApplicationConfiguration.TransportQuotas.MaxStringLength;
            }
            set
            {
                InternalApplicationConfiguration.TransportQuotas.MaxStringLength = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum length of a byte string encoded in a message body.
        /// </summary>
        public int MaxByteStringLength
        {
            get { return InternalApplicationConfiguration.TransportQuotas.MaxByteStringLength; }
            set { InternalApplicationConfiguration.TransportQuotas.MaxByteStringLength = value; }
        }

        /// <summary>
        /// Gets or sets the maximum length of an array encoded in a message body.
        /// </summary>
        public int MaxArrayLength
        {
            get { return InternalApplicationConfiguration.TransportQuotas.MaxArrayLength; }
            set { InternalApplicationConfiguration.TransportQuotas.MaxArrayLength = value; }
        }

        /// <summary>
        /// Gets or sets the maximum length of a message body.
        /// </summary>
        public int MaxMessageSize
        {
            get { return InternalApplicationConfiguration.TransportQuotas.MaxMessageSize; }
            set { InternalApplicationConfiguration.TransportQuotas.MaxMessageSize = value; }
        }

        /// <summary>
        /// Gets or sets the maximum size of the buffer to use when sending messages.
        /// </summary>
        public int MaxBufferSize
        {
            get { return InternalApplicationConfiguration.TransportQuotas.MaxBufferSize; }
            set { InternalApplicationConfiguration.TransportQuotas.MaxBufferSize = value; }
        }

        /// <summary>
        /// Gets or sets the lifetime of a secure channel.
        /// </summary>
        public int ChannelLifetime
        {
            get { return InternalApplicationConfiguration.TransportQuotas.ChannelLifetime; }
            set { InternalApplicationConfiguration.TransportQuotas.ChannelLifetime = value; }
        }

        /// <summary>
        /// Gets or sets the lifetime of a security token.
        /// </summary>
        public int SecurityTokenLifetime
        {
            get { return InternalApplicationConfiguration.TransportQuotas.SecurityTokenLifetime; }
            set { InternalApplicationConfiguration.TransportQuotas.SecurityTokenLifetime = value; }
        }

        /// <summary>
        /// Gets or sets the documentation file path.
        /// </summary>
        /// <value>
        /// The documentation file path.
        /// </value>
        public string DocumentationFilePath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the Client configuration.
        /// </summary>
        /// <value>
        /// The Client configuration.
        /// </value>
        public Opc.Ua.ClientConfiguration Client
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the security configuration.
        /// </summary>
        /// <value>
        /// The security configuration.
        /// </value>
        public Opc.Ua.SecurityConfiguration Security
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the trace configuration.
        /// </summary>
        /// <value>
        /// The trace configuration.
        /// </value>
        public Opc.Ua.TraceConfiguration Trace
        {
            get;
            set;
        }
        #endregion

        #region Events
        /// <summary>
        /// Occurs when [on create application instance certificate].
        /// </summary>
        public event EventHandler<CreateApplicationInstanceCertificateEventArgs> ApplicationInstanceCertificateCreated;
        #endregion

        #region Public Methods
        /// <summary>
        /// Validates this instance.
        /// </summary>
        public void Validate()
        {
            InternalApplicationConfiguration.Validate(Application.ApplicationInstance.ApplicationType);

            if (InternalApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate == null)
            {
                if (Application.Configuration.Security.ApplicationCertificateSubject.StartsWith("CN=", StringComparison.CurrentCultureIgnoreCase))
                {
                    Dictionary<string, string> subjectParts = new Dictionary<string, string>();
                    string[] parts = Application.Configuration.Security.ApplicationCertificateSubject.Split(',');
                    foreach (var part in parts)
                    {
                        string[] values = part.Split('=');
                        if (values.Length == 2)
                        {
                            subjectParts.Add(values[0].Trim(), values[1].Trim());
                        }
                    }

                    string commonName;
                    if (subjectParts.TryGetValue("CN", out commonName))
                    {
                        Application.Configuration.Security.ApplicationCertificateSubject = commonName;
                    }
                }

                StringBuilder buffer = new StringBuilder();

                buffer.Append("urn:");
                buffer.Append(System.Net.Dns.GetHostName());
                buffer.Append(":");
                buffer.Append(ApplicationName.Replace(" ", string.Empty));

                InternalApplicationConfiguration.ApplicationUri = buffer.ToString();

                if (ApplicationInstanceCertificateCreated != null)
                {
                    string message = "Before you can use the Client an application instance certificate which will identify this installation must be created.\r\n"
                                    + "A certificate with the following information will be generated :\r\n\r\n"
                                    + "Application Uri : " + InternalApplicationConfiguration.ApplicationUri + "\r\n"
                                    + "Subject Name : " + Application.Configuration.Security.ApplicationCertificateSubject + "\r\n"
                                    + "Key Size : 2048 Bits\r\n"
                                    + "Validity : 600 months";

                    ApplicationInstanceCertificateCreated(this, new CreateApplicationInstanceCertificateEventArgs(message));
                }

                CreateApplicationInstanceCertificate();
            }

            // ensure the application uri matches the certificate.
            string applicationUri = ToolkitUtils.GetApplicationUriFromCertficate(InternalApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.Certificate);

            if (applicationUri != null)
            {
                InternalApplicationConfiguration.ApplicationUri = applicationUri;
            }

            Application.OnConfigurationValidated();
        }
        #endregion

        #region Private Methods
        private static void CreateApplicationInstanceCertificate()
        {
            Application.ApplicationInstance.CheckApplicationInstanceCertificate(true, 0);
        }
        #endregion
    }
    /// <summary>
    /// Event arguments for the creation of the instance application certificate event.
    /// </summary>
    public class CreateApplicationInstanceCertificateEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public string Message
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateApplicationInstanceCertificateEventArgs"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        internal CreateApplicationInstanceCertificateEventArgs(string message)
        {
            Message = message;
        }
    }
}