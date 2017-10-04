using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Opc.Ua.Toolkit
{
    public static class ToolkitUtils
    {
        /// <summary>
        /// Extracts the the application URI specified in the certificate.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        /// <returns>The application URI.</returns>
        public static string GetApplicationUriFromCertficate(X509Certificate2 certificate)
        {
#if !SILVERLIGHT
            // extract the alternate domains from the subject alternate name extension.
            X509SubjectAltNameExtension alternateName = null;

            foreach (X509Extension extension in certificate.Extensions)
            {
                if (extension.Oid.Value == X509SubjectAltNameExtension.SubjectAltNameOid || extension.Oid.Value == X509SubjectAltNameExtension.SubjectAltName2Oid)
                {
                    alternateName = new X509SubjectAltNameExtension(extension, extension.Critical);
                    break;
                }
            }

            // get the application uri.
            if (alternateName != null && alternateName.Uris.Count > 0)
            {
                return alternateName.Uris[0];
            }
#endif

            // return the list.
            return null;
        }


        /// <summary>
        /// When connecting to a server, this method adds that servers certificate to the application's trusted store.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        internal static void AddCertificateToStore(CertificateStoreIdentifier store, X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new System.ArgumentNullException("certificate");
            }

            if (store != null)
            {
                try
                {
                    ICertificateStore certificateStore = store.OpenStore();
                    if (certificateStore != null)
                    {
                        certificateStore.Add(certificate);
                    }

                    string logMessage = string.Format("Certificate with SubjectName \"{0}\" added to Trusted Store", certificate.Subject);
                    //TraceService.Log(TraceMasks.ClientAPI, logMessage);
                }
                catch (Exception ex)
                {
                   // TraceService.Log(TraceMasks.ClientAPI, ex.Message);
                }
            }
        }
    }
}
