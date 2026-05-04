namespace LogViewer
{
    /// <summary>
    /// Controls how <see cref="LogCollection"/> raises <see cref="System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged"/>
    /// for batch operations such as <see cref="LogCollection.AddRange"/> and <see cref="LogCollection.RemoveRange"/>.
    /// </summary>
    /// <remarks>
    /// Single-item operations (<c>Add</c>, <c>Remove</c>, indexer set, <c>Insert</c>, <c>RemoveAt</c>)
    /// always raise per-item events regardless of this setting.
    /// </remarks>
    public enum BatchNotificationMode
    {
        /// <summary>
        /// A single <c>Reset</c> event is raised for the entire batch. The default.
        /// Compatible with every WPF binding consumer including <c>CollectionView</c>
        /// and <c>CollectionViewSource</c>. Consumers must re-enumerate the collection
        /// to discover what changed.
        /// </summary>
        Reset = 0,

        /// <summary>
        /// A single multi-item <c>Add</c> or <c>Remove</c> event is raised carrying the
        /// changed items. Efficient for reactive consumers that handle batch payloads,
        /// but breaks WPF <c>CollectionView</c> (and any consumer derived from it),
        /// which throws <c>NotSupportedException</c> on range actions.
        /// </summary>
        Atomic = 1,

        /// <summary>
        /// One single-item <c>Add</c> or <c>Remove</c> event is raised per item in the
        /// batch. Matches <c>ObservableCollection&lt;T&gt;</c> semantics; universally
        /// compatible; the slowest option for large batches.
        /// </summary>
        PerItem = 2,
    }
}
