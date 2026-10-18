using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using ServerAutomatedStorage.Infrastructure.Authentication;
using ServerAutomatedStorage.WebAPI.Contracts;
using ServerAutomatedStorage.WebAPI.Services;
using System;
using System.Collections.Generic;
using System.Text;
using static ServerAutomatedStorage.WebAPI.Contracts.DeviceMonitorResponses;

namespace ServerAutomatedStorage.UnitTests.WebAPI
{
    public class DeviceMonitorTests
    {
        private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero));
        private readonly DeviceMonitor _monitor;

        public DeviceMonitorTests()
        {
            var options = Substitute.For<IOptionsMonitor<DeviceApiKeyOptions>>();
            options.Get(DeviceApiKeyDefaults.Scheme).Returns(new DeviceApiKeyOptions
            {
                Devices = [new DeviceSettings { Name = "cabinet-1", ApiKey = "key" }]
            });
            _monitor = new DeviceMonitor(options, _time);
        }

        private static HeartbeatRequest Heartbeat() => new(WeightGrams: 120, RfidOk: true, ScaleOk: true);

        [Fact]
        public void ConfiguredDevice_WithoutHeartbeat_IsOffline()
        {
            var status = Assert.Single(_monitor.GetStatuses());

            Assert.Equal("cabinet-1", status.Name);
            Assert.False(status.IsOnline);
            Assert.Null(status.LastSeenUtc);
        }

        [Fact]
        public void AfterHeartbeat_DeviceIsOnline()
        {
            _monitor.RecordHeartbeat("cabinet-1", Heartbeat(), "192.168.0.50");

            var status = _monitor.GetStatus("cabinet-1");

            Assert.True(status.IsOnline);
            Assert.Equal(120, status.WeightGrams);
            Assert.Equal("192.168.0.50", status.RemoteAddress);
        }

        [Fact]
        public void After30SecondsOfSilence_DeviceIsOffline()
        {
            _monitor.RecordHeartbeat("cabinet-1", Heartbeat(), null);

            _time.Advance(TimeSpan.FromSeconds(31)); 

            Assert.False(_monitor.GetStatus("cabinet-1").IsOnline);
        }

        [Fact]
        public void Events_NewestFirst_AndLimitedTo100()
        {
            for (var i = 0; i < 105; i++)
            {
                _monitor.RecordEvent("cabinet-1", DeviceEventType.CardScan, $"CARD{i}", null, true, "ok");
                _time.Advance(TimeSpan.FromSeconds(1));
            }

            var events = _monitor.GetEvents(sinceUtc: null, type: null, take: 1000);

            Assert.Equal(100, events.Count);
            Assert.Equal("CARD104", events[0].CardUid);
        }
    }
}
