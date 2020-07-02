using System;
using System.Linq;
using Masterloop.Core.Types.Commands;
using Masterloop.Core.Types.Devices;
using Xunit;

namespace Masterloop.Plugin.Application.Tests
{
    public class Rest : ApplicationBase
    {
        [Fact]
        public void GetAllDevices()
        {
            Device[] devices = GetMCSAPI().GetDevices(false);
            Assert.NotNull(devices);
            Assert.Contains(GetMID(), devices.Select(d => d.MID));
        }

        [Fact]
        public void GetTemplateDevices()
        {
            Device[] devices = GetMCSAPI().GetTemplateDevices(GetTID());
            Assert.NotNull(devices);
            Assert.Contains(GetMID(), devices.Select(d => d.MID));
        }

        [Fact]
        public void SendDeviceCommand()
        {
            Command cmd = new Command()
            {
                Id = Templates.MLTEST.Commands.Simple,
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                Arguments = null
            };
            Assert.True(GetMCSAPI().SendDeviceCommand(GetMID(), cmd));
        }

        [Fact]
        public void SendDeviceCommandWithMetadata()
        {
            Command cmd = new Command()
            {
                Id = Templates.MLTEST.Commands.Simple,
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                Arguments = null
            };
            MasterloopServerConnection mcs = GetMCSAPI();
            mcs.Metadata = new ApplicationMetadata()
            {
                Application = "Tests.ServerConnection",
                Reference = "SendDeviceCommandWithMetadata"
            };
            Assert.True(mcs.SendDeviceCommand(GetMID(), cmd));
        }
    }
}
