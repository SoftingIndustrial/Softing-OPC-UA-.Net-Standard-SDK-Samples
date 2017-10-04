using System.Collections.Generic;
using TestServer.StatisticsModule;
using Opc.Ua.Server;
using Opc.Ua;
using TestServer.AlarmsAndConditions;
using TestServer.EventingModule;

namespace TestServer
{
	public class TestServer : StandardServer
	{
		#region Overridden Methods
		/// <summary>
		/// Creates the node managers for the server.
		/// </summary>
		/// <remarks>
		/// This method allows the sub-class create any additional node managers which it uses. The SDK
		/// always creates a CoreNodeManager which handles the built-in nodes defined by the specification.
		/// Any additional NodeManagers are expected to handle application specific nodes.
		/// </remarks>
		protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
		{
			Utils.Trace("Creating the Node Managers.");

			List<INodeManager> nodeManagers = new List<INodeManager>();

            //instantiate modules
            SimulationModule.SimulationModule simulationModule = new SimulationModule.SimulationModule();
            INodeManager simulationNodeManager = simulationModule.GetNodeManager(server, configuration);
            ApplicationModule.Instance.RegisterNodeManager(simulationNodeManager);
            // create the custom node managers.
            nodeManagers.Add(simulationNodeManager);

            //init Test module
            INodeManager testModuleNodeManager = new TestModule.TestModuleNodeManager(server, configuration);
            ApplicationModule.Instance.RegisterNodeManager(testModuleNodeManager);
            nodeManagers.Add(testModuleNodeManager);

            ApplicationModule.Instance.TimerThread.Start();

            //init StatisticsModule module
            StatisticModule statisticModule = new StatisticModule();
            INodeManager statisticNodeManager = statisticModule.GetNodeManager(server, configuration);
            ApplicationModule.Instance.RegisterNodeManager(statisticNodeManager);
            //add to node manager
            nodeManagers.Add(statisticNodeManager);

            //init ComplexBrowse module
            ComplexBrowseModule.ComplexBrowseModule complexBrowseModule = new ComplexBrowseModule.ComplexBrowseModule();
            INodeManager complexBrowseNodeManager = complexBrowseModule.GetNodeManager(server, configuration);
            ApplicationModule.Instance.RegisterNodeManager(complexBrowseNodeManager);
            //add to node manager
            nodeManagers.Add(complexBrowseNodeManager);

            //init Eventing module
            INodeManager eventingModuleNodeManager = new EventingModuleNodeManager(server, configuration);
            ApplicationModule.Instance.RegisterNodeManager(eventingModuleNodeManager);
            nodeManagers.Add(eventingModuleNodeManager);

            //init HistoricalEvents
            INodeManager historicalEventsNodeManager = new HistoricalEvents.TestHistoricalEventsNodeManager(server, configuration);
            ApplicationModule.Instance.RegisterNodeManager(historicalEventsNodeManager);
            nodeManagers.Add(historicalEventsNodeManager);

            //init Alarms Module
            INodeManager alarmsNodeManager = new AlarmsNodeManager(server, configuration);
            ApplicationModule.Instance.RegisterNodeManager(alarmsNodeManager);
            nodeManagers.Add(alarmsNodeManager);


            // add the types defined in the quickstart information model library to the factory.
            server.Factory.AddEncodeableTypes(this.GetType().Assembly);

            // create the custom node managers.
            nodeManagers.Add(new Nodeset2xmlNodeManager(server, configuration));

            // create master node manager.
            return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
        }


		/// <summary>
		/// Loads the non-configurable properties for the application.
		/// </summary>
		/// <remarks>
		/// These properties are exposed by the server but cannot be changed by administrators.
		/// </remarks>
		protected override ServerProperties LoadServerProperties()
		{
			ServerProperties properties = new ServerProperties();

			properties.ManufacturerName = "Softing Industrial Automation GmbH";
			properties.ProductName = "Test Server";
            properties.ProductUri = "http://industrial.softing.com/OpcUaNetStandardToolkit/TestServer";
			properties.SoftwareVersion = Utils.GetAssemblySoftwareVersion();
			properties.BuildNumber = Utils.GetAssemblyBuildNumber();
			properties.BuildDate = Utils.GetAssemblyTimestamp();

			// TBD - All applications have software certificates that need to added to the properties.

			return properties;
		}

		/// <summary>
		/// Called after the server has been started.
		/// </summary>
		protected override void OnServerStarted(IServerInternal server)
		{
			base.OnServerStarted(server);

			// request notifications when the user identity is changed. all valid users are accepted by default.
			server.SessionManager.ImpersonateUser += new ImpersonateEventHandler(SessionManager_ImpersonateUser);
		}

		/// <summary>
		/// Called when a client tries to change its user identity.
		/// </summary>
		private void SessionManager_ImpersonateUser(Session session, ImpersonateEventArgs args)
		{
			// check for a user name token.
			UserNameIdentityToken userNameToken = args.NewIdentity as UserNameIdentityToken;

			if (userNameToken != null)
			{
				VerifyPassword(userNameToken.UserName, userNameToken.DecryptedPassword);
				args.Identity = new UserIdentity(userNameToken);
				Utils.Trace("UserName Token Accepted: {0}", args.Identity.DisplayName);
			}
		}

		/// <summary>
		/// Validates the password for a username token.
		/// </summary>
		private void VerifyPassword(string userName, string password)
		{
			if (userName != "usr" || password != "pwd")
			{
				ServiceResult error = new ServiceResult(StatusCodes.BadUserAccessDenied);

				// throw the exception.
				throw new ServiceResultException(error);
			}
		}

		#endregion
	}
}