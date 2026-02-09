using System;

namespace HVO.Enterprise.Telemetry.Wcf.Server
{
    /// <summary>
    /// Marker attribute to indicate that a WCF service class should be
    /// instrumented with telemetry on the server side.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute serves as a marker for server-side WCF services.
    /// The actual registration of dispatch message inspectors must happen
    /// programmatically via <see cref="WcfServerIntegration.TryAddTelemetryInspector"/>
    /// because the <c>IServiceBehavior</c> interface is not available in the
    /// <c>System.ServiceModel.Primitives</c> NuGet package.
    /// </para>
    /// <para>
    /// On .NET Framework 4.8, this attribute can be detected by hosting code
    /// to automatically register telemetry when a service host is opened:
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [WcfTelemetryBehavior]
    /// [ServiceContract]
    /// public class CustomerService : ICustomerService
    /// {
    ///     [OperationContract]
    ///     public Customer GetCustomer(int id)
    ///     {
    ///         // Telemetry automatically captured when registered
    ///         return _repository.GetCustomer(id);
    ///     }
    /// }
    ///
    /// // Registration code (typically in hosting setup):
    /// var host = new ServiceHost(typeof(CustomerService));
    /// host.Opening += (s, e) =&gt;
    /// {
    ///     if (typeof(CustomerService).GetCustomAttribute&lt;WcfTelemetryBehaviorAttribute&gt;() != null)
    ///     {
    ///         WcfServerIntegration.TryAddTelemetryInspector(host);
    ///     }
    /// };
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class WcfTelemetryBehaviorAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets whether to propagate trace context in reply messages.
        /// Default: <c>true</c>.
        /// </summary>
        public bool PropagateTraceContextInReply { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to capture WCF fault details in Activity tags.
        /// Default: <c>true</c>.
        /// </summary>
        public bool CaptureFaultDetails { get; set; } = true;
    }
}
