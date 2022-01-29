namespace IZEncoder.Common.ASSParser.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    ///     Observable collection of <see cref="SubEvent" />.
    /// </summary>
    public class EventCollection : ObservableCollection<SubEvent>
    {
        /// <summary>
        ///     Reorder the items in this <see cref="EventCollection" /> by the <paramref name="comparer" />.
        /// </summary>
        /// <param name="comparer">
        ///     The <see cref="IComparer{SubEvent}" /> to compare the <see cref="SubEvent" /> in this
        ///     <see cref="EventCollection" />
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="comparer" /> is <c>null</c>.</exception>
        public void Sort(IComparer<SubEvent> comparer)
        {
            if (comparer == null)
                throw new ArgumentNullException(nameof(comparer));
            if (Items is List<SubEvent> l)
            {
                l.Sort(comparer);
            }
            else
            {
                var items = Items.OrderBy(i => i, comparer).ToList();
                Items.Clear();
                foreach (var item in items) Items.Add(item);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Items)));
        }

        /// <summary>
        ///     Reorder the items in this <see cref="EventCollection" /> by the <paramref name="comparison" />.
        /// </summary>
        /// <param name="comparison">
        ///     The <see cref="Comparison{SubEvent}" /> to compare the <see cref="SubEvent" /> in this
        ///     <see cref="EventCollection" />
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="comparison" /> is <c>null</c>.</exception>
        public void Sort(Comparison<SubEvent> comparison)
        {
            if (comparison == null)
                throw new ArgumentNullException(nameof(comparison));
            Sort(Comparer<SubEvent>.Create(comparison));
        }

        /// <summary>
        ///     Reorder the items in this <see cref="EventCollection" /> by <see cref="SubEvent.StartTime" />.
        /// </summary>
        public void SortByTime()
        {
            Sort((a, b) =>
            {
                var c1 = TimeSpan.Compare(a.StartTime, b.StartTime);
                if (c1 != 0)
                    return c1;
                var c2 = string.Compare(a.Style, b.Style);
                if (c2 != 0)
                    return c2;
                return TimeSpan.Compare(a.EndTime, b.EndTime);
            });
        }

        /// <summary>
        ///     Reorder the items in this <see cref="EventCollection" /> by <see cref="SubEvent.Style" />.
        /// </summary>
        public void SortByStyle()
        {
            Sort((a, b) =>
            {
                var c2 = string.Compare(a.Style, b.Style);
                if (c2 != 0)
                    return c2;
                var c1 = TimeSpan.Compare(a.StartTime, b.StartTime);
                if (c1 != 0)
                    return c1;
                return TimeSpan.Compare(a.EndTime, b.EndTime);
            });
        }
    }
}