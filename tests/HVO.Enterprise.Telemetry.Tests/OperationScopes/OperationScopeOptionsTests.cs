using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using HVO.Enterprise.Telemetry;
using HVO.Enterprise.Telemetry.Context;
using Microsoft.Extensions.Logging;

namespace HVO.Enterprise.Telemetry.Tests.OperationScopes
{
    /// <summary>
    /// Tests for <see cref="OperationScopeOptions"/> defaults and <c>CreateChildOptions</c>.
    /// </summary>
    [TestClass]
    public class OperationScopeOptionsTests
    {
        [TestMethod]
        public void Defaults_CreateActivity_IsTrue()
        {
            var options = new OperationScopeOptions();
            Assert.IsTrue(options.CreateActivity);
        }

        [TestMethod]
        public void Defaults_ActivityKind_IsInternal()
        {
            var options = new OperationScopeOptions();
            Assert.AreEqual(ActivityKind.Internal, options.ActivityKind);
        }

        [TestMethod]
        public void Defaults_LogEvents_IsTrue()
        {
            var options = new OperationScopeOptions();
            Assert.IsTrue(options.LogEvents);
        }

        [TestMethod]
        public void Defaults_LogLevel_IsInformation()
        {
            var options = new OperationScopeOptions();
            Assert.AreEqual(LogLevel.Information, options.LogLevel);
        }

        [TestMethod]
        public void Defaults_RecordMetrics_IsTrue()
        {
            var options = new OperationScopeOptions();
            Assert.IsTrue(options.RecordMetrics);
        }

        [TestMethod]
        public void Defaults_EnrichContext_IsTrue()
        {
            var options = new OperationScopeOptions();
            Assert.IsTrue(options.EnrichContext);
        }

        [TestMethod]
        public void Defaults_CaptureExceptions_IsTrue()
        {
            var options = new OperationScopeOptions();
            Assert.IsTrue(options.CaptureExceptions);
        }

        [TestMethod]
        public void Defaults_InitialTags_IsNull()
        {
            var options = new OperationScopeOptions();
            Assert.IsNull(options.InitialTags);
        }

        [TestMethod]
        public void Defaults_PiiOptions_IsNotNull()
        {
            var options = new OperationScopeOptions();
            Assert.IsNotNull(options.PiiOptions);
        }

        [TestMethod]
        public void Defaults_SerializeComplexTypes_IsTrue()
        {
            var options = new OperationScopeOptions();
            Assert.IsTrue(options.SerializeComplexTypes);
        }

        [TestMethod]
        public void Defaults_ComplexTypeSerializer_IsNull()
        {
            var options = new OperationScopeOptions();
            Assert.IsNull(options.ComplexTypeSerializer);
        }

        [TestMethod]
        public void Defaults_JsonSerializerOptions_IsNull()
        {
            var options = new OperationScopeOptions();
            Assert.IsNull(options.JsonSerializerOptions);
        }

        [TestMethod]
        public void AllProperties_CanBeSet()
        {
            var jsonOptions = new JsonSerializerOptions();
            Func<object, string> serializer = obj => obj.ToString()!;

            var options = new OperationScopeOptions
            {
                CreateActivity = false,
                ActivityKind = ActivityKind.Client,
                LogEvents = false,
                LogLevel = LogLevel.Debug,
                RecordMetrics = false,
                EnrichContext = false,
                CaptureExceptions = false,
                InitialTags = new Dictionary<string, object?> { ["key"] = "value" },
                PiiOptions = new EnrichmentOptions { RedactPii = false },
                SerializeComplexTypes = false,
                ComplexTypeSerializer = serializer,
                JsonSerializerOptions = jsonOptions
            };

            Assert.IsFalse(options.CreateActivity);
            Assert.AreEqual(ActivityKind.Client, options.ActivityKind);
            Assert.IsFalse(options.LogEvents);
            Assert.AreEqual(LogLevel.Debug, options.LogLevel);
            Assert.IsFalse(options.RecordMetrics);
            Assert.IsFalse(options.EnrichContext);
            Assert.IsFalse(options.CaptureExceptions);
            Assert.AreEqual(1, options.InitialTags!.Count);
            Assert.IsFalse(options.PiiOptions!.RedactPii);
            Assert.IsFalse(options.SerializeComplexTypes);
            Assert.AreSame(serializer, options.ComplexTypeSerializer);
            Assert.AreSame(jsonOptions, options.JsonSerializerOptions);
        }

        // --- CreateChildOptions ---

        [TestMethod]
        public void CreateChildOptions_CopiesParentValues()
        {
            var parent = new OperationScopeOptions
            {
                CreateActivity = true,
                ActivityKind = ActivityKind.Server,
                LogEvents = false,
                LogLevel = LogLevel.Warning,
                RecordMetrics = false,
                CaptureExceptions = false,
                SerializeComplexTypes = false
            };

            // CreateChildOptions is internal, access via reflection
            var method = typeof(OperationScopeOptions).GetMethod("CreateChildOptions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(method, "CreateChildOptions method should exist");

            var child = (OperationScopeOptions)method!.Invoke(parent, null)!;

            Assert.AreEqual(parent.CreateActivity, child.CreateActivity);
            Assert.AreEqual(parent.ActivityKind, child.ActivityKind);
            Assert.AreEqual(parent.LogEvents, child.LogEvents);
            Assert.AreEqual(parent.LogLevel, child.LogLevel);
            Assert.AreEqual(parent.RecordMetrics, child.RecordMetrics);
            Assert.AreEqual(parent.CaptureExceptions, child.CaptureExceptions);
            Assert.AreEqual(parent.SerializeComplexTypes, child.SerializeComplexTypes);
        }

        [TestMethod]
        public void CreateChildOptions_EnrichContext_AlwaysFalse()
        {
            var parent = new OperationScopeOptions { EnrichContext = true };

            var method = typeof(OperationScopeOptions).GetMethod("CreateChildOptions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var child = (OperationScopeOptions)method!.Invoke(parent, null)!;

            Assert.IsFalse(child.EnrichContext,
                "Child options should always have EnrichContext=false to avoid double enrichment");
        }

        [TestMethod]
        public void CreateChildOptions_PreservesSerializerReferences()
        {
            Func<object, string> serializer = obj => obj.ToString()!;
            var jsonOptions = new JsonSerializerOptions();

            var parent = new OperationScopeOptions
            {
                ComplexTypeSerializer = serializer,
                JsonSerializerOptions = jsonOptions
            };

            var method = typeof(OperationScopeOptions).GetMethod("CreateChildOptions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var child = (OperationScopeOptions)method!.Invoke(parent, null)!;

            Assert.AreSame(serializer, child.ComplexTypeSerializer);
            Assert.AreSame(jsonOptions, child.JsonSerializerOptions);
        }
    }
}
