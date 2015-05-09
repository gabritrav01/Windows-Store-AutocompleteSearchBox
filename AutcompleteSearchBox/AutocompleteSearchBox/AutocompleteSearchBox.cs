using System;
using System.Collections;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Search.Core;
using Windows.Foundation;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace AutocompleteSearchBox
{
    public delegate void SearchResultSelectedEventHandler(object sender, SearchResultSelectedEventArgs e);

    public delegate void QueryChangedEventHandler(object sender, SearchBoxQueryChangedEventArgs e);

    [TemplatePart(Name = "PART_SearchBox", Type = typeof (SearchBox)),
     TemplatePart(Name = "PART_ResultsListBox", Type = typeof (ListBox)),
     TemplatePart(Name = "PART_PopupCanvas", Type = typeof(Canvas))]
    public class AutocompleteSearchBox : Control
    {
        #region Constructor

        public AutocompleteSearchBox()
        {
            DefaultStyleKey = typeof (AutocompleteSearchBox);
        }

        #endregion

        #region Events

        public event SearchResultSelectedEventHandler SearchResultSelected;
        public event QueryChangedEventHandler QueryChanged;

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof (IEnumerable), typeof (AutocompleteSearchBox),
                new PropertyMetadata(null));

        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.Register("Filter", typeof (Func<object, string, bool>), typeof (AutocompleteSearchBox),
                new PropertyMetadata(new Func<object, string, bool>(DefaultFilter)));

        private static bool DefaultFilter(object item, string queryString)
        {
            return item != null && queryString != null &&
                   item.ToString().Trim().ToUpperInvariant().Contains(queryString.ToUpperInvariant().Trim());
        }

        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register("ItemTemplate", typeof (DataTemplate), typeof (AutocompleteSearchBox),
                new PropertyMetadata(null));

        public static readonly DependencyProperty QueryTextProperty =
            DependencyProperty.Register("QueryText", typeof (string), typeof (AutocompleteSearchBox),
                new PropertyMetadata(string.Empty));
        
        #endregion

        #region Private Fields

        private ListBox _resultsListBox;
        private SearchBox _searchBox;
        private Canvas _popupCanvas;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the function used to filter each item in the ItemsSource given the provided 
        /// query text that should provide a value indicating wether the items should be displayed
        /// in the results list. The default value filters the items based on their .ToString() representation
        /// </summary>
        public Func<object, string, bool> Filter
        {
            get { return (Func<object, string, bool>) GetValue(FilterProperty); }
            set { SetValue(FilterProperty, value); }
        }

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable) GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate) GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public string QueryText
        {
            get { return (string) GetValue(QueryTextProperty); }
            set { SetValue(QueryTextProperty, value); }
        }

        #endregion

        #region Protected Methods

        protected virtual void OnSearchResultSelected(SearchResultSelectedEventArgs e)
        {
            var handler = SearchResultSelected;
            if (handler != null)
                handler(this, e);
        }

        protected virtual void OnQueryChanged(SearchBoxQueryChangedEventArgs e)
        {
            var handler = QueryChanged;
            if (handler != null)
                handler.Invoke(this, e);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _searchBox = GetTemplateChild("PART_SearchBox") as SearchBox;
            _resultsListBox = GetTemplateChild("PART_ResultsListBox") as ListBox;
            _popupCanvas = GetTemplateChild("PART_PopupCanvas") as Canvas;
            CheckTemplateParts();

            if (DesignMode.DesignModeEnabled)
                return;

            // ReSharper disable once PossibleNullReferenceException
            _searchBox.QueryChanged += SearchBox_QueryChanged;
            _searchBox.KeyUp += SearchBox_KeyUp;
            _searchBox.LostFocus += SearchBox_LostFocus;
            _searchBox.SizeChanged += SearchBox_SizeChanged;
            _searchBox.QuerySubmitted += SearchBox_QuerySubmitted;

            ClearSearchHistory();

            // ReSharper disable once PossibleNullReferenceException
            _resultsListBox.KeyUp += ResultsListBox_KeyUp;
            _resultsListBox.SelectionChanged += ResultsListBox_SelectionChanged;
            _resultsListBox.ItemsSource = ItemsSource;
        }

        #endregion

        #region Private Methods

        private void SearchBox_QuerySubmitted(SearchBox sender, SearchBoxQuerySubmittedEventArgs args)
        {
            if (_resultsListBox.Items != null && _resultsListBox.Items.Count > 0)
            {
                _resultsListBox.Visibility = Visibility.Visible;
                ClearSearchHistory();
            }
        }

        private void ResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            var focusedElement = FocusManager.GetFocusedElement() as ListBoxItem;
            if (focusedElement != null && focusedElement.FocusState == FocusState.Pointer)
            {
                AnounceSelectedItemAndCloseList();
            }
        }

        private void SearchBox_SizeChanged(object sender, SizeChangedEventArgs args)
        {
            _resultsListBox.Width = args.NewSize.Width;
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs args)
        {
            if (CheckListBoxAndItemsFocusState(FocusState.Unfocused))
            {
                _resultsListBox.Visibility = Visibility.Collapsed;
            }
        }

        private void ResultsListBox_KeyUp(object sender, KeyRoutedEventArgs args)
        {
            if (args.Key == VirtualKey.Enter && _resultsListBox.SelectedItem != null)
            {
                AnounceSelectedItemAndCloseList();
            }
            else if (args.Key == VirtualKey.Escape)
            {
                CloseResultsAndFocusSearchBox();
            }
        }

        private void SearchBox_KeyUp(object sender, KeyRoutedEventArgs args)
        {
            if (args.Key == VirtualKey.Down)
            {
                SelectAndOpenResults();
            }
            else if (args.Key == VirtualKey.Escape)
            {
                CloseResultsAndFocusSearchBox();
            }
        }

        private void SearchBox_QueryChanged(object sender, SearchBoxQueryChangedEventArgs args)
        {
            OnQueryChanged(args);

            var text = args.QueryText;
            if (string.IsNullOrEmpty(text))
            {
                _resultsListBox.Visibility = Visibility.Collapsed;
                return;
            }

            var itemsSource = (ItemsSource ?? Enumerable.Empty<object>()).Cast<object>();
            var filteredItems = from item in itemsSource
                where Filter(item, text)
                select item;

            var filteredItemsList = filteredItems.ToList();
            _resultsListBox.ItemsSource = filteredItemsList;
            _resultsListBox.Visibility = filteredItemsList.Count > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private static void ClearSearchHistory()
        {
            var manager = new SearchSuggestionManager();
            manager.SearchHistoryEnabled = false;
            manager.ClearHistory();
        }

        private void CloseResultsAndFocusSearchBox()
        {
            _resultsListBox.SelectedItem = null;
            _resultsListBox.Visibility = Visibility.Collapsed;
            _searchBox.Focus(FocusState.Keyboard);
        }

        private void SelectAndOpenResults()
        {
            _resultsListBox.SelectedItem = ItemsSource.Cast<object>().FirstOrDefault();
            _resultsListBox.Focus(FocusState.Keyboard);
        }

        private bool CheckListBoxAndItemsFocusState(FocusState focusState)
        {
            var listBoxIsUnfocused = _resultsListBox.FocusState == focusState;
            var allItemsAreUnfocused = _resultsListBox.Items
                .Select(o => _resultsListBox.ItemContainerGenerator.ContainerFromItem(o))
                .Cast<ListBoxItem>()
                .Where(x => x != null)
                .All(i => i.FocusState == focusState);

            return allItemsAreUnfocused && listBoxIsUnfocused;
        }

        private void AnounceSelectedItemAndCloseList()
        {
            var selectedItem = _resultsListBox.SelectedItem;
            if (selectedItem == null)
                return;

            var eventArgs = new SearchResultSelectedEventArgs(selectedItem);

            OnSearchResultSelected(eventArgs);

            _resultsListBox.Visibility = Visibility.Collapsed;
            _resultsListBox.SelectedItem = null;
            _searchBox.Focus(FocusState.Keyboard);
            _searchBox.QueryText = string.Empty;
        }

        private void CheckTemplateParts()
        {
            if (_searchBox == null)
                throw new InvalidOperationException("PART_SeachBox is required");

            if (_resultsListBox == null)
                throw new InvalidOperationException("PART_SeachBox is required");

            if (_popupCanvas == null)
                throw new InvalidOperationException("PART_PopupCanvas is required");
        }

        #endregion
    }
}