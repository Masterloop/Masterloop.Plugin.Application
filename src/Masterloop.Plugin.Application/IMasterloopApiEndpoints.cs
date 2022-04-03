using System;
using System.Threading.Tasks;
using Masterloop.Core.Types.Base;
using Masterloop.Core.Types.Commands;
using Masterloop.Core.Types.Devices;
using Masterloop.Core.Types.Firmware;
using Masterloop.Core.Types.ImportExport;
using Masterloop.Core.Types.LiveConnect;
using Masterloop.Core.Types.Observations;
using Masterloop.Core.Types.Pulse;
using Masterloop.Core.Types.Settings;

namespace Masterloop.Plugin.Application
{
    /// <summary>
    ///     Exposed Masterloop API Endpoints
    /// </summary>
    public interface IMasterloopApiEndpoints
    {
        // Templates
        Task<DeviceTemplate[]> GetTemplatesAsync();
        Task<DeviceTemplate> GetTemplateAsync(string TID);
        Task<Device[]> GetTemplateDevicesAsync(string TID);
        Task<bool> CreateTemplateAsync(int tenantId, DeviceTemplate template);

        // Devices
        Devicelet[] GetDevicelets(bool includeDetails = false);
        Task<Device[]> GetDevicesAsync(bool includeMetadata = false);
        Task<DetailedDevice[]> GetDevicesAsync(bool includeMetadata = false, bool includeDetails = false);
        Task<DetailedDevice> GetDeviceDetailsAsync(string MID);
        Task<SecureDetailedDevice> GetSecureDeviceDetailsAsync(string MID);
        Task<DeviceTemplate> GetDeviceTemplateAsync(string MID);
        Task<DetailedDevice> CreateDeviceAsync(NewDevice newDevice);
        Task<bool> DeleteDeviceAsync(string MID);
        Task<DateTime?> GetLatestLoginTimestampAsync(string MID);
        Task<FirmwareReleaseDescriptor> GetCurrentDeviceTemplateFirmwareDetailsAsync(string TID);
        Task<FirmwareVariant[]> GetDeviceTemplateFirmwareVariantsAsync(string TID);
        Task<FirmwareReleaseDescriptor> GetCurrentDeviceTemplateVariantFirmwareDetailsAsync(string TID, int firmwareVariantId);

        // Observations
        Task<Observation> GetCurrentObservationAsync(string MID, int observationId, DataType dataType);
        Task<IdentifiedObservation[]> GetCurrentObservationsAsync(string MID);
        Task<Observation[]> GetObservationsAsync(string MID, int observationId, DataType dataType, DateTime from, DateTime to);
        Task<int> DeleteObservationsAsync(string MID, int observationId, DateTime from, DateTime to);

        // Commands
        Task<bool> SendDeviceCommandAsync(string MID, Command command);
        Task<bool> SendMultipleDeviceCommandAsync(CommandsPackage[] commandPackages);
        Task<Command[]> GetDeviceCommandQueueAsync(string MID);
        Task<CommandHistory[]> GetDeviceCommandHistoryAsync(string MID, DateTime from, DateTime to);

        // Settings
        Task<bool> SetSettingsAsync(string MID, SettingValue[] values);
        Task<bool> SetMultipleSettingsAsync(SettingsPackage[] values);
        Task<ExpandedSettingsPackage> GetSettingsAsync(string MID);

        // Live Temporary Connection
        Task<LiveConnectionDetails> RequestLiveConnectionAsync(LiveAppRequest[] liveAppRequests);
        Task<bool> DeleteLiveTemporaryConnectionAsync(string temporaryKey);

        // Live Persistent Subscription Connection
        Task<bool> CreateLivePersistentSubscriptionAsync(LivePersistentSubscriptionRequest livePersistentSubscriptionRequest);
        Task<LiveConnectionDetails> GetLivePersistentSubscriptionConnectionAsync(string subscriptionKey);
        Task<bool> AddLivePersistentSubscriptionDeviceAsync(string subscriptionKey, string mid);
        Task<bool> RemoveLivePersistentSubscriptionDeviceAsync(string subscriptionKey, string mid);
        Task<bool> DeleteLivePersistentSubscriptionAsync(string subscriptionKey);
        Task<string[]> GetPersistentSubscriptionWhitelistAsync(string subscriptionKey);
        Task<bool> CreatePersistentSubscriptionWhitelistAsync(string subscriptionKey);

        // Pulse
        Task<PulsePeriod[]> GetPulsePeriodAsync(string MID, int pulseId, DateTime from, DateTime to);
        Task<PulsePeriod> GetCurrentPulsePeriodAsync(string MID, int pulseId);

        // Snapshot
        Task<SnapshotItem[]> GetCurrentSnapshotAsync(SnapshotRequest snapshotRequest);

        // Connectivity
        Task<bool> CanPingAsync();
    }
}