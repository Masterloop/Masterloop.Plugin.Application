using Masterloop.Core.Types.Base;
using Masterloop.Core.Types.Commands;
using Masterloop.Core.Types.LiveConnect;
using Masterloop.Core.Types.Observations;
using Masterloop.Core.Types.Pulse;
using System;
using System.Threading.Tasks;

namespace Masterloop.Plugin.Application
{
    public interface IMasterloopLiveConnection : IDisposable
    {
        bool IgnoreSslCertificateErrors { get; set; }
        ushort HeartbeatInterval { get; set; }
        int Timeout { get; set; }
        ApplicationMetadata Metadata { get; set; }
        string LastErrorMessage { get; set; }
        LiveConnectionDetails ConnectionDetails { get; }
        string ConnectionKey { get; }
        bool UseAutomaticCallbacks { get; set; }
        bool UseAutomaticAcknowledgement { get; set; }
        int QueueCount { get; }
        int PrefetchCount { get; set; }

        bool Connect();
        bool Connect(LiveAppRequest[] liveRequests);
        Task<bool> ConnectAsync(LiveAppRequest[] liveRequests);
        void Disconnect();
        bool IsConnected();
        bool PauseIncoming();
        bool ResumeIncoming();
        bool Fetch();

        bool SendCommand(string MID, Command command);
        bool SendPulse(DateTime? timestamp, int expiryMilliseconds);

        void RegisterObservationHandler(string MID, int observationId, Action<string, int, Observation> observationHandler, DataType dataType);
        void RegisterObservationHandler(string MID, int observationId, Action<string, int, BooleanObservation> observationHandler);
        void RegisterObservationHandler(string MID, int observationId, Action<string, int, DoubleObservation> observationHandler);
        void RegisterObservationHandler(string MID, int observationId, Action<string, int, IntegerObservation> observationHandler);
        void RegisterObservationHandler(string MID, int observationId, Action<string, int, PositionObservation> observationHandler);
        void RegisterObservationHandler(string MID, int observationId, Action<string, int, StringObservation> observationHandler);
        void UnregisterObservationHandler(string MID, int observationId);

        void RegisterCommandHandler(string MID, int commandId, Action<string, Command> commandHandler);
        void UnregisterCommandHandler(string MID, int commandId);

        void RegisterCommandResponseHandler(string MID, int commandId, Action<string, CommandResponse> commandResponseHandler);
        void UnregisterCommandResponseHandler(string MID, int commandId);

        void RegisterPulseHandler(string MID, Action<string, int, Pulse> pulseHandler);
        void UnregisterPulseHandler(string MID);
    }
}