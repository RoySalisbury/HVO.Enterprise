using System;

namespace HVO.Enterprise.Telemetry.Abstractions
{
    /// <summary>
    /// Factory for creating operation scopes.
    /// </summary>
    public interface IOperationScopeFactory
    {
        /// <summary>
        /// Creates a new operation scope.
        /// </summary>
        /// <param name="name">Operation name.</param>
        /// <param name="options">Optional scope options.</param>
        /// <returns>New operation scope.</returns>
        IOperationScope Begin(string name, OperationScopeOptions? options = null);
    }
}
