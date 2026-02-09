using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Proxies;

namespace HVO.Enterprise.Telemetry.Tests.Proxies
{
    // ─── Simple instrumented interface (method-level) ───────────────────

    public interface ISimpleService
    {
        [InstrumentMethod]
        string GetValue(int id);

        [InstrumentMethod(CaptureReturnValue = true, OperationName = "Custom.Get")]
        Task<string> GetValueAsync(int id);

        [InstrumentMethod(CaptureParameters = false)]
        void DoWork(string data);

        // Not instrumented - no attribute
        string NotInstrumented(int id);
    }

    public class SimpleService : ISimpleService
    {
        public string GetValue(int id) => $"value-{id}";

        public Task<string> GetValueAsync(int id)
            => Task.FromResult($"async-value-{id}");

        public void DoWork(string data) { /* no-op */ }

        public string NotInstrumented(int id) => $"plain-{id}";
    }

    // ─── Class-level instrumented interface ──────────────────────────────

    [InstrumentClass(OperationPrefix = "OrderSvc")]
    public interface IOrderService
    {
        Task<string> GetOrderAsync(int orderId);

        void ProcessOrder(string name);

        [NoTelemetry]
        bool HealthCheck();
    }

    public class OrderService : IOrderService
    {
        public Task<string> GetOrderAsync(int orderId)
            => Task.FromResult($"order-{orderId}");

        public void ProcessOrder(string name) { /* no-op */ }

        public bool HealthCheck() => true;
    }

    // ─── Class-level with method override ────────────────────────────────

    [InstrumentClass]
    public interface IOverrideService
    {
        [InstrumentMethod(ActivityKind = ActivityKind.Client, CaptureReturnValue = true)]
        string GetData(int id);

        void DefaultMethod();
    }

    public class OverrideService : IOverrideService
    {
        public string GetData(int id) => $"data-{id}";
        public void DefaultMethod() { /* no-op */ }
    }

    // ─── Sensitive data interface ────────────────────────────────────────

    [InstrumentClass]
    public interface ISensitiveService
    {
        void ProcessPayment(
            int orderId,
            [SensitiveData] string creditCard,
            [SensitiveData(Strategy = RedactionStrategy.Hash)] string email,
            [SensitiveData(Strategy = RedactionStrategy.Remove)] string ssn);
    }

    public class SensitiveService : ISensitiveService
    {
        public void ProcessPayment(int orderId, string creditCard, string email, string ssn)
        { /* no-op */ }
    }

    // ─── Auto PII detection interface ────────────────────────────────────

    [InstrumentClass]
    public interface IPiiAutoDetectService
    {
        void Login(string username, string password, string token);
    }

    public class PiiAutoDetectService : IPiiAutoDetectService
    {
        public void Login(string username, string password, string token)
        { /* no-op */ }
    }

    // ─── Async exception interface ───────────────────────────────────────

    [InstrumentClass]
    public interface IExceptionService
    {
        [InstrumentMethod]
        void ThrowSync();

        [InstrumentMethod]
        Task ThrowAsync();

        [InstrumentMethod]
        Task<int> ThrowAsyncWithResult();
    }

    public class ExceptionService : IExceptionService
    {
        public void ThrowSync() => throw new InvalidOperationException("sync-boom");

        public Task ThrowAsync() => Task.FromException(new InvalidOperationException("async-boom"));

        public Task<int> ThrowAsyncWithResult()
            => Task.FromException<int>(new ArgumentException("async-result-boom"));
    }

    // ─── Void Task interface ─────────────────────────────────────────────

    [InstrumentClass]
    public interface IVoidTaskService
    {
        Task DoWorkAsync();
    }

    public class VoidTaskService : IVoidTaskService
    {
        public Task DoWorkAsync() => Task.CompletedTask;
    }

    // ─── Complex parameter interface ─────────────────────────────────────

    [InstrumentClass]
    public interface IComplexParamService
    {
        void Process(ComplexParam param);
        void ProcessList(List<int> items);
    }

    public class ComplexParamService : IComplexParamService
    {
        public void Process(ComplexParam param) { }
        public void ProcessList(List<int> items) { }
    }

    public class ComplexParam
    {
        public string? Name { get; set; }
        public int Age { get; set; }

        [SensitiveData]
        public string? Secret { get; set; }
    }

    // ─── Non-interface class (for negative test) ─────────────────────────

    public class NotAnInterface
    {
        public int Value { get; set; }
    }

    // ─── Fake Operation Scope for verification ───────────────────────────

    public sealed class FakeOperationScope : IOperationScope
    {
        public string Name { get; }
        public string CorrelationId { get; } = Guid.NewGuid().ToString("N");
        public Activity? Activity => null;
        public TimeSpan Elapsed => TimeSpan.FromMilliseconds(1);

        public Dictionary<string, object?> Tags { get; } = new Dictionary<string, object?>();
        public Dictionary<string, Func<object?>> Properties { get; } = new Dictionary<string, Func<object?>>();
        public bool DidSucceed { get; private set; }
        public Exception? FailException { get; private set; }
        public object? ResultValue { get; private set; }
        public List<Exception> RecordedExceptions { get; } = new List<Exception>();
        public List<FakeOperationScope> Children { get; } = new List<FakeOperationScope>();

        public FakeOperationScope(string name)
        {
            Name = name;
        }

        public IOperationScope WithTag(string key, object? value)
        {
            Tags[key] = value;
            return this;
        }

        public IOperationScope WithTags(IEnumerable<KeyValuePair<string, object?>> tags)
        {
            foreach (var kv in tags)
            {
                Tags[kv.Key] = kv.Value;
            }
            return this;
        }

        public IOperationScope WithProperty(string key, Func<object?> valueFactory)
        {
            Properties[key] = valueFactory;
            return this;
        }

        public IOperationScope Fail(Exception exception)
        {
            FailException = exception;
            return this;
        }

        public IOperationScope Succeed()
        {
            DidSucceed = true;
            return this;
        }

        public IOperationScope WithResult(object? result)
        {
            ResultValue = result;
            return this;
        }

        public IOperationScope CreateChild(string name)
        {
            var child = new FakeOperationScope(name);
            Children.Add(child);
            return child;
        }

        public void RecordException(Exception exception)
        {
            RecordedExceptions.Add(exception);
        }

        public void Dispose() { }
    }

    // ─── Fake Operation Scope Factory ────────────────────────────────────

    public sealed class FakeOperationScopeFactory : IOperationScopeFactory
    {
        public List<FakeOperationScope> CreatedScopes { get; } = new List<FakeOperationScope>();

        public FakeOperationScope? LastScope =>
            CreatedScopes.Count > 0 ? CreatedScopes[CreatedScopes.Count - 1] : null;

        public IOperationScope Begin(string name, OperationScopeOptions? options = null)
        {
            var scope = new FakeOperationScope(name);
            CreatedScopes.Add(scope);
            return scope;
        }
    }
}
