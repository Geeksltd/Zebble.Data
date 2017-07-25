namespace Zebble.Data.Services
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Provides Sorting services for all entities.
    /// </summary>
    public static class Sorter
    {
        public const int INCREMENT = 10;

        static readonly object SyncLock = new object();

        public static ISortable FindItemAbove(ISortable item)
        {
            return FindSiblings(item).Except(item).Where(o => o.Order <= item.Order).WithMax(o => o.Order);
        }

        public static ISortable FindItemBelow(ISortable item)
        {
            return FindSiblings(item).Except(item).Where(i => i.Order >= item.Order).WithMin(i => i.Order);
        }

        public static bool CanMoveUp(ISortable item)
        {
            return FindItemAbove(item) != null;
        }

        public static bool CanMoveDown(ISortable item)
        {
            return FindItemBelow(item) != null;
        }

        /// <summary>
        /// Moves this item before a specified other item. If null is specified, it will be moved to the end of its siblings.
        /// </summary>
        public static void MoveBefore(ISortable item, ISortable before, SaveBehaviour saveBehaviour = SaveBehaviour.Default)
        {
            var newOrder = (before == null ? int.MaxValue : before.Order) - 1;

            if (newOrder < 0) newOrder = 0;

            item = Database.Update(item, o => o.Order = newOrder, saveBehaviour);

            JustifyOrders(item, saveBehaviour);
        }

        /// <summary>
        /// Moves this item after a specified other item. If null is specified, it will be moved to the beginning of its siblings.
        /// </summary>
        public static void MoveAfter(ISortable item, ISortable after, SaveBehaviour saveBehaviour = SaveBehaviour.Default)
        {
            var newOrder = (after == null ? 0 : after.Order) + 1;

            item = Database.Update(item, o => o.Order = newOrder, saveBehaviour);

            JustifyOrders(item, saveBehaviour);
        }

        /// <summary>
        /// Moves an item up among its siblings. Returns False if the item is already first in the list, otherwise true.
        /// </summary>
        public static bool MoveUp(ISortable item, SaveBehaviour saveBehaviour = SaveBehaviour.Default)
        {
            lock (SyncLock)
            {
                var above = FindItemAbove(item);

                if (above == null)
                    return false;

                if (above.Order == item.Order)
                    above.Order--;

                Swap(item, above, saveBehaviour);

                item = Database.Reload(item);
                above = Database.Reload(above);

                JustifyOrders(item, saveBehaviour);

                return true;
            }
        }

        /// <summary>
        /// Moves an item up to first among its siblings. Returns False if the item is already first in the list, otherwise true.
        /// </summary>
        public static bool MoveFirst(ISortable item, SaveBehaviour saveBehaviour = SaveBehaviour.Default)
        {
            lock (SyncLock)
            {
                var first = FindSiblings(item).Min(o => o.Order);

                if (first <= 0)
                    return false;

                Database.Update(item, o => o.Order = first - 1, saveBehaviour);
                JustifyOrders(item, saveBehaviour);
                return true;
            }
        }

        /// <summary>
        /// Moves an item up to last among its siblings. Always returns true.
        /// </summary>
        public static bool MoveLast(ISortable item, SaveBehaviour saveBehaviour = SaveBehaviour.Default)
        {
            lock (SyncLock)
            {
                var last = FindSiblings(item).Max(o => o.Order);

                Database.Update(item, o => o.Order = last + 1, saveBehaviour);
                JustifyOrders(item, saveBehaviour);
                return true;
            }
        }

        /// <summary>
        /// Moves an item down among its siblings. Returns False if the item is already last in the list, otherwise true.
        /// </summary>
        public static bool MoveDown(ISortable item, SaveBehaviour saveBehaviour = SaveBehaviour.Default)
        {
            lock (SyncLock)
            {
                var below = FindItemBelow(item);

                if (below == null)
                    return false;

                if (below.Order == item.Order)
                    item.Order++;

                Swap(item, below, saveBehaviour);

                JustifyOrders(item, saveBehaviour);

                return true;
            }
        }

        /// <summary>
        /// Swaps the order of two specified items.
        /// </summary>
        static void Swap(ISortable one, ISortable two, SaveBehaviour saveBehaviour)
        {
            var somethingAboveAll = FindSiblings(one).Max(i => i.Order) + 10;

            Database.EnlistOrCreateTransaction(() =>
            {
                var order1 = two.Order;
                var order2 = one.Order;

                Database.Update(one, i => i.Order = order1, saveBehaviour);
                Database.Update(two, i => i.Order = order2, saveBehaviour);
            });
        }

        /// <summary>
        /// Justifies the order of a specified item and its siblings. 
        /// The value of the "Order" property in those objects will be 10, 20, 30, ...
        /// </summary>
        public static void JustifyOrders(ISortable item, SaveBehaviour saveBehaviour = SaveBehaviour.Default)
        {
            lock (SyncLock)
            {
                var changed = new List<Entity>();

                var order = 0;

                foreach (var sibling in FindSiblings(item).OrderBy(i => i.Order).ToArray())
                {
                    order += INCREMENT;
                    if (sibling.Order == order) continue;

                    var clone = sibling.Clone() as Entity;
                    (clone as ISortable).Order = order;
                    changed.Add(clone);
                }

                Database.Save(changed, saveBehaviour);
            }
        }

        /// <summary>
        /// Discovers the siblings of the specified sortable object.
        /// </summary>
        static IEnumerable<ISortable> FindSiblings(ISortable item)
        {
            var getSiblingsMethod = item.GetType().GetMethod("GetSiblings", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            bool isAcceptable = true;

            if (getSiblingsMethod == null) isAcceptable = false;
            else if (getSiblingsMethod.GetParameters().Any()) isAcceptable = false;
            else if (!getSiblingsMethod.ReturnType.Implements(typeof(IEnumerable))) isAcceptable = false;

            IEnumerable<ISortable> result;

            if (!isAcceptable)
            {
                result = Database.GetList(item.GetType()).Cast<ISortable>();
            }
            else
            {
                var list = new List<ISortable>();

                try
                {
                    foreach (ISortable element in getSiblingsMethod.Invoke(item, null) as IEnumerable)
                        list.Add(element);
                }
                catch (Exception ex)
                {
                    throw new Exception("Services.Sorter Could not process the GetSiblings method from the " + item.GetType().Name + " instance.", ex);
                }

                result = list;
            }

            return result.OrderBy(i => i.Order).ToList();
        }

        /// <summary>
        /// Gets the Next order for an ISortable entity.
        /// The result will be 10 plus the largest order of its siblings.
        /// </summary>
        public static int GetNewOrder(ISortable item)
        {
            lock (SyncLock)
            {
                if (!item.IsNew)
                    throw new ArgumentException("item", "Sorter.GetNewOrder() method needs a new ISortable argument (with IsNew set to true).");

                return INCREMENT + (FindSiblings(item).LastOrDefault()?.Order ?? 0);
            }
        }

    }
}