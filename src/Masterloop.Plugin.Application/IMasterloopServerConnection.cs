using Masterloop.Core.Types.Base;
using Masterloop.Core.Types.Commands;
using Masterloop.Core.Types.Devices;
using Masterloop.Core.Types.Firmware;
using Masterloop.Core.Types.ImportExport;
using Masterloop.Core.Types.LiveConnect;
using Masterloop.Core.Types.Observations;
using Masterloop.Core.Types.Pulse;
using Masterloop.Core.Types.Settings;
using System;
using System.Net;
using System.Threading.Tasks;

namespace Masterloop.Plugin.Application
{
    public interface IMasterloopServerConnection : IDisposable
    {
        // Configuration
        int Timeout { get; set; }
        bool UseCompression { get; set; }
        ApplicationMetadata Metadata { get; set; }

        // State
        string LastErrorMessage { get; set; }
        HttpStatusCode LastHttpStatusCode { get; set; }

        // Templates
        DeviceTemplate[] GetTemplates();
        DeviceTemplate GetTemplate(string TID);
        Device[] GetTemplateDevices(string TID);
        bool CreateTemplate(int tenantId, DeviceTemplate template);

        // Devices
        Device[] GetDevices(bool includeMetadata);
        Task<Device[]> GetDevicesAsync(bool includeMetadata);
        DetailedDevice[] GetDevices(bool includeMetadata, bool includeDetails);
        Task<DetailedDevice[]> GetDevicesAsync(bool includeMetadata, bool includeDetails);
        DetailedDevice GetDeviceDetails(string MID);
        Task<DetailedDevice> GetDeviceDetailsAsync(string MID);
        SecureDetailedDevice GetSecureDeviceDetails(string MID);
        Task<SecureDetailedDevice> GetSecureDeviceDetailsAsync(string MID);
        DeviceTemplate GetDeviceTemplate(string MID);
        Task<DeviceTemplate> GetDeviceTemplateAsync(string MID);
        DetailedDevice CreateDevice(Device newDevice);
        Task<DetailedDevice> CreateDeviceAsync(Device newDevice);
        bool DeleteDevice(string MID);
        Task<bool> DeleteDeviceAsync(string MID);
        DateTime? GetLatestLoginTimestamp(string MID);
        Task<DateTime?> GetLatestLoginTimestampAsync(string MID);
        FirmwareReleaseDescriptor GetCurrentDeviceTemplateFirmwareDetails(string TID);
        Task<FirmwareReleaseDescriptor> GetCurrentDeviceTemplateFirmwareDetailsAsync(string TID);

        // Observations
        Observation GetCurrentObservation(string MID, int observationId, DataType dataType);
        Task<Observation> GetCurrentObservationAsync(string MID, int observationId, DataType dataType);
        IdentifiedObservation[] GetCurrentObservations(string MID);
        Task<IdentifiedObservation[]> GetCurrentObservationsAsync(string MID);
        Observation[] GetObservations(string MID, int observationId, DataType dataType, DateTime from, DateTime to);
        Task<Observation[]> GetObservationsAsync(string MID, int observationId, DataType dataType, DateTime from, DateTime to);
        int DeleteObservations(string MID, int observationId, DateTime from, DateTime to);
        Task<int> DeleteObservationsAsync(string MID, int observationId, DateTime from, DateTime to);

        // Commands
        bool SendDeviceCommand(string MID, Command command);
        Task<bool> SendDeviceCommandAsync(string MID, Command command);
        bool SendMultipleDeviceCommand(CommandsPackage[] commandPackages);
        Task<bool> SendMultipleDeviceCommandAsync(CommandsPackage[] commandPackages);
        Command[] GetDeviceCommandQueue(string MID);
        Task<Command[]> GetDeviceCommandQueueAsync(string MID);
        CommandHistory[] GetDeviceCommandHistory(string MID, DateTime from, DateTime to);
        Task<CommandHistory[]> GetDeviceCommandHistoryAsync(string MID, DateTime from, DateTime to);

        // Settings
        bool SetSettings(string MID, SettingValue[] values);
        Task<bool> SetSettingsAsync(string MID, SettingValue[] values);
        bool SetMultipleSettings(SettingsPackage[] values);
        Task<bool> SetMultipleSettingsAsync(SettingsPackage[] values);
        ExpandedSettingsPackage GetSettings(string MID);
        Task<ExpandedSettingsPackage> GetSettingsAsync(string MID);

        // Live Temporary Connection
        LiveConnectionDetails RequestLiveConnection(LiveAppRequest[] liveAppRequests);
        Task<LiveConnectionDetails> RequestLiveConnectionAsync(LiveAppRequest[] liveAppRequests);
        bool DeleteLiveTemporaryConnction(string temporaryKey);
        Task<bool> DeleteLiveTemporaryConnctionAsync(string temporaryKey);

        // Live Persistent Subscription Connection
        bool CreateLivePersistentSubscription(LivePersistentSubscriptionRequest livePersistentSubscriptionRequest);
        Task<bool> CreateLivePersistentSubscriptionAsync(LivePersistentSubscriptionRequest livePersistentSubscriptionRequest);
        LiveConnectionDetails GetLivePersistentSubscriptionConnection(string subscriptionKey);
        Task<LiveConnectionDetails> GetLivePersistentSubscriptionConnectionAsync(string subscriptionKey);
        bool AddLivePersistentSubscriptionDevice(string subscriptionKey, string mid);
        Task<bool> AddLivePersistentSubscriptionDeviceAsync(string subscriptionKey, string mid);
        bool RemoveLivePersistentSubscriptionDevice(string subscriptionKey, string mid);
        Task<bool> RemoveLivePersistentSubscriptionDeviceAsync(string subscriptionKey, string mid);
        bool DeleteLivePersistentSubscription(string subscriptionKey);
        Task<bool> DeleteLivePersistentSubscriptionAsync(string subscriptionKey);

        // Pulse
        PulsePeriod[] GetPulsePeriod(string MID, int pulseId, DateTime from, DateTime to);
        Task<PulsePeriod[]> GetPulsePeriodAsync(string MID, int pulseId, DateTime from, DateTime to);
        PulsePeriod GetCurrentPulsePeriod(string MID, int pulseId);
        Task<PulsePeriod> GetCurrentPulsePeriodAsync(string MID, int pulseId);

        // Snapshot
        SnapshotItem[] GetCurrentSnapshot(SnapshotRequest snapshotRequest);
        Task<SnapshotItem[]> GetCurrentSnapshotAsync(SnapshotRequest snapshotRequest);

        // Connectivity
        bool CanPing();
        Task<bool> CanPingAsync();
    }
}