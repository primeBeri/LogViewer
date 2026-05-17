using System;
using System.Threading.Tasks;

namespace LogViewer
{
    /// <summary>
    /// Abstracts <see cref="System.Windows.Threading.Dispatcher"/> for testability.
    /// Implementations marshal calls to a specific thread (typically the UI thread).
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// Determines whether the calling thread has access to this dispatcher.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the calling thread has access to this dispatcher; otherwise, <see langword="false"/>.
        /// </returns>
        bool CheckAccess();

        /// <summary>
        /// Executes the specified callback synchronously on the thread associated with this dispatcher.
        /// </summary>
        /// <typeparam name="T">The return type of the callback.</typeparam>
        /// <param name="callback">The function to invoke on the dispatcher thread.</param>
        /// <returns>The value returned by <paramref name="callback"/>.</returns>
        T Invoke<T>(Func<T> callback);

        /// <summary>
        /// Executes the specified action asynchronously on the thread associated with this dispatcher.
        /// </summary>
        /// <param name="callback">The action to invoke on the dispatcher thread.</param>
        /// <returns>A <see cref="Task"/> that completes when the action has finished executing.</returns>
        Task InvokeAsync(Action callback);

        /// <summary>
        /// Executes the specified function asynchronously on the thread associated with this dispatcher.
        /// </summary>
        /// <typeparam name="T">The return type of the callback.</typeparam>
        /// <param name="callback">The function to invoke on the dispatcher thread.</param>
        /// <returns>
        /// A <see cref="Task{T}"/> that completes when the function has finished executing,
        /// with the value returned by <paramref name="callback"/>.
        /// </returns>
        Task<T> InvokeAsync<T>(Func<T> callback);
    }
}
