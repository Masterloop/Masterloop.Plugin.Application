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

namespace Masterloop.Plugin.Application
{
    public interface IMasterloopServerConnection : IDisposable
    {
        // Propertie
        string LastErrorMessage { get; set; }
        HttpStatusCode LastHttpStatusCode { get; set; }
        int Timeout { get; set; }

        // Templates
        DeviceTemplate[] GetTemplates();
        DeviceTemplate GetTemplate(string TID);
        Device[] GetTemplateDevices(string TID);
        bool CreateTemplate(int tenantId, DeviceTemplate template);
        bool UpdateTemplate(DeviceTemplate template);
        bool DeleteTemplate(string TID);

        // Devices
        Device[] GetDevices(bool includeMetadata);
        Device[] GetDevicesAsync(bool includeMetadata);
        DetailedDevice[] GetDevices(bool includeMetadata, bool includeDetails);
        DetailedDevice[] GetDevicesAsync(bool includeMetadata, bool includeDetails);
        DetailedDevice GetDeviceDetails(string MID);
        DetailedDevice GetDeviceDetailsAsync(string MID);
        SecureDetailedDevice GetSecureDeviceDetails(string MID);
        SecureDetailedDevice GetSecureDeviceDetailsAsync(string MID);
        DeviceTemplate GetDeviceTemplate(string MID);
        DeviceTemplate GetDeviceTemplateAsync(string MID);
        DetailedDevice CreateDevice(Device newDevice);
        DetailedDevice CreateDeviceAsync(Device newDevice);
        bool DeleteDevice(string MID);
        bool DeleteDeviceAsync(string MID);
        DateTime? GetLatestLoginTimestamp(string MID);
        DateTime? GetLatestLoginTimestampAsync(string MID);
        FirmwareReleaseDescriptor GetCurrentDeviceTemplateFirmwareDetails(string TID);
        FirmwareReleaseDescriptor GetCurrentDeviceTemplateFirmwareDetailsAsync(string TID);

        // Observations
        Observation GetCurrentObservation(string MID, int observationId, DataType dataType);
        Observation GetCurrentObservationAsync(string MID, int observationId, DataType dataType);
        IdentifiedObservation[] GetCurrentObservations(string MID);
        IdentifiedObservation[] GetCurrentObservationsAsync(string MID);
        Observation[] GetObservations(string MID, int observationId, DataType dataType, DateTime from, DateTime to);
        Observation[] GetObservationsAsync(string MID, int observationId, DataType dataType, DateTime from, DateTime to);
        int DeleteObservations(string MID, int observationId, DateTime from, DateTime to);
        int DeleteObservationsAsync(string MID, int observationId, DateTime from, DateTime to);

        // Commands
        bool SendDeviceCommand(string MID, Command command);
        bool SendDeviceCommandAsync(string MID, Command command);
        bool SendMultipleDeviceCommand(CommandsPackage[] commandPackages);
        bool SendMultipleDeviceCommandAsync(CommandsPackage[] commandPackages);
        Command[] GetDeviceCommandQueue(string MID);
        Command[] GetDeviceCommandQueueAsync(string MID);
        CommandHistory[] GetDeviceCommandHistory(string MID, DateTime from, DateTime to);
        CommandHistory[] GetDeviceCommandHistoryAsync(string MID, DateTime from, DateTime to);

        // Export
        ExportJob[] GetExportJobs();
        ExportJob[] GetExportJobsAsync();
        bool SendExportRequest(ExportRequest request);
        bool SendExportRequestAsync(ExportRequest request);

        // Settings
        bool SetSettings(string MID, SettingValue[] values);
        bool SetSettingsAsync(string MID, SettingValue[] values);
        bool SetMultipleSettings(SettingsPackage[] values);
        bool SetMultipleSettingsAsync(SettingsPackage[] values);
        ExpandedSettingsPackage GetSettings(string MID);
        ExpandedSettingsPackage GetSettingsAsync(string MID);

        // Live Connect
        LiveConnectionDetails RequestLiveConnection(LiveAppRequest[] liveRequests);
        LiveConnectionDetails RequestLiveConnectionAsync(LiveAppRequest[] liveRequests);
        LiveConnectionDetails RequestLiveConnection(PersistentLiveAppRequest persistentLiveRequest);
        LiveConnectionDetails RequestLiveConnectionAsync(PersistentLiveAppRequest persistentLiveRequest);
        bool DeleteTemporaryQueue(string temporaryKey);
        bool DeleteTemporaryQueueAsync(string temporaryKey);
        bool DeletePersistentQueue(string subscriptionKey);
        bool DeletePersistentQueueAsync(string subscriptionKey);

        // Pulse
        PulsePeriod[] GetPulsePeriod(string MID, int pulseId, DateTime from, DateTime to);
        PulsePeriod[] GetPulsePeriodAsync(string MID, int pulseId, DateTime from, DateTime to);
        PulsePeriod GetCurrentPulsePeriod(string MID, int pulseId);
        PulsePeriod GetCurrentPulsePeriodAsync(string MID, int pulseId);

        // Snapshot
        SnapshotItem[] GetCurrentSnapshot(SnapshotRequest snapshotRequest);
        SnapshotItem[] GetCurrentSnapshotAsync(SnapshotRequest snapshotRequest);

        // Connection
        bool CanPing();
        bool CanPingAsync();
    }
}
