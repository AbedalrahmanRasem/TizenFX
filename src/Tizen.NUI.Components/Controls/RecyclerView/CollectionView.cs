﻿/* Copyright (c) 2021 Samsung Electronics Co., Ltd.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows.Input;
using System.ComponentModel;
using Tizen.NUI.BaseComponents;
using Tizen.NUI.Binding;

namespace Tizen.NUI.Components
{
    /// <summary>
    /// Selectable RecyclerView that presenting a collection of items with variable layouters.
    /// </summary>
    /// <since_tizen> 9 </since_tizen>
    public partial class CollectionView : RecyclerView
    {
        /// <summary>
        /// Binding Property of selected item in single selection.
        /// </summary>
        /// <since_tizen> 9 </since_tizen>
        public static readonly BindableProperty SelectedItemProperty =
            BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(CollectionView), null,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var colView = bindable as CollectionView;
                    if (colView == null)
                    {
                        throw new Exception("Bindable object is not CollectionView.");
                    }

                    oldValue = colView.selectedItem;
                    colView.selectedItem = newValue;
                    var args = new SelectionChangedEventArgs(oldValue, newValue);

                    foreach (RecyclerViewItem item in colView.ContentContainer.Children.Where((item) => item is RecyclerViewItem))
                    {
                        if (item.BindingContext == null)
                        {
                            continue;
                        }

                        if (item.BindingContext == oldValue)
                        {
                            item.IsSelected = false;
                        }
                        else if (item.BindingContext == newValue)
                        {
                            item.IsSelected = true;
                        }
                    }

                    SelectionPropertyChanged(colView, args);
                },
                defaultValueCreator: (bindable) =>
                {
                    var colView = bindable as CollectionView;
                    if (colView == null)
                    {
                        throw new Exception("Bindable object is not CollectionView.");
                    }

                    return colView.selectedItem;
                });

        /// <summary>
        /// Binding Property of selected items list in multiple selection.
        /// </summary>
        /// <since_tizen> 9 </since_tizen>
        public static readonly BindableProperty SelectedItemsProperty =
            BindableProperty.Create(nameof(SelectedItems), typeof(IList<object>), typeof(CollectionView), null,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var colView = bindable as CollectionView;
                    if (colView == null)
                    {
                        throw new Exception("Bindable object is not CollectionView.");
                    }

                    var oldSelection = colView.selectedItems ?? selectEmpty;
                    //FIXME : CoerceSelectedItems calls only isCreatedByXaml
                    var newSelection = (SelectionList)CoerceSelectedItems(colView, newValue);
                    colView.selectedItems = newSelection;
                    colView.SelectedItemsPropertyChanged(oldSelection, newSelection);
                },
                defaultValueCreator: (bindable) =>
                {
                    var colView = bindable as CollectionView;
                    if (colView == null)
                    {
                        throw new Exception("Bindable object is not CollectionView.");
                    }

                    colView.selectedItems = colView.selectedItems ?? new SelectionList(colView);
                    return colView.selectedItems;
                });

        /// <summary>
        /// Binding Property of selected items list in multiple selection.
        /// </summary>
        /// <since_tizen> 9 </since_tizen>
        public static readonly BindableProperty SelectionModeProperty =
            BindableProperty.Create(nameof(SelectionMode), typeof(ItemSelectionMode), typeof(CollectionView), ItemSelectionMode.None,
                propertyChanged: (bindable, oldValue, newValue) =>
                {
                    var colView = bindable as CollectionView;
                    if (colView == null)
                    {
                        throw new Exception("Bindable object is not CollectionView.");
                    }

                    oldValue = colView.selectionMode;
                    colView.selectionMode = (ItemSelectionMode)newValue;
                    SelectionModePropertyChanged(colView, oldValue, newValue);
                },
                defaultValueCreator: (bindable) =>
                {
                    var colView = bindable as CollectionView;
                    if (colView == null)
                    {
                        throw new Exception("Bindable object is not CollectionView.");
                    }

                    return colView.selectionMode;
                });


        private static readonly IList<object> selectEmpty = new List<object>(0);
        private DataTemplate itemTemplate = null;
        private IEnumerable itemsSource = null;
        private ItemsLayouter itemsLayouter = null;
        private DataTemplate groupHeaderTemplate;
        private DataTemplate groupFooterTemplate;
        private bool isGrouped;
        private bool wasRelayouted = false;
        private bool needInitalizeLayouter = false;
        private object selectedItem;
        private SelectionList selectedItems;
        private bool suppressSelectionChangeNotification;
        private ItemSelectionMode selectionMode = ItemSelectionMode.None;
        private RecyclerViewItem header;
        private RecyclerViewItem footer;
        private List<RecyclerViewItem> recycleGroupHeaderCache { get; } = new List<RecyclerViewItem>();
        private List<RecyclerViewItem> recycleGroupFooterCache { get; } = new List<RecyclerViewItem>();
        private bool delayedScrollTo;
        private (float position, bool anim) delayedScrollToParam;

        private bool delayedIndexScrollTo;
        private (int index, bool anim, ItemScrollTo scrollTo) delayedIndexScrollToParam;

        private void Initialize()
        {
            FocusGroup = true;
            SetKeyboardNavigationSupport(true);
        }

        /// <summary>
        /// Base constructor.
        /// </summary>
        /// <since_tizen> 9 </since_tizen>
        public CollectionView() : base()
        {
            Initialize();
        }

        /// <summary>
        /// Base constructor with ItemsSource
        /// </summary>
        /// <param name="itemsSource">item's data source</param>
        /// <since_tizen> 9 </since_tizen>
        public CollectionView(IEnumerable itemsSource) : this()
        {
            ItemsSource = itemsSource;
        }

        /// <summary>
        /// Base constructor with ItemsSource, ItemsLayouter and ItemTemplate
        /// </summary>
        /// <param name="itemsSource">item's data source</param>
        /// <param name="layouter">item's layout manager</param>
        /// <param name="template">item's view template with data bindings</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CollectionView(IEnumerable itemsSource, ItemsLayouter layouter, DataTemplate template) : this()
        {
            ItemsSource = itemsSource;
            ItemTemplate = template;
            ItemsLayouter = layouter;
        }

        /// <summary>
        /// Creates a new instance of a CollectionView with style.
        /// </summary>
        /// <param name="style">A style applied to the newly created CollectionView.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public CollectionView(ControlStyle style) : base(style)
        {
            Initialize();
        }

        /// <summary>
        /// Event of Selection changed.
        /// previous selection list and current selection will be provided.
        /// </summary>
        /// <since_tizen> 9 </since_tizen>
        public event EventHandler<SelectionChangedEventArgs> SelectionChanged;

        /// <summary>
        /// Align item in the viewport when ScrollTo() calls.
        /// </summary>
        /// <since_tizen> 9 </since_tizen>
        public enum ItemScrollTo
        {
            /// <summary>
            /// Scroll to show item in nearest viewport on scroll direction.
            /// item is above the scroll viewport, item will be came into front,
            /// item is under the scroll viewport, item will be came into end,
            /// item is in the scroll viewport, no scroll.
            /// </summary>
            /// <since_tizen> 9 </since_tizen>
            Nearest,
            /// <summary>
            /// Scroll to show item in start of the viewport.
            /// </summary>
            /// <since_tizen> 9 </since_tizen>
            Start,
            /// <summary>
            /// Scroll to show item in center of the viewport.
            /// </summary>
            /// <since_tizen> 9 </since_tizen>
            Center,
            /// <summary>
            /// Scroll to show item in end of the viewport.
            /// </summary>
            /// <since_tizen> 9 </since_tizen>
            End,
        }

        /// <summary>
        /// Item's source data in IEnumerable.
        /// </summary>
        /// <since_tizen> 9 </since_tizen>
        public override IEnumerable ItemsSource
        {
            get => GetValue(RecyclerView.ItemsSourceProperty) as IEnumerable;
            set => SetValue(RecyclerView.ItemsSourceProperty, value);
        }

        internal override IEnumerable InternalItemsSource
        {
            get
            {
                return itemsSource;
            }
            set
            {
                if (itemsSource != null)
                {
                    // Clearing old data!
                    if (itemsSource is INotifyCollectionChanged prevNotifyCollectionChanged)
                    {
                        prevNotifyCollectionChanged.CollectionChanged -= CollectionChanged;
                    }
                    if (selectedItem != null)
                    {
                        selectedItem = null;
                    }
                    selectedItems?.Clear();
                }

                itemsSource = value as IEnumerable;

                if (itemsSource == null)
                {
                    InternalItemSource?.Dispose();
                    InternalItemSource = null;
                    itemsLayouter?.Clear();
                    ClearCache();
                    return;
                }
                if (itemsSource is INotifyCollectionChanged newNotifyCollectionChanged)
                {
                    newNotifyCollectionChanged.CollectionChanged += CollectionChanged;
                }

                InternalItemSource?.Dispose();
                InternalItemSource = ItemsSourceFactory.Create(this);

                if (itemsLayouter == null) return;

                needInitalizeLayouter = true;
                ReinitializeLayout();

            }
        }

        /// <summary>
        /// DataTemplate for items.
        /// Create visual contents and binding properties.
        /// return object type is restricted RecyclerViewItem.
        /// <seealso cref="Tizen.NUI.Binding.DataTemplate" />
        /// </summary>
        /// <since_tizen> 9 </since_tizen>
        public override DataTemplate ItemTemplate
        {
            get
            {
                return GetValue(ItemTemplateProperty) as DataTemplate;
            }
            set
            {
                SetValue(ItemTemplateProperty, value);
                NotifyPropertyChanged();
            }
        }
        internal override DataTemplate InternalItemTemplate
        {
            get
            {
                return itemTemplate;
            }
            set
            {
                itemTemplate = value;
                if (value == null)
                {
                    return;
                }

                needInitalizeLayouter = true;
                ReinitializeLayout();
            }
        }

        /// <summary>
        /// Items Layouter.
        /// Layouting items on the scroll ContentContainer.
        /// <seealso cref="ItemsLayouter" />
        /// <seealso cref="LinearLayouter" />
        /// <seealso cref="GridLayouter" />
        /// </summary>
        /// <since_tizen> 9 </since_tizen>
        public virtual ItemsLayouter ItemsLayouter
        {
            get
            {
                return GetValue(ItemsLayouterProperty) as ItemsLayouter;
            }
            set
            {
                SetValue(ItemsLayouterProperty, value);
                NotifyPropertyChanged();
            }
        }


        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override ItemsLayouter InternalItemsLayouter
        {
            get
            {
                return itemsLayouter;
            }
            set
            {
                itemsLayouter?.Clear();
                ClearCache();

                itemsLayouter = value;
                base.InternalItemsLayouter = itemsLayouter;
                if (value == null)
                {
                    needInitalizeLayouter = false;
                    return;
                }

                needInitalizeLayouter = true;

                var styleName = "Tizen.NUI.Components." + (itemsLayouter is LinearLayouter? "LinearLayouter" : (itemsLayouter is GridLayouter ? "GridLayouter" : "ItemsLayouter"));
                ViewStyle layouterStyle = ThemeManager.GetStyle(styleName);
                if (layouterStyle != null)
                {
                    itemsLayouter.Padding = new Extents(layouterStyle.Padding);
                }
                ReinitializeLayout();
            }
        }

        /// <summary>
        /// Scrolling direction to display items layout.
        /// </summary>
        /// <since_tizen> 9 </since_tizen>
        public new Direction ScrollingDirection
        {
            get
            {
                return (Direction)GetValue(ScrollingDirectionProperty);
            }
            set
            {
                SetValue(ScrollingDirectionProperty, value);
                NotifyPropertyChanged();
            }
        }
        private Direction InternalScrollingDirection
        {
            get
            {
                return base.ScrollingDirection;
            }
            set
            {
                if (base.ScrollingDirection != value)
                {
                    base.ScrollingDirection = value;
                    needInitalizeLayouter = true;
                    ReinitializeLayout();
                }
            }
        }

        /// <summary>
        /// Selected item in single selection.
        /// </summary>
        /// <since_tizen> 9 </since_tizen>
        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        /// <summary>
        /// Selected items list in multiple selection.
        /// </summary>
        /// <since_tizen> 9 </since_tizen>
        public IList<object> SelectedItems
        {
            get => GetValue(SelectedItemsProperty) as IList<object>;
            // set => SetValue(SelectedItemsProperty, new SelectionList(this, value));
        }

        /// <summary>
        /// Selection mode to handle items selection. See ItemSelectionMode for details.
        /// </summary>
        /// <since_tizen> 9 </since_tizen>
        public ItemSelectionMode SelectionMode
        {
            get => (ItemSelectionMode)GetValue(SelectionModeProperty);
            set => SetValue(SelectionModeProperty, value);
        }

        /// <summary>
        /// Command of selection changed.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ICommand SelectionChangedCommand
        {
            get
            {
                return GetValue(SelectionChangedCommandProperty) as ICommand;
            }
            set
            {
                SetValue(SelectionChangedCommandProperty, value);
                NotifyPropertyChanged();
            }
        }
        private ICommand InternalSelectionChangedCommand { set; get; }

        /// <summary>
        /// Command parameter of selection changed.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object SelectionChangedCommandParameter
        {
            get
            {
                return GetValue(SelectionChangedCommandParameterProperty);
            }
            set
            {
                SetValue(SelectionChangedCommandParameterProperty, value);
                NotifyPropertyChanged();
            }
        }
        private object InternalSelectionChangedCommandParameter { set; get; }

        /// <summary>
        /// Header item placed in top-most position.
        /// </summary>
        /// <remarks>Please note that, internal index will be increased by header.</remarks>
        /// <since_tizen> 9 </since_tizen>
        public RecyclerViewItem Header
        {
            get
            {
                return GetValue(HeaderProperty) as RecyclerViewItem;
            }
            set
            {
                SetValue(HeaderProperty, value);
                NotifyPropertyChanged();
            }
        }
        private RecyclerViewItem InternalHeader
        {
            get => header;
            set
            {
                if (header != null)
                {
                    //ContentContainer.Remove(header);
                    Utility.Dispose(header);
                }
                if (value != null)
                {
                    value.Index = 0;
                    value.ParentItemsView = this;
                    value.IsHeader = true;
                    ContentContainer.Add(value);
                }
                header = value;
                if (InternalItemSource != null)
                {
                    InternalItemSource.HasHeader = (value != null);
                }
                needInitalizeLayouter = true;
                ReinitializeLayout();
            }
        }

        /// <summary>
        /// Footer item placed in bottom-most position.
        /// </summary>
        /// <remarks>Please note that, internal index will be increased by footer.</remarks>
        /// <since_tizen> 9 </since_tizen>
        public RecyclerViewItem Footer
        {
            get
            {
                return GetValue(FooterProperty) as RecyclerViewItem;
            }
            set
            {
                SetValue(FooterProperty, value);
                NotifyPropertyChanged();
            }
        }
        private RecyclerViewItem InternalFooter
        {
            get => footer;
            set
            {
                if (footer != null)
                {
                    //ContentContainer.Remove(footer);
                    Utility.Dispose(footer);
                }
                if (value != null)
                {
                    value.Index = InternalItemSource?.Count ?? 0;
                    value.ParentItemsView = this;
                    value.IsFooter = true;
                    ContentContainer.Add(value);
                }
                footer = value;
                if (InternalItemSource != null)
                {
                    InternalItemSource.HasFooter = (value != null);
                }
                needInitalizeLayouter = true;
                ReinitializeLayout();
            }
        }

        /// <summary>
        /// Enable groupable view.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsGrouped
        {
            get
            {
                return (bool)GetValue(IsGroupedProperty);
            }
            set
            {
                SetValue(IsGroupedProperty, value);
                NotifyPropertyChanged();
            }
        }
        private bool InternalIsGrouped
        {
            get => isGrouped;
            set
            {
                isGrouped = value;
                needInitalizeLayouter = true;
                //Need to re-intialize Internal Item Source.
                if (InternalItemSource != null)
                {
                    InternalItemSource.Dispose();
                    InternalItemSource = null;
                }
                if (ItemsSource != null)
                {
                    InternalItemSource = ItemsSourceFactory.Create(this);
                }

                ReinitializeLayout();
            }
        }

        /// <summary>
        ///  DataTemplate of group header.
        /// </summary>
        /// <remarks>Please note that, internal index will be increased by group header.
        /// GroupHeaderTemplate is essential for groupable view.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public DataTemplate GroupHeaderTemplate
        {
            get
            {
                return GetValue(GroupHeaderTemplateProperty) as DataTemplate;
            }
            set
            {
                SetValue(GroupHeaderTemplateProperty, value);
                NotifyPropertyChanged();
            }
        }
        private DataTemplate InternalGroupHeaderTemplate
        {
            get
            {
                return groupHeaderTemplate;
            }
            set
            {
                groupHeaderTemplate = value;
                needInitalizeLayouter = true;

                //Need to re-intialize Internal Item Source.
                if (InternalItemSource != null)
                {
                    InternalItemSource.Dispose();
                    InternalItemSource = null;
                }

                if (ItemsSource != null)
                {
                    InternalItemSource = ItemsSourceFactory.Create(this);
                }

                ReinitializeLayout();
            }
        }

        /// <summary>
        /// DataTemplate of group footer. Group feature is not supported yet.
        /// </summary>
        /// <remarks>Please note that, internal index will be increased by group footer.</remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public DataTemplate GroupFooterTemplate
        {
            get
            {
                return GetValue(GroupFooterTemplateProperty) as DataTemplate;
            }
            set
            {
                SetValue(GroupFooterTemplateProperty, value);
                NotifyPropertyChanged();
            }
        }
        private DataTemplate InternalGroupFooterTemplate
        {
            get
            {
                return groupFooterTemplate;
            }
            set
            {
                groupFooterTemplate = value;
                needInitalizeLayouter = true;

                //Need to re-intialize Internal Item Source.
                if (InternalItemSource != null)
                {
                    InternalItemSource.Dispose();
                    InternalItemSource = null;
                }

                if (ItemsSource != null)
                {
                    InternalItemSource = ItemsSourceFactory.Create(this);
                }

                ReinitializeLayout();
            }
        }

        /// <summary>
        /// Internal encapsulated items data source.
        /// </summary>
        internal new IGroupableItemSource InternalItemSource
        {
            get
            {
                return (base.InternalItemSource as IGroupableItemSource);
            }
            set
            {
                base.InternalItemSource = value;
            }
        }

        /// <summary>
        /// Size strategy of measuring scroll content. see details in ItemSizingStrategy.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal ItemSizingStrategy SizingStrategy { get; set; }

        /// <inheritdoc/>
        /// <since_tizen> 9 </since_tizen>
        public override void OnRelayout(Vector2 size, RelayoutContainer container)
        {
            base.OnRelayout(size, container);

            wasRelayouted = true;
            if (needInitalizeLayouter)
            {
                ReinitializeLayout();
            }
        }

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void NotifyDataSetChanged()
        {
            if (selectedItem != null)
            {
                selectedItem = null;
            }
            if (selectedItems != null)
            {
                selectedItems.Clear();
            }

            base.NotifyDataSetChanged();
        }

        /// <summary>
        /// Update selected items list in multiple selection.
        /// </summary>
        /// <param name="newSelection">updated selection list by user</param>
        /// <since_tizen> 9 </since_tizen>
        public void UpdateSelectedItems(IList<object> newSelection)
        {
            if (SelectedItems != null)
            {
                var oldSelection = new List<object>(SelectedItems);

                suppressSelectionChangeNotification = true;

                SelectedItems.Clear();

                if (newSelection?.Count > 0)
                {
                    for (int n = 0; n < newSelection.Count; n++)
                    {
                        SelectedItems.Add(newSelection[n]);
                    }
                }

                suppressSelectionChangeNotification = false;

                SelectedItemsPropertyChanged(oldSelection, newSelection);
            }
        }

        /// <summary>
        /// Scroll to specific position with or without animation.
        /// </summary>
        /// <param name="position">Destination.</param>
        /// <param name="animate">Scroll with or without animation</param>
        /// <since_tizen> 9 </since_tizen>
        public new void ScrollTo(float position, bool animate)
        {
            if (ItemsLayouter == null)
            {
                throw new Exception("Item Layouter must exist.");
            }

            if ((InternalItemSource == null) || needInitalizeLayouter)
            {
                delayedScrollTo = true;
                delayedScrollToParam = (position, animate);
                return;
            }

            base.ScrollTo(position, animate);
        }

        /// <summary>
        /// Scrolls to the item at the specified index.
        /// </summary>
        /// <param name="index">Index of item.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new void ScrollToIndex(int index)
        {
            ScrollTo(index, true, ItemScrollTo.Start);
        }

        /// <summary>
        /// Scroll to specific item's aligned position with or without animation.
        /// </summary>
        /// <param name="index">Target item index of dataset.</param>
        /// <param name="animate">Boolean flag of animation.</param>
        /// <param name="align">Align state of item. See details in <see cref="ItemScrollTo"/>.</param>
        /// <since_tizen> 9 </since_tizen>
        public virtual void ScrollTo(int index, bool animate = false, ItemScrollTo align = ItemScrollTo.Nearest)
        {
            if (ItemsLayouter == null)
            {
                throw new Exception("Item Layouter must exist.");
            }

            if ((InternalItemSource == null) || needInitalizeLayouter)
            {
                delayedIndexScrollTo = true;
                delayedIndexScrollToParam = (index, animate, align);
                return;
            }

            if (index < 0 || index >= InternalItemSource.Count)
            {
                throw new Exception("index is out of boundary. index should be a value between (0, " + InternalItemSource.Count.ToString() + ").");
            }

            float scrollPos, curPos, curSize, curItemSize;
            (float x, float y) = ItemsLayouter.GetItemPosition(index);
            (float width, float height) = ItemsLayouter.GetItemSize(index);

            if (ScrollingDirection == Direction.Horizontal)
            {
                scrollPos = x;
                curPos = ScrollPosition.X;
                curSize = Size.Width;
                curItemSize = width;
            }
            else
            {
                scrollPos = y;
                curPos = ScrollPosition.Y;
                curSize = Size.Height;
                curItemSize = height;
            }

            //Console.WriteLine("[NUI] ScrollTo [{0}:{1}], curPos{2}, itemPos{3}, curSize{4}, itemSize{5}", InternalItemSource.GetPosition(item), align, curPos, scrollPos, curSize, curItemSize);
            switch (align)
            {
                case ItemScrollTo.Start:
                    //nothing necessary.
                    break;
                case ItemScrollTo.Center:
                    scrollPos = scrollPos - (curSize / 2) + (curItemSize / 2);
                    break;
                case ItemScrollTo.End:
                    scrollPos = scrollPos - curSize + curItemSize;
                    break;
                case ItemScrollTo.Nearest:
                    if (scrollPos < curPos - curItemSize)
                    {
                        // item is placed before the current screen. scrollTo.Top
                    }
                    else if (scrollPos >= curPos + curSize + curItemSize)
                    {
                        // item is placed after the current screen. scrollTo.End
                        scrollPos = scrollPos - curSize + curItemSize;
                    }
                    else
                    {
                        // item is in the scroller. ScrollTo() is ignored.
                        return;
                    }
                    break;
            }

            //Console.WriteLine("[NUI] ScrollTo [{0}]-------------------", scrollPos);
            base.ScrollTo(scrollPos, animate);
        }

        /// <summary>
        /// Apply style to CollectionView
        /// </summary>
        /// <param name="viewStyle">The style to apply.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void ApplyStyle(ViewStyle viewStyle)
        {
            base.ApplyStyle(viewStyle);
            if (viewStyle != null)
            {
                //Extension = RecyclerViewItemStyle.CreateExtension();
            }
            if (itemsLayouter != null)
            {
                string styleName = "Tizen.NUI.Compoenents." + (itemsLayouter is LinearLayouter? "LinearLayouter" : (itemsLayouter is GridLayouter ? "GridLayouter" : "ItemsLayouter"));
                ViewStyle layouterStyle = ThemeManager.GetStyle(styleName);
                if (layouterStyle != null)
                {
                    itemsLayouter.Padding = new Extents(layouterStyle.Padding);
                }
            }
        }

        /// <summary>
        /// Initialize AT-SPI object.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void OnInitialize()
        {
            base.OnInitialize();
            AccessibilityRole = Role.List;
        }

        /// <summary>
        /// Scroll to specified item
        /// </summary>
        /// <remarks>
        /// Make sure that the item that is about to receive the accessibility highlight is visible.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override bool AccessibilityScrollToChild(View child)
        {
            if (ScrollingDirection == Direction.Horizontal)
            {
                if (child.ScreenPosition.X + child.Size.Width <= this.ScreenPosition.X)
                {
                    ScrollTo((float)(child.ScreenPosition.X - ContentContainer.ScreenPosition.X), false);
                }
                else if (child.ScreenPosition.X >= this.ScreenPosition.X + this.Size.Width)
                {
                    ScrollTo((float)(child.ScreenPosition.X + child.Size.Width - ContentContainer.ScreenPosition.X - this.Size.Width), false);
                }
            }
            else
            {
                if (child.ScreenPosition.Y + child.Size.Height <= this.ScreenPosition.Y)
                {
                    ScrollTo((float)(child.ScreenPosition.Y - ContentContainer.ScreenPosition.Y), false);
                }
                else if (child.ScreenPosition.Y >= this.ScreenPosition.Y + this.Size.Height)
                {
                    ScrollTo((float)(child.ScreenPosition.Y + child.Size.Height - ContentContainer.ScreenPosition.Y - this.Size.Height), false);
                }
            }
            return true;
        }

        // Realize and Decorate the item.

        internal override RecyclerViewItem RealizeItem(int index)
        {
            RecyclerViewItem item;
            if (index == 0 && Header != null)
            {
                Header.Show();
                return Header;
            }

            if (index == InternalItemSource.Count - 1 && Footer != null)
            {
                Footer.Show();
                return Footer;
            }

            if (isGrouped)
            {
                var context = InternalItemSource.GetItem(index);
                if (InternalItemSource.IsGroupHeader(index))
                {
                    item = RealizeGroupHeader(index, context);
                }
                else if (InternalItemSource.IsGroupFooter(index))
                {

                    //group selection?
                    item = RealizeGroupFooter(index, context);
                }
                else
                {
                    item = base.RealizeItem(index);
                    if (item == null)
                    {
                        throw new Exception("Item realize failed by Null content return.");
                    }
                    item.ParentGroup = InternalItemSource.GetGroupParent(index);
                }
            }
            else
            {
                item = base.RealizeItem(index);
            }

            if (item == null)
                throw new Exception("Item realize failed by Null content return.");

            switch (SelectionMode)
            {
                case ItemSelectionMode.Single:
                case ItemSelectionMode.SingleAlways:
                    if (item.BindingContext != null && item.BindingContext == SelectedItem)
                    {
                        item.IsSelected = true;
                    }
                    break;

                case ItemSelectionMode.Multiple:
                    if ((item.BindingContext != null) && (SelectedItems?.Contains(item.BindingContext) ?? false))
                    {
                        item.IsSelected = true;
                    }
                    break;
                case ItemSelectionMode.None:
                    item.IsSelectable = false;
                    break;
            }
            return item;
        }

        // Unrealize and caching the item.
        internal override void UnrealizeItem(RecyclerViewItem item, bool recycle = true)
        {
            if (item == null)
            {
                return;
            }

            if (item == Header)
            {
                item?.Hide();
                return;
            }
            if (item == Footer)
            {
                item.Hide();
                return;
            }

            if (item.isGroupHeader || item.isGroupFooter)
            {
                item.Index = -1;
                item.ParentItemsView = null;
                item.BindingContext = null;
                item.IsPressed = false;
                item.IsSelected = false;
                item.IsEnabled = true;
                item.UpdateState();
                //item.Relayout -= OnItemRelayout;
                if (!recycle || !PushRecycleGroupCache(item))
                {
                    Utility.Dispose(item);
                }
                return;
            }

            base.UnrealizeItem(item, recycle);
        }

        internal void SelectedItemsPropertyChanged(IList<object> oldSelection, IList<object> newSelection)
        {
            if (suppressSelectionChangeNotification)
            {
                return;
            }

            foreach (RecyclerViewItem item in ContentContainer.Children.Where((item) => item is RecyclerViewItem))
            {
                if (item.BindingContext == null) continue;
                if (newSelection.Contains(item.BindingContext))
                {
                    if (!item.IsSelected)
                    {
                        item.IsSelected = true;
                    }
                }
                else
                {
                    if (item.IsSelected)
                    {
                        item.IsSelected = false;
                    }
                }
            }
            SelectionPropertyChanged(this, new SelectionChangedEventArgs(oldSelection, newSelection));

            OnPropertyChanged(SelectedItemsProperty.PropertyName);
        }

        /// <summary>
        /// Internal selection callback.
        /// </summary>
        /// <since_tizen> 9 </since_tizen>
        protected virtual void OnSelectionChanged(SelectionChangedEventArgs args)
        {
            //Selection Callback
        }

        /// <summary>
        /// Adjust scrolling position by own scrolling rules.
        /// Override this function when developer wants to change destination of flicking.(e.g. always snap to center of item)
        /// </summary>
        /// <param name="position">Scroll position which is calculated by ScrollableBase</param>
        /// <returns>Adjusted scroll destination</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override float AdjustTargetPositionOfScrollAnimation(float position)
        {
            // Destination is depending on implementation of layout manager.
            // Get destination from layout manager.
            return ItemsLayouter?.CalculateCandidateScrollPosition(position) ?? position;
        }

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override void ClearCache()
        {
            foreach (RecyclerViewItem item in recycleGroupHeaderCache)
            {
                Utility.Dispose(item);
            }
            recycleGroupHeaderCache.Clear();
            foreach (RecyclerViewItem item in recycleGroupFooterCache)
            {
                Utility.Dispose(item);
            }
            recycleGroupFooterCache.Clear();
            base.ClearCache();
        }


        /// <summary>
        /// OnScroll event callback. Requesting layout to the layouter with given scrollPosition.
        /// </summary>
        /// <param name="source">Scroll source object</param>
        /// <param name="args">Scroll event argument</param>
        /// <since_tizen> 9 </since_tizen>
        protected override void OnScrolling(object source, ScrollEventArgs args)
        {
            if (disposed) return;

            if (needInitalizeLayouter && (ItemsLayouter != null))
            {
                ItemsLayouter.Initialize(this);
                needInitalizeLayouter = false;
            }

            base.OnScrolling(source, args);
        }

        /// <summary>
        /// Dispose ItemsView and all children on it.
        /// </summary>
        /// <param name="type">Dispose type.</param>
        /// <since_tizen> 9 </since_tizen>
        protected override void Dispose(DisposeTypes type)
        {
            if (disposed)
            {
                return;
            }

            if (type == DisposeTypes.Explicit)
            {
                // From now on, no need to use this properties,
                // so remove reference, to push it into garbage collector.

                // Arugable to disposing user-created members.
                /*
                if (Header != null)
                {
                    Utility.Dispose(Header);
                    Header = null;
                }
                if (Footer != null)
                {
                    Utility.Dispose(Footer);
                    Footer = null;
                }
                */

                groupHeaderTemplate = null;
                groupFooterTemplate = null;

                if (selectedItem != null)
                {
                    selectedItem = null;
                }
                if (selectedItems != null)
                {
                    selectedItems.Clear();
                    selectedItems = null;
                }
                if (InternalItemSource != null)
                {
                    InternalItemSource.Dispose();
                    InternalItemSource = null;
                }
            }

            base.Dispose(type);
        }

        private static void SelectionPropertyChanged(CollectionView colView, SelectionChangedEventArgs args)
        {
            var command = colView.SelectionChangedCommand;

            if (command != null)
            {
                var commandParameter = colView.SelectionChangedCommandParameter;

                if (command.CanExecute(commandParameter))
                {
                    command.Execute(commandParameter);
                }
            }
            colView.SelectionChanged?.Invoke(colView, args);
            colView.OnSelectionChanged(args);
        }

        private static object CoerceSelectedItems(CollectionView colView, object value)
        {
            if (value == null)
            {
                return new SelectionList(colView);
            }

            if (value is SelectionList)
            {
                return value;
            }

            return new SelectionList(colView, value as IList<object>);
        }

        private static void SelectionModePropertyChanged(CollectionView colView, object oldValue, object newValue)
        {
            var oldMode = (ItemSelectionMode)oldValue;
            var newMode = (ItemSelectionMode)newValue;

            IList<object> previousSelection = new List<object>();
            IList<object> newSelection = new List<object>();

            switch (oldMode)
            {
                case ItemSelectionMode.None:
                    break;
                case ItemSelectionMode.Single:
                    if (colView.SelectedItem != null)
                    {
                        previousSelection.Add(colView.SelectedItem);
                    }
                    break;
                case ItemSelectionMode.Multiple:
                    previousSelection = colView.SelectedItems;
                    break;
            }

            switch (newMode)
            {
                case ItemSelectionMode.None:
                    break;
                case ItemSelectionMode.Single:
                    if (colView.SelectedItem != null)
                    {
                        newSelection.Add(colView.SelectedItem);
                    }
                    break;
                case ItemSelectionMode.Multiple:
                    newSelection = colView.SelectedItems;
                    break;
            }

            if (previousSelection.Count == newSelection.Count)
            {
                if (previousSelection.Count == 0 || (previousSelection[0] == newSelection[0]))
                {
                    // Both selections are empty or have the same single item; no reason to signal a change
                    return;
                }
            }

            var args = new SelectionChangedEventArgs(previousSelection, newSelection);
            SelectionPropertyChanged(colView, args);
        }

        private void ReinitializeLayout()
        {
            if (ItemsSource == null || ItemsLayouter == null || ItemTemplate == null)
            {
                return;
            }

            if (disposed)
            {
                return;
            }

            if (!wasRelayouted)
            {
                return;
            }

            if (needInitalizeLayouter)
            {
                if (InternalItemSource == null)
                {
                    return;
                }

                InternalItemSource.HasHeader = (header != null);
                InternalItemSource.HasFooter = (footer != null);

                itemsLayouter.Clear();
                ClearCache();

                ItemsLayouter.Initialize(this);
                needInitalizeLayouter = false;
            }

            ItemsLayouter.RequestLayout(0.0f, true);

            if (delayedScrollTo)
            {
                delayedScrollTo = false;
                ScrollTo(delayedScrollToParam.position, delayedScrollToParam.anim);
            }

            if (delayedIndexScrollTo)
            {
                delayedIndexScrollTo = false;
                ScrollTo(delayedIndexScrollToParam.index, delayedIndexScrollToParam.anim, delayedIndexScrollToParam.scrollTo);
            }

            if (ScrollingDirection == Direction.Horizontal)
            {
                ContentContainer.SizeWidth = (float)ItemsLayouter?.CalculateLayoutOrientationSize();
            }
            else
            {
                ContentContainer.SizeHeight = (float)ItemsLayouter?.CalculateLayoutOrientationSize();
            }
        }

        private bool PushRecycleGroupCache(RecyclerViewItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if  (item.Template == null || RecycleCache.Count >= 20)
            {
                return false;
            }

            if (item.isGroupHeader)
            {
                recycleGroupHeaderCache.Add(item);
            }
            else if (item.isGroupFooter)
            {
                recycleGroupFooterCache.Add(item);
            }
            else
            {
                return false;
            }

            item.Hide();
            item.Index = -1;

            return true;
        }

        private RecyclerViewItem PopRecycleGroupCache(DataTemplate Template, bool isHeader)
        {
            RecyclerViewItem viewItem = null;

            var Cache = (isHeader ? recycleGroupHeaderCache : recycleGroupFooterCache);
            for (int i = 0; i < Cache.Count; i++)
            {
                viewItem = Cache[i];
                if (Template == viewItem.Template)
                {
                    break;
                }
            }

            if (viewItem != null)
            {
                Cache.Remove(viewItem);
                viewItem.Show();
            }

            return viewItem;
        }

        private RecyclerViewItem RealizeGroupHeader(int index, object context)
        {
            DataTemplate templ = (groupHeaderTemplate as DataTemplateSelector)?.SelectDataTemplate(context, this) ?? groupHeaderTemplate;

            RecyclerViewItem groupHeader = PopRecycleGroupCache(templ, true);

            if (groupHeader == null)
            {
                groupHeader = DataTemplateExtensions.CreateContent(groupHeaderTemplate, context, this) as RecyclerViewItem;
                if (groupHeader == null)
                {
                    return null;
                }

                groupHeader.Template = templ;
                groupHeader.isGroupHeader = true;
                groupHeader.isGroupFooter = false;
                ContentContainer.Add(groupHeader);
            }

            if (groupHeader != null)
            {
                groupHeader.ParentItemsView = this;
                groupHeader.Index = index;
                groupHeader.ParentGroup = context;
                groupHeader.BindingContext = context;

                return groupHeader;
            }

            return null;
        }

        private RecyclerViewItem RealizeGroupFooter(int index, object context)
        {
            DataTemplate templ = (groupFooterTemplate as DataTemplateSelector)?.SelectDataTemplate(context, this) ?? groupFooterTemplate;

            RecyclerViewItem groupFooter = PopRecycleGroupCache(templ, false);

            if (groupFooter == null)
            {
                groupFooter = DataTemplateExtensions.CreateContent(groupFooterTemplate, context, this) as RecyclerViewItem;
                if (groupFooter == null)
                {
                    return null;
                }

                groupFooter.Template = templ;
                groupFooter.isGroupHeader = false;
                groupFooter.isGroupFooter = true;
                ContentContainer.Add(groupFooter);
            }

            if (groupFooter != null)
            {
                groupFooter.ParentItemsView = this;
                groupFooter.Index = index;
                groupFooter.ParentGroup = context;
                groupFooter.BindingContext = context;
                return groupFooter;
            }

            return null;
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    // Clear removed items.
                    if (args.OldItems != null)
                    {
                        if (args.OldItems.Contains(selectedItem))
                        {
                            selectedItem = null;
                        }

                        if (selectedItems != null)
                        {
                            foreach (object removed in args.OldItems)
                            {
                                if (selectedItems.Contains(removed))
                                {
                                    selectedItems.Remove(removed);
                                }
                            }
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(args));
            }
        }

    }
}
