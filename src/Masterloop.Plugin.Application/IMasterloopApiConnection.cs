namespace Masterloop.Plugin.Application
{
    public interface IMasterloopApiConnection : IMasterloopApiEndpoints
    {
        MasterloopApiOptions MasterloopApiOptions { get; }
        ApplicationMetadata ApplicationMetadata { get; }
        int Timeout { get; set; }
    }
}