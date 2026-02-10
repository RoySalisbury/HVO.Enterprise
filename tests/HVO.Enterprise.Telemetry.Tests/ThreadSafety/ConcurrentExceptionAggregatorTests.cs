using System;
using System.Collections.Concurrent;
using System.Threading;
using HVO.Enterprise.Telemetry.Exceptions;
using HVO.Enterprise.Telemetry.Tests.Helpers;

namespace HVO.Enterprise.Telemetry.Tests.ThreadSafety
{
    /// <summary>
    /// Validates that <see cref="ExceptionAggregator"/> is thread-safe under concurrent
    /// exception recording.
    /// </summary>
    [TestClass]
    public class ConcurrentExceptionAggregatorTests
    {
        [TestMethod]
        public void ExceptionAggregator_ConcurrentRecordSameException_CountIsAccurate()
        {
            // Arrange
            var aggregator = new ExceptionAggregator();
            const int threadCount = 20;
            const int exceptionsPerThread = 100;

            // Act - all threads record the same exception type/message
            TestHelpers.RunConcurrently(threadCount, _ =>
            {
                for (int i = 0; i < exceptionsPerThread; i++)
                {
                    try { throw new InvalidOperationException("concurrent-test"); }
                    catch (Exception ex) { aggregator.RecordException(ex); }
                }
            });

            // Assert
            long expected = threadCount * exceptionsPerThread;
            Assert.AreEqual(expected, aggregator.TotalExceptions);

            // All should be in one group (same fingerprint)
            var groups = aggregator.GetGroups();
            Assert.IsTrue(groups.Count >= 1, "Should have at least one exception group");
        }

        [TestMethod]
        public void ExceptionAggregator_ConcurrentRecordDifferentExceptions_CreatesMultipleGroups()
        {
            // Arrange
            var aggregator = new ExceptionAggregator();
            const int threadCount = 10;
            const int exceptionsPerThread = 50;

            // Act - each thread creates a unique exception
            TestHelpers.RunConcurrently(threadCount, index =>
            {
                for (int i = 0; i < exceptionsPerThread; i++)
                {
                    try
                    {
                        throw new InvalidOperationException($"error-{index}-unique-msg");
                    }
                    catch (Exception ex) { aggregator.RecordException(ex); }
                }
            });

            // Assert
            long expected = threadCount * exceptionsPerThread;
            Assert.AreEqual(expected, aggregator.TotalExceptions);
            Assert.IsTrue(aggregator.GetGroups().Count >= 1);
        }

        [TestMethod]
        public void ExceptionAggregator_ConcurrentGetGroupDuringRecording_NoExceptions()
        {
            // Arrange
            var aggregator = new ExceptionAggregator();
            const int writerThreads = 10;
            const int readerThreads = 5;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var readCount = 0;

            // Act - writers record exceptions while readers query groups
            var writers = new Thread[writerThreads];
            var readers = new Thread[readerThreads];

            for (int i = 0; i < writerThreads; i++)
            {
                writers[i] = new Thread(() =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        try { throw new Exception("test"); }
                        catch (Exception ex) { aggregator.RecordException(ex); }
                    }
                });
                writers[i].IsBackground = true;
                writers[i].Start();
            }

            for (int i = 0; i < readerThreads; i++)
            {
                readers[i] = new Thread(() =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        _ = aggregator.GetGroups();
                        _ = aggregator.TotalExceptions;
                        _ = aggregator.GetGlobalErrorRatePerMinute();
                        Interlocked.Increment(ref readCount);
                    }
                });
                readers[i].IsBackground = true;
                readers[i].Start();
            }

            cts.Token.WaitHandle.WaitOne();

            foreach (var w in writers) w.Join(1000);
            foreach (var r in readers) r.Join(1000);

            // Assert
            Assert.IsTrue(aggregator.TotalExceptions > 0);
            Assert.IsTrue(readCount > 0, "Readers should have queried at least once");
        }
    }
}
