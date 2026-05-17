using System.Collections.Specialized;
using System.ComponentModel;
using FluentAssertions;
using LogViewer;
using Microsoft.Extensions.Logging;
using Xunit;

namespace LogViewer.Tests
{
    public class LogCollectionTests
    {
        private static LogEventArgs Make(string handle = "h", string text = "m")
            => new(LogLevel.Information, handle, text, LogColor.Black) { LogDateTime = DateTime.UtcNow };

        private sealed class EventRecorder
        {
            public List<NotifyCollectionChangedEventArgs> Collection { get; } = [];
            public List<string?> Property { get; } = [];

            public EventRecorder(LogCollection c)
            {
                c.CollectionChanged += (_, e) => Collection.Add(e);
                c.PropertyChanged += (_, e) => Property.Add(e.PropertyName);
            }
        }

        // -------- single-item operations --------

        [Fact]
        public void Add_NewItem_ReturnsTrueAndRaisesEvents()
        {
            var c = new LogCollection();
            var rec = new EventRecorder(c);
            var e = Make();

            c.Add(e).Should().BeTrue();

            c.Count.Should().Be(1);
            rec.Collection.Should().HaveCount(1);
            rec.Collection[0].Action.Should().Be(NotifyCollectionChangedAction.Add);
            rec.Property.Should().Contain(nameof(LogCollection.Count));
        }

        [Fact]
        public void Add_Duplicate_ReturnsFalseAndDoesNotRaise()
        {
            var c = new LogCollection();
            var e = Make();
            c.Add(e);
            var rec = new EventRecorder(c);

            c.Add(e).Should().BeFalse();

            c.Count.Should().Be(1);
            rec.Collection.Should().BeEmpty();
        }

