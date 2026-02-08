using System.Collections.Generic;
using HVO.Enterprise.Telemetry.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Logging
{
    [TestClass]
    public sealed class TelemetryLoggerOptionsTests
    {
        [TestMethod]
        public void Defaults_EnrichmentEnabled()
        {
            // Arrange & Act
            var options = new TelemetryLoggerOptions();

            // Assert
            Assert.IsTrue(options.EnableEnrichment);
        }

        [TestMethod]
        public void Defaults_TraceIdSpanIdParentSpanIdCorrelationIdEnabled()
        {
            // Arrange & Act
            var options = new TelemetryLoggerOptions();

            // Assert
            Assert.IsTrue(options.IncludeTraceId);
            Assert.IsTrue(options.IncludeSpanId);
            Assert.IsTrue(options.IncludeParentSpanId);
            Assert.IsTrue(options.IncludeCorrelationId);
        }

        [TestMethod]
        public void Defaults_TraceFlagsAndTraceStateDisabled()
        {
            // Arrange & Act
            var options = new TelemetryLoggerOptions();

            // Assert
            Assert.IsFalse(options.IncludeTraceFlags);
            Assert.IsFalse(options.IncludeTraceState);
        }

        [TestMethod]
        public void Defaults_FieldNamesArePascalCase()
        {
            // Arrange & Act
            var options = new TelemetryLoggerOptions();

            // Assert
            Assert.AreEqual("TraceId", options.TraceIdFieldName);
            Assert.AreEqual("SpanId", options.SpanIdFieldName);
            Assert.AreEqual("ParentSpanId", options.ParentSpanIdFieldName);
            Assert.AreEqual("TraceFlags", options.TraceFlagsFieldName);
            Assert.AreEqual("TraceState", options.TraceStateFieldName);
            Assert.AreEqual("CorrelationId", options.CorrelationIdFieldName);
        }

        [TestMethod]
        public void Defaults_CustomEnrichersIsNull()
        {
            // Arrange & Act
            var options = new TelemetryLoggerOptions();

            // Assert
            Assert.IsNull(options.CustomEnrichers);
        }

        [TestMethod]
        public void CustomFieldNames_CanBeOverridden()
        {
            // Arrange & Act
            var options = new TelemetryLoggerOptions
            {
                TraceIdFieldName = "dd.trace_id",
                SpanIdFieldName = "dd.span_id",
                ParentSpanIdFieldName = "dd.parent_id",
                TraceFlagsFieldName = "dd.trace_flags",
                TraceStateFieldName = "dd.trace_state",
                CorrelationIdFieldName = "x-correlation-id"
            };

            // Assert
            Assert.AreEqual("dd.trace_id", options.TraceIdFieldName);
            Assert.AreEqual("dd.span_id", options.SpanIdFieldName);
            Assert.AreEqual("dd.parent_id", options.ParentSpanIdFieldName);
            Assert.AreEqual("dd.trace_flags", options.TraceFlagsFieldName);
            Assert.AreEqual("dd.trace_state", options.TraceStateFieldName);
            Assert.AreEqual("x-correlation-id", options.CorrelationIdFieldName);
        }

        [TestMethod]
        public void CustomEnrichers_CanBeSet()
        {
            // Arrange & Act
            var enricher = new FakeEnricher(props => props["test"] = "value");
            var options = new TelemetryLoggerOptions
            {
                CustomEnrichers = new List<ILogEnricher> { enricher }
            };

            // Assert
            Assert.IsNotNull(options.CustomEnrichers);
            Assert.AreEqual(1, options.CustomEnrichers.Count);
        }

        [TestMethod]
        public void AllFieldsCanBeDisabled()
        {
            // Arrange & Act
            var options = new TelemetryLoggerOptions
            {
                EnableEnrichment = false,
                IncludeTraceId = false,
                IncludeSpanId = false,
                IncludeParentSpanId = false,
                IncludeTraceFlags = false,
                IncludeTraceState = false,
                IncludeCorrelationId = false
            };

            // Assert
            Assert.IsFalse(options.EnableEnrichment);
            Assert.IsFalse(options.IncludeTraceId);
            Assert.IsFalse(options.IncludeSpanId);
            Assert.IsFalse(options.IncludeParentSpanId);
            Assert.IsFalse(options.IncludeTraceFlags);
            Assert.IsFalse(options.IncludeTraceState);
            Assert.IsFalse(options.IncludeCorrelationId);
        }
    }
}
