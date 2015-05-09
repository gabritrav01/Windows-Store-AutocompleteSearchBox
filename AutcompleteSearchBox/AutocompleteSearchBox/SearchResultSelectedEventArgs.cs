using Windows.UI.Xaml;

namespace AutocompleteSearchBox
{
    public class SearchResultSelectedEventArgs : RoutedEventArgs
    {
        public SearchResultSelectedEventArgs(object selectedItem)
        {
            SelectedItem = selectedItem;
        }

        public object SelectedItem { get; private set; }
    }
}