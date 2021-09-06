using System;
using System.Threading;
using Masterloop.Core.Types.Base;
using Masterloop.Core.Types.Commands;
using Masterloop.Core.Types.LiveConnect;
using Masterloop.Core.Types.Observations;

namespace Masterloop.Plugin.Application.LongTests
{
    public class ObservationSubscriber
    {
        IMasterloopLiveConnection _live;
        LiveAppRequest _lar;
        DateTime _nextGenerate = DateTime.UtcNow.AddDays(1);
        DateTime _nextPoll = DateTime.UtcNow;
        DateTime _nextStats = DateTime.UtcNow;
        int _counter = 0;
        object _semaphore = new object();
        string _mid;

        public ObservationSubscriber(string hostname, string username, string password, bool useEncryption, string mid, string tid)
        {
            _live = new MasterloopLiveConnection(hostname, username, password, useEncryption);
            _live.UseAutomaticCallbacks = false;
            _live.UseAutomaticAcknowledgement = false;
            _live.PrefetchCount = 100;
            _lar = new LiveAppRequest()
            {
                TID = tid,
                ConnectAllObservations = true,
                ConnectAllCommands = true
            };
            _mid = mid;
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
                try
                {
                    if (!_live.IsConnected())
                    {
                        Console.WriteLine($"{DateTime.UtcNow:o} - Not connected, reconnecting.");
                        if (!_live.Connect(new LiveAppRequest[] { _lar }))
                        {
                            Console.WriteLine($"{DateTime.UtcNow:o} - Re-connection failed.");
                            Thread.Sleep(5000);
                        }
                    }
                    if (DateTime.UtcNow > _nextGenerate)
                    {
                        _live.SendCommand(_mid, new Command() { Id = 6, Timestamp = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddMinutes(2), Arguments = new CommandArgument[] { new CommandArgument() { Id = 1, Value = "1000" } } });
                        //_nextGenerate = DateTime.UtcNow.AddSeconds(30);
                    }

                    if (DateTime.UtcNow > _nextPoll)
                    {
                        _live.SendCommand(_mid, new Command() { Id = 6, Timestamp = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddMinutes(2), Arguments = new CommandArgument[] { new CommandArgument() { Id = 1, Value = "100" } } });
                        _nextPoll = DateTime.UtcNow.AddMilliseconds(5000);
                    }

                    /*if (_live.QueueCount > 0)
                    {
                        Console.WriteLine($"{DateTime.UtcNow:o} - {_live.QueueCount} messages in queue.");
                    }*/
                    while (_live.Fetch())
                    {
                    }
                    Thread.Sleep(1);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{DateTime.UtcNow:o} - {e.Message} - {e.StackTrace}");
                }
            }
        }

        private void OnObservationReceived(string mid, int observationId, Observation o)
        {
            lock (_semaphore)
            {
                _counter++;
                if (DateTime.UtcNow > _nextStats)
                {
                    Console.WriteLine($"{DateTime.UtcNow:o} - Message rate {_counter} msgs/s");
                    _counter = 0;
                    _nextStats = DateTime.UtcNow.AddSeconds(1);
                }
                //Console.WriteLine($"{mid} received obsId={observationId} with timestamp {o.Timestamp:O}");
            }
        }
    }
}
