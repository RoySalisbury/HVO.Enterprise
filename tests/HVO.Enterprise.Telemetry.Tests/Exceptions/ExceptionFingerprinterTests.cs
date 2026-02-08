using System;
using HVO.Enterprise.Telemetry.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Enterprise.Telemetry.Tests.Exceptions
{
    [TestClass]
    public class ExceptionFingerprinterTests
    {
        [TestMethod]
        public void ExceptionFingerprinter_GeneratesConsistentFingerprints()
        {
            var ex1 = new InvalidOperationException("Failed to process user 12345");
            var ex2 = new InvalidOperationException("Failed to process user 67890");

            var fp1 = ExceptionFingerprinter.GenerateFingerprint(ex1);
            var fp2 = ExceptionFingerprinter.GenerateFingerprint(ex2);

            Assert.AreEqual(fp1, fp2);
        }

        [TestMethod]
        public void ExceptionFingerprinter_HandlesDifferentExceptionTypes()
        {
            var ex1 = new InvalidOperationException("Error");
            var ex2 = new ArgumentException("Error");

            var fp1 = ExceptionFingerprinter.GenerateFingerprint(ex1);
            var fp2 = ExceptionFingerprinter.GenerateFingerprint(ex2);

            Assert.AreNotEqual(fp1, fp2);
        }

        [TestMethod]
        public void ExceptionFingerprinter_HandlesInnerExceptions()
        {
            var inner = new InvalidOperationException("Inner error");
            var outer = new Exception("Outer error", inner);

            var fingerprint = ExceptionFingerprinter.GenerateFingerprint(outer);

            Assert.IsFalse(string.IsNullOrEmpty(fingerprint));
        }

        [TestMethod]
        public void ExceptionFingerprinter_HandlesAggregateException()
        {
            var ex1 = new InvalidOperationException("Error 1");
            var ex2 = new ArgumentException("Error 2");
            var aggEx = new AggregateException(ex1, ex2);

            var fingerprint = ExceptionFingerprinter.GenerateFingerprint(aggEx);

            Assert.IsFalse(string.IsNullOrEmpty(fingerprint));
        }
    }
}