        [Fact]
        public void Add_Null_Throws()
        {
            var c = new LogCollection();
            ((Action)(() => c.Add(null!))).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Remove_Existing_ReturnsTrueAndRaisesEvents()
        {
            var c = new LogCollection();
            var e = Make();
            c.Add(e);
            var rec = new EventRecorder(c);

            c.Remove(e).Should().BeTrue();

            c.Count.Should().Be(0);
            rec.Collection.Should().HaveCount(1);
            rec.Collection[0].Action.Should().Be(NotifyCollectionChangedAction.Remove);
        }

        [Fact]
        public void Remove_NotPresent_ReturnsFalse()
        {
            var c = new LogCollection();
            var e = Make();

            c.Remove(e).Should().BeFalse();
        }

        [Fact]
        public void Remove_Null_Throws()
        {
            var c = new LogCollection();
            ((Action)(() => c.Remove(null!))).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Clear_EmptiesAndRaisesReset()
        {
            var c = new LogCollection();
            c.Add(Make());
            c.Add(Make());
            var rec = new EventRecorder(c);

            c.Clear();

            c.Count.Should().Be(0);
            rec.Collection.Should().HaveCount(1);
            rec.Collection[0].Action.Should().Be(NotifyCollectionChangedAction.Reset);
        }

        [Fact]
        public void Indexer_Get_ReturnsItem()
        {
            var c = new LogCollection();
            var a = Make("a"); var b = Make("b");
            c.Add(a); c.Add(b);

            c[0].Should().BeSameAs(a);
            c[1].Should().BeSameAs(b);
        }

        [Fact]
        public void Indexer_Set_ReplacesWithUniqueValue()
        {
            var c = new LogCollection();
            var a = Make("a"); var b = Make("b"); var rep = Make("c");
            c.Add(a); c.Add(b);

            c[0] = rep;

            c[0].Should().BeSameAs(rep);
            c[1].Should().BeSameAs(b);
            c.Contains(a).Should().BeFalse();
        }

        [Fact]
        public void Indexer_Set_DuplicateThrows()
        {
            var c = new LogCollection();
            var a = Make("a"); var b = Make("b");
            c.Add(a); c.Add(b);

            ((Action)(() => c[0] = b)).Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Insert_AddsAtPosition()
        {
            var c = new LogCollection();
            var a = Make("a"); var b = Make("b"); var ins = Make("c");
            c.Add(a); c.Add(b);

            c.Insert(1, ins);

            c.Count.Should().Be(3);
            c[0].Should().BeSameAs(a);
            c[1].Should().BeSameAs(ins);
            c[2].Should().BeSameAs(b);
        }

        [Fact]
        public void Insert_Duplicate_Throws()
        {
            var c = new LogCollection();
            var a = Make("a");
            c.Add(a);

            ((Action)(() => c.Insert(0, a))).Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void RemoveAt_Removes()
        {
            var c = new LogCollection();
            var a = Make("a"); var b = Make("b");
            c.Add(a); c.Add(b);

            c.RemoveAt(0);

            c.Count.Should().Be(1);
            c[0].Should().BeSameAs(b);
            c.Contains(a).Should().BeFalse();
        }

        [Fact]
        public void RemoveAt_OutOfRange_Throws()
        {
            var c = new LogCollection();
            c.Add(Make());

            ((Action)(() => c.RemoveAt(5))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => c.RemoveAt(-1))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Contains_ReflectsMembership()
        {
            var c = new LogCollection();
            var a = Make("a"); var b = Make("b");
            c.Add(a);

            c.Contains(a).Should().BeTrue();
            c.Contains(b).Should().BeFalse();
        }

        [Fact]
        public void IndexOf_ReturnsIndex_OrMinusOne()
        {
            var c = new LogCollection();
            var a = Make("a"); var b = Make("b"); var c2 = Make("c");
            c.Add(a); c.Add(b);

            c.IndexOf(a).Should().Be(0);
            c.IndexOf(b).Should().Be(1);
            c.IndexOf(c2).Should().Be(-1);
        }

        // -------- batch operations --------

        [Fact]
        public void AddRange_AddsAllUnique()
        {
            var c = new LogCollection();
            var items = new[] { Make("a"), Make("b"), Make("c") };

            int added = c.AddRange(items);

            added.Should().Be(3);
            c.Count.Should().Be(3);
        }

        [Fact]
        public void AddRange_SkipsDuplicates()
        {
            var c = new LogCollection();
            var a = Make("a"); var b = Make("b");
            c.Add(a);

            int added = c.AddRange(new[] { a, b });

            added.Should().Be(1);
            c.Count.Should().Be(2);
        }

        [Fact]
        public void AddRange_DefaultMode_RaisesReset()
        {
            var c = new LogCollection();
            var rec = new EventRecorder(c);

            c.AddRange(new[] { Make("a"), Make("b"), Make("c") });

            rec.Collection.Should().HaveCount(1);
            rec.Collection[0].Action.Should().Be(NotifyCollectionChangedAction.Reset);
        }

        [Fact]
        public void AddRange_AtomicMode_RaisesSingleMultiItemAddEvent()
        {
            var c = new LogCollection { NotificationMode = BatchNotificationMode.Atomic };
            var rec = new EventRecorder(c);

            c.AddRange(new[] { Make("a"), Make("b"), Make("c") });

            rec.Collection.Should().HaveCount(1);
            rec.Collection[0].Action.Should().Be(NotifyCollectionChangedAction.Add);
            rec.Collection[0].NewItems.Should().NotBeNull();
            rec.Collection[0].NewItems!.Count.Should().Be(3);
            rec.Collection[0].NewStartingIndex.Should().Be(0);
        }

        [Fact]
        public void AddRange_PerItemMode_RaisesOneAddEventPerItem()
        {
            var c = new LogCollection { NotificationMode = BatchNotificationMode.PerItem };
            var rec = new EventRecorder(c);
            var items = new[] { Make("a"), Make("b"), Make("c") };

            c.AddRange(items);

            rec.Collection.Should().HaveCount(3);
            rec.Collection.Should().AllSatisfy(e => e.Action.Should().Be(NotifyCollectionChangedAction.Add));
            for (int i = 0; i < 3; i++)
            {
                rec.Collection[i].NewStartingIndex.Should().Be(i);
                rec.Collection[i].NewItems![0].Should().BeSameAs(items[i]);
            }
        }

        [Fact]
        public void RemoveRange_RemovesItems()
        {
            var c = new LogCollection();
            var items = new[] { Make("a"), Make("b"), Make("c"), Make("d"), Make("e") };
            foreach (var i in items) c.Add(i);

            int removed = c.RemoveRange(1, 3);

            removed.Should().Be(3);
            c.Count.Should().Be(2);
            c[0].Should().BeSameAs(items[0]);
            c[1].Should().BeSameAs(items[4]);
        }

        [Fact]
        public void RemoveRange_DefaultMode_RaisesReset()
        {
            var c = new LogCollection();
            for (int i = 0; i < 5; i++) c.Add(Make($"h{i}"));
            var rec = new EventRecorder(c);

            c.RemoveRange(1, 3);

            rec.Collection.Should().HaveCount(1);
            rec.Collection[0].Action.Should().Be(NotifyCollectionChangedAction.Reset);
        }

        [Fact]
        public void RemoveRange_AtomicMode_RaisesSingleMultiItemRemoveEvent()
        {
            var c = new LogCollection { NotificationMode = BatchNotificationMode.Atomic };
            for (int i = 0; i < 5; i++) c.Add(Make($"h{i}"));
            var rec = new EventRecorder(c);

            c.RemoveRange(1, 3);

            rec.Collection.Should().HaveCount(1);
            rec.Collection[0].Action.Should().Be(NotifyCollectionChangedAction.Remove);
            rec.Collection[0].OldItems.Should().NotBeNull();
            rec.Collection[0].OldItems!.Count.Should().Be(3);
            rec.Collection[0].OldStartingIndex.Should().Be(1);
        }

        [Fact]
        public void RemoveRange_PerItemMode_RaisesEventsHighestIndexFirst()
        {
            var c = new LogCollection { NotificationMode = BatchNotificationMode.PerItem };
            var items = new[] { Make("a"), Make("b"), Make("c"), Make("d"), Make("e") };
            foreach (var i in items) c.Add(i);
            var rec = new EventRecorder(c);

            c.RemoveRange(1, 3); // remove items[1], items[2], items[3]

            rec.Collection.Should().HaveCount(3);
            rec.Collection.Should().AllSatisfy(e => e.Action.Should().Be(NotifyCollectionChangedAction.Remove));
            // expected order: index 3 (items[3]), index 2 (items[2]), index 1 (items[1])
            rec.Collection[0].OldStartingIndex.Should().Be(3);
            rec.Collection[0].OldItems![0].Should().BeSameAs(items[3]);
            rec.Collection[1].OldStartingIndex.Should().Be(2);
            rec.Collection[1].OldItems![0].Should().BeSameAs(items[2]);
            rec.Collection[2].OldStartingIndex.Should().Be(1);
            rec.Collection[2].OldItems![0].Should().BeSameAs(items[1]);
        }

        [Fact]
        public void RemoveRange_NegativeArgs_Throw()
        {
            var c = new LogCollection();
            ((Action)(() => c.RemoveRange(-1, 1))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => c.RemoveRange(0, -1))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void AddRange_NullItem_Throws()
        {
            var c = new LogCollection();
            ((Action)(() => c.AddRange(new LogEventArgs[] { Make("a"), null! }))).Should().Throw<ArgumentNullException>();
        }

        // -------- other --------

        [Fact]
        public void Count_TracksMutations()
        {
            var c = new LogCollection();
            c.Count.Should().Be(0);
            c.Add(Make("a"));
            c.Count.Should().Be(1);
            c.Add(Make("b"));
            c.Count.Should().Be(2);
            c.Clear();
            c.Count.Should().Be(0);
        }

        [Fact]
        public void IsReadOnly_IsFalse()
        {
            var c = new LogCollection();
            c.IsReadOnly.Should().BeFalse();
        }

        [Fact]
        public void GetEnumerator_ReturnsSnapshot_NotLiveView()
        {
            var c = new LogCollection();
            c.Add(Make("a"));
            c.Add(Make("b"));

            var enumerator = c.GetEnumerator();
            c.Add(Make("c")); // mutate after enumerator obtained

            int seen = 0;
            while (enumerator.MoveNext()) seen++;

            seen.Should().Be(2); // snapshot reflects state at enumerator creation
        }
    }
}
