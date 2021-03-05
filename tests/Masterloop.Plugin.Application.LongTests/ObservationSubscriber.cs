using System;
using System.Threading;
using Masterloop.Core.Types.Base;
using Masterloop.Core.Types.LiveConnect;
using Masterloop.Core.Types.Observations;

namespace Masterloop.Plugin.Application.LongTests
{
    public class ObservationSubscriber
    {
        IMasterloopLiveConnection _live;
        LiveAppRequest _lar; 

        public ObservationSubscriber(string hostname, string username, string password, bool useEncryption)
        {
            _live = new MasterloopLiveConnection(hostname, username, password, useEncryption);
            _live.UseAutomaticCallbacks = false;
            _lar = new LiveAppRequest()
            {
                TID = "",
                ConnectAllObservations = true
            };
        }

        public bool Init()
        {
            _live.RegisterObservationHandler(null, 2, OnObservationReceived, DataType.Boolean);
            _live.RegisterObservationHandler(null, 3, OnObservationReceived, DataType.Double);
            _live.RegisterObservationHandler(null, 4, OnObservationReceived, DataType.Integer);
            _live.RegisterObservationHandler(null, 5, OnObservationReceived, DataType.Position);
            _live.RegisterObservationHandler(null, 6, OnObservationReceived, DataType.String);
            _live.RegisterObservationHandler(null, 7, OnObservationReceived, DataType.Statistics);
            return _live.Connect(new LiveAppRequest[] { _lar });
        }

        public void Run()
        {
            while (true)
            {
                if (!_live.IsConnected())
                {
                    if (!_live.Connect())
                    {
                        Thread.Sleep(5000);
                    }
                }
                if (_live.QueueCount > 0)
                {
                    Console.WriteLine($"{_live.QueueCount} messages in queue.");
                }
                while (_live.QueueCount > 0)
                {
                    _live.Fetch();
                }
                Thread.Sleep(1);
            }
        }

        private void OnObservationReceived(string mid, int observationId, Observation o)
        {
            Console.WriteLine($"{mid} received obsId={observationId} with timestamp {o.Timestamp:O}");
        }
    }
}
