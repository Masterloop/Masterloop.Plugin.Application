using System;
using Masterloop.Core.Types.LiveConnect;
using Microsoft.Extensions.Configuration;

namespace Masterloop.Plugin.Application.Tests
{
    public abstract class ApplicationBase
    {
        protected static IConfiguration _config;

        protected static IConfiguration GetConfig()
        {
            if (_config == null)
            {
                _config = new ConfigurationBuilder().AddJsonFile("appsettings.test.json").Build();
            }
            return _config;
        }

        protected static IMasterloopServerConnection GetMCSAPI()
        {
            IConfiguration config = GetConfig();
            return new MasterloopServerConnection(config["Hostname"], config["Username"], config["Password"], Boolean.Parse(config["UseHTTPS"]));
        }

        protected static MasterloopLiveConnection GetMCSLiveTemporary()
        {
            var mcs = GetMCSAPI();
            LiveAppRequest lar = new LiveAppRequest()
            {
                MID = GetMID(),
                ConnectAllCommands = true,
                ConnectAllObservations = true,
                InitObservationValues = false,
                ReceiveDevicePulse = true
            };
            LiveConnectionDetails lcd = mcs.RequestLiveConnection(new LiveAppRequest[] { lar });
            return new MasterloopLiveConnection(lcd);
        }

        protected static MasterloopLiveConnection GetMCSPersistentConnection()
        {
            var mcs = GetMCSAPI();
            string subscriptionKey = GetPersistentSubscriptionKey();
            mcs.DeleteLivePersistentSubscription(subscriptionKey);
            LivePersistentSubscriptionRequest request = new LivePersistentSubscriptionRequest()
            {
                SubscriptionKey = subscriptionKey,
                TID = GetTID(),
                ConnectAllCommands = true
            };
            if (mcs.CreateLivePersistentSubscription(request))
            {
                LiveConnectionDetails lcd = mcs.GetLivePersistentSubscriptionConnection(subscriptionKey);
                return new MasterloopLiveConnection(lcd);
            }
            else
            {
                throw new Exception("Could not create subscription");
            }
        }

        protected static string GetMID()
        {
            IConfiguration config = GetConfig();
            return config["MID"];
        }

        protected static string GetTID()
        {
            IConfiguration config = GetConfig();
            return config["TID"];
        }

        protected static string GetPersistentSubscriptionKey()
        {
            IConfiguration config = GetConfig();
            return config["PersistentSubscriptionKey"];
        }
    }
}