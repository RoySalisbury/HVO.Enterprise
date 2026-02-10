using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HVO.Enterprise.Telemetry.Abstractions;

namespace HVO.Enterprise.Telemetry
{
    /// <summary>
    /// Extension methods for executing operations within scopes.
    /// </summary>
    public static class OperationScopeExtensions
    {
        /// <summary>
        /// Executes an action within an operation scope.
        /// </summary>
        /// <param name="factory">Scope factory.</param>
        /// <param name="name">Operation name.</param>
        /// <param name="action">Action to execute.</param>
        /// <param name="options">Optional scope options.</param>
        public static void Execute(
            this IOperationScopeFactory factory,
            string name,
            Action action,
            OperationScopeOptions? options = null)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            using (var scope = factory.Begin(name, options))
            {
                try
                {
                    action();
                    scope.Succeed();
                }
                catch (Exception ex)
                {
                    scope.Fail(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Executes a function within an operation scope.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="factory">Scope factory.</param>
        /// <param name="name">Operation name.</param>
        /// <param name="func">Function to execute.</param>
        /// <param name="options">Optional scope options.</param>
        /// <returns>Result from the function.</returns>
        public static T Execute<T>(
            this IOperationScopeFactory factory,
            string name,
            Func<T> func,
            OperationScopeOptions? options = null)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            using (var scope = factory.Begin(name, options))
            {
                try
                {
                    var result = func();
                    scope.Succeed().WithResult(result);
                    return result;
                }
                catch (Exception ex)
                {
                    scope.Fail(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Executes an async action within an operation scope.
        /// </summary>
        /// <param name="factory">Scope factory.</param>
        /// <param name="name">Operation name.</param>
        /// <param name="action">Async action to execute.</param>
        /// <param name="options">Optional scope options.</param>
        /// <returns>Task representing the async operation.</returns>
        public static async Task ExecuteAsync(
            this IOperationScopeFactory factory,
            string name,
            Func<Task> action,
            OperationScopeOptions? options = null)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            using (var scope = factory.Begin(name, options))
            {
                try
                {
                    await action().ConfigureAwait(false);
                    scope.Succeed();
                }
                catch (Exception ex)
                {
                    scope.Fail(ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// Executes an async function within an operation scope.
        /// </summary>
        /// <typeparam name="T">Result type.</typeparam>
        /// <param name="factory">Scope factory.</param>
        /// <param name="name">Operation name.</param>
        /// <param name="func">Async function to execute.</param>
        /// <param name="options">Optional scope options.</param>
        /// <returns>Task representing the async operation result.</returns>
        public static async Task<T> ExecuteAsync<T>(
            this IOperationScopeFactory factory,
            string name,
            Func<Task<T>> func,
            OperationScopeOptions? options = null)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            using (var scope = factory.Begin(name, options))
            {
                try
                {
                    var result = await func().ConfigureAwait(false);
                    scope.Succeed().WithResult(result);
                    return result;
                }
                catch (Exception ex)
                {
                    scope.Fail(ex);
                    throw;
                }
            }
        }
    }
}
