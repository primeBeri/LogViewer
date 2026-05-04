using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace LogViewer
{
    /// <summary>
    /// Read-only public view over a <see cref="LogCollection"/>. Forwards
    /// <see cref="CollectionChanged"/> and <see cref="PropertyChanged"/> from the
    /// underlying collection so WPF bindings continue to receive change
    /// notifications, while preventing external callers from mutating the
    /// collection directly.
    /// </summary>
    /// <remarks>
    /// Inherits from <see cref="ReadOnlyCollection{T}"/>, so it implements
    /// <see cref="System.Collections.Generic.IList{T}"/> and the non-generic
    /// <see cref="System.Collections.IList"/> in read-only mode — preserving
    /// WPF <c>ListView</c> virtualization, which queries the non-generic
    /// <c>IList</c> for random access during scrolling.
    /// </remarks>
    public sealed class ReadOnlyLogCollection
        : ReadOnlyCollection<LogEventArgs>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new <see cref="ReadOnlyLogCollection"/> wrapping the
        /// specified <see cref="LogCollection"/>.
        /// </summary>
        /// <param name="inner">The collection to expose read-only.</param>
        public ReadOnlyLogCollection(LogCollection inner) : base(inner)
        {
            inner.CollectionChanged += (s, e) => CollectionChanged?.Invoke(this, e);
            inner.PropertyChanged += (s, e) => PropertyChanged?.Invoke(this, e);
        }

        /// <inheritdoc/>
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
