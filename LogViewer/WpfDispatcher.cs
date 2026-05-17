using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LogViewer
{
    /// <summary>
    /// WPF-specific implementation of <see cref="IDispatcher"/> that delegates to
    /// <see cref="System.Windows.Threading.Dispatcher"/>.
    /// </summary>
    public sealed class WpfDispatcher : IDispatcher
    {
        private readonly Dispatcher _dispatcher;

        /// <summary>
        /// Initializes a new instance of <see cref="WpfDispatcher"/> wrapping the specified WPF dispatcher.
        /// </summary>
        /// <param name="dispatcher">The WPF dispatcher to wrap. Must not be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dispatcher"/> is <see langword="null"/>.</exception>
        public WpfDispatcher(Dispatcher dispatcher)
        {
            ArgumentNullException.ThrowIfNull(dispatcher);
            _dispatcher = dispatcher;
        }

        /// <inheritdoc/>
        public bool CheckAccess() => _dispatcher.CheckAccess();

        /// <inheritdoc/>
        public T Invoke<T>(Func<T> callback) => _dispatcher.Invoke(callback);

        /// <inheritdoc/>
        public Task InvokeAsync(Action callback) =>
            _dispatcher.InvokeAsync(callback).Task;

        /// <inheritdoc/>
        public Task<T> InvokeAsync<T>(Func<T> callback) =>
            _dispatcher.InvokeAsync(callback).Task;
    }
}
