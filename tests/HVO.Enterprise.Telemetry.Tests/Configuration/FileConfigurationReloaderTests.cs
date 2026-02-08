using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using HVO.Enterprise.Telemetry.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Configuration
{
    [TestClass]
    public class FileConfigurationReloaderTests
    {
        [TestMethod]
        public void FileConfigurationReloader_ReloadsOnFileChange()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(tempFile, JsonSerializer.Serialize(new TelemetryOptions()));

            var reloader = new FileConfigurationReloader(
                tempFile,
                logger: null,
                debounceDelay: TimeSpan.FromMilliseconds(50),
                maxReadRetries: 3,
                retryDelay: TimeSpan.FromMilliseconds(10));

            var changedEvent = new ManualResetEventSlim(false);
            reloader.ConfigurationChanged += (_, __) => changedEvent.Set();

            var updatedOptions = new TelemetryOptions { DefaultSamplingRate = 0.5 };
            File.WriteAllText(tempFile, JsonSerializer.Serialize(updatedOptions));

            Assert.IsTrue(changedEvent.Wait(TimeSpan.FromSeconds(2)));
            Assert.AreEqual(0.5, reloader.CurrentOptions.DefaultSamplingRate, 0.0001);

            reloader.Dispose();
            File.Delete(tempFile);
        }

        [TestMethod]
        public void FileConfigurationReloader_InvalidConfig_DoesNotReplace()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(tempFile, JsonSerializer.Serialize(new TelemetryOptions { DefaultSamplingRate = 0.5 }));

            var reloader = new FileConfigurationReloader(
                tempFile,
                logger: null,
                debounceDelay: TimeSpan.FromMilliseconds(50),
                maxReadRetries: 3,
                retryDelay: TimeSpan.FromMilliseconds(10));

            var invalidOptions = new TelemetryOptions { DefaultSamplingRate = 2.0 };
            File.WriteAllText(tempFile, JsonSerializer.Serialize(invalidOptions));

            Thread.Sleep(200);

            Assert.AreEqual(0.5, reloader.CurrentOptions.DefaultSamplingRate, 0.0001);

            reloader.Dispose();
            File.Delete(tempFile);
        }
    }
}
