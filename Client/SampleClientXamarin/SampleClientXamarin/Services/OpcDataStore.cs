using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using SampleClientXamarin.Models;

using Xamarin.Forms;
using Softing.Opc.Ua;
using Opc.Ua;
using Softing.Opc.Ua.Client;

[assembly: Dependency(typeof(SampleClientXamarin.Services.OpcDataStore))]
namespace SampleClientXamarin.Services
{
    class OpcDataStore : IDataStore<Item>
    {
        bool isInitialized;
        List<Item> items;

        public async Task<bool> AddItemAsync(Item item)
        {
            await InitializeAsync();

            items.Add(item);

            return await Task.FromResult(true);
        }

        public async Task<bool> UpdateItemAsync(Item item)
        {
            await InitializeAsync();

            var _item = items.Where((Item arg) => arg.Id == item.Id).FirstOrDefault();
            items.Remove(_item);
            items.Add(item);

            return await Task.FromResult(true);
        }

        public async Task<bool> DeleteItemAsync(Item item)
        {
            await InitializeAsync();

            var _item = items.Where((Item arg) => arg.Id == item.Id).FirstOrDefault();
            items.Remove(_item);

            return await Task.FromResult(true);
        }

        public async Task<Item> GetItemAsync(string id)
        {
            await InitializeAsync();

            return await Task.FromResult(items.FirstOrDefault(s => s.Id == id));
        }

        public async Task<IEnumerable<Item>> GetItemsAsync(bool forceRefresh = false)
        {
            await InitializeAsync();

            return await Task.FromResult(items);
        }

        public Task<bool> PullLatestAsync()
        {
            return Task.FromResult(true);
        }


        public Task<bool> SyncAsync()
        {
            return Task.FromResult(true);
        }

        public async Task InitializeAsync()
        {
            if (isInitialized)
                return;

            ClientSession session;
            try
            {
                var configuration = CreateAplicationConfiguration();
                UaApplication application = UaApplication.Create(configuration).Result;

                // Create the Session object.
                session  = application.CreateSession("opc.tcp://wboaw10:61510/SampleServer", MessageSecurityMode.Sign, SecurityPolicy.Basic256Sha256);

                session.Connect(true, true);
            }
            catch (Exception ex)
            {
                
            }


            items = new List<Item>();
            var _items = new List<Item>
            {
                new Item { Id = Guid.NewGuid().ToString(), Text = "Buy some cat food", Description="The cats are hungry"},
                new Item { Id = Guid.NewGuid().ToString(), Text = "Learn F#", Description="Seems like a functional idea"},
                new Item { Id = Guid.NewGuid().ToString(), Text = "Learn to play guitar", Description="Noted"},
                new Item { Id = Guid.NewGuid().ToString(), Text = "Buy some new candles", Description="Pine and cranberry for that winter feel"},
                new Item { Id = Guid.NewGuid().ToString(), Text = "Complete holiday shopping", Description="Keep it a secret!"},
                new Item { Id = Guid.NewGuid().ToString(), Text = "Finish a todo list", Description="Done"}
            };

            foreach (Item item in _items)
            {
                items.Add(item);
            }

            isInitialized = true;
        }

        /// <summary>
        /// Creates Application's ApplicationConfiguration programmatically
        /// </summary>
        /// <returns></returns>
        private ApplicationConfigurationEx CreateAplicationConfiguration()
        {
            Console.WriteLine("Creating ApplicationConfigurationEx for current UaApplication...");
            ApplicationConfigurationEx configuration = new ApplicationConfigurationEx();

            configuration.ApplicationName = "UA Sample Client";
            configuration.ApplicationType = ApplicationType.Client;
            configuration.ApplicationUri = $"urn:{Utils.GetHostName()}:OPCFoundation:SampleClient";
            configuration.TransportConfigurations = new TransportConfigurationCollection();
            configuration.TransportQuotas = new TransportQuotas { OperationTimeout = 15000 };
            configuration.ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 5000 };

            configuration.TraceConfiguration = new TraceConfiguration()
            {
                OutputFilePath = @"%CommonApplicationData%\Softing\OpcUaNetStandardToolkit\logs\SampleClient.log",
                TraceMasks = 519
            };

            string myDocsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            configuration.SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = myDocsFolder + @"\pki\own"
                },
                TrustedPeerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = myDocsFolder + @"\pki\trusted",
                },
                TrustedIssuerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = myDocsFolder + @"\pki\issuer",
                },
                RejectedCertificateStore = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = myDocsFolder + @"\pki\rejected",
                },
                AutoAcceptUntrustedCertificates = true
            };

            return configuration;
        }
    }
}
