using System;
using Masterloop.Core.Types.Commands;
using Masterloop.Core.Types.LiveConnect;
using Xunit;

namespace Masterloop.Plugin.Application.Tests
{
    public class Live : ApplicationBase
    {
        [Fact]
        public void ConnectDisconnect()
        {
            MasterloopLiveConnection live = GetMCSLiveTemporary();
            Assert.NotNull(live);
            Assert.False(live.IsConnected());
            Assert.True(live.Connect());
            Assert.True(live.IsConnected());
            live.Disconnect();
            Assert.False(live.IsConnected());
        }

        [Fact]
        public void SendCommand()
        {
            MasterloopLiveConnection live = GetMCSLiveTemporary();
            Assert.NotNull(live);
            Assert.False(live.IsConnected());
            Assert.True(live.Connect());
            Assert.True(live.IsConnected());
            Command cmd = new Command()
            {
                Id = Templates.MLTEST.Commands.Simple,
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                Arguments = null
            };
            Assert.True(live.SendCommand(GetMID(), cmd));
            live.Disconnect();
            Assert.False(live.IsConnected());
        }

        [Fact]
        public void SendCommandWithMetadata()
        {
            MasterloopLiveConnection live = GetMCSLiveTemporary();
            live.Metadata = new ApplicationMetadata()
            {
                Application = "Tests.LiveConnection",
                Reference = "SendCommandWithMetadata"
            };
            Assert.NotNull(live);
            Assert.False(live.IsConnected());
            Assert.True(live.Connect());
            Assert.True(live.IsConnected());
            Command cmd = new Command()
            {
                Id = 3,
                Timestamp = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                Arguments = null
            };
            Assert.True(live.SendCommand(GetMID(), cmd));
            live.Disconnect();
            Assert.False(live.IsConnected());
        }
    }
}
