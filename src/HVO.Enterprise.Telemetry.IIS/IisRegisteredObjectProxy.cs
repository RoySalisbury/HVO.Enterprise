using System;
using System.Reflection;
using System.Threading;

namespace HVO.Enterprise.Telemetry.IIS
{
    /// <summary>
    /// A <see cref="DispatchProxy"/>-based proxy that implements
    /// <c>System.Web.Hosting.IRegisteredObject</c> at runtime via reflection.
    /// This allows the IIS extension to register for graceful shutdown notifications
    /// without requiring a compile-time reference to System.Web.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On .NET Framework 4.8, IIS calls <c>IRegisteredObject.Stop(bool immediate)</c>
    /// when the app pool is recycling. This proxy intercepts that call and delegates
    /// to the configured <see cref="IisShutdownHandler"/>.
    /// </para>
    /// <para>
    /// This class is not intended for direct instantiation. Use
    /// <see cref="IisRegisteredObjectFactory.TryCreate"/> to create proxy instances.
    /// </para>
    /// </remarks>
    public class IisRegisteredObjectProxy : DispatchProxy
    {
        /// <summary>
        /// Gets or sets the shutdown handler to delegate to when IIS calls Stop.
        /// </summary>
        internal IisShutdownHandler? ShutdownHandler { get; set; }

        /// <summary>
        /// Gets or sets the shutdown timeout for graceful shutdown.
        /// </summary>
        internal TimeSpan ShutdownTimeout { get; set; }

        /// <summary>
        /// Gets or sets the delegate to unregister this object from IIS HostingEnvironment.
        /// </summary>
        internal Action? UnregisterSelf { get; set; }

        /// <inheritdoc />
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            // IRegisteredObject has a single method: void Stop(bool immediate)
            if (targetMethod != null &&
                string.Equals(targetMethod.Name, "Stop", StringComparison.Ordinal) &&
                args != null &&
                args.Length >= 1 &&
                args[0] is bool immediate)
            {
                HandleStop(immediate);
            }

            return null;
        }

        private void HandleStop(bool immediate)
        {
            if (immediate)
            {
                // Immediate shutdown - minimal cleanup
                ShutdownHandler?.OnImmediateShutdown();
                UnregisterSelf?.Invoke();
                return;
            }

            try
            {
                // Graceful shutdown - flush telemetry within timeout
                using (var cts = new CancellationTokenSource(ShutdownTimeout))
                {
                    ShutdownHandler?.OnGracefulShutdownAsync(cts.Token)
                        .GetAwaiter()
                        .GetResult();
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout expired - unregister anyway
                System.Diagnostics.Trace.WriteLine(
                    "[HVO.Enterprise.Telemetry.IIS] Graceful shutdown timed out during IRegisteredObject.Stop");
            }
            catch (Exception ex)
            {
                // Log but don't throw - IIS expects clean unregister
                System.Diagnostics.Trace.WriteLine(
                    $"[HVO.Enterprise.Telemetry.IIS] Error during IRegisteredObject.Stop: {ex}");
            }
            finally
            {
                UnregisterSelf?.Invoke();
            }
        }
    }
}
