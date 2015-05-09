using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.UI.Xaml.Controls;
using AutocompleteSearchBox;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SampleApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();

            Persons = GetPersonData();
            SearchBox.ItemsSource = Persons;

            // Define a custom filter used to get results
            SearchBox.Filter = (item, searchText) =>
            {
                var person = ((Person)item);
                return person.Name.ToUpperInvariant().Contains(searchText.ToUpperInvariant())
                       || person.DateOfBirth.ToString("d").Contains(searchText)
                       || person.Occupation.ToUpperInvariant().Contains(searchText.ToUpperInvariant());
            };
        }

        public ObservableCollection<Person> Persons { get; private set; }

        private static ObservableCollection<Person> GetPersonData()
        {
            return new ObservableCollection<Person>(new[]
            {
                new Person()
                {
                    DateOfBirth = new DateTime(1989, 1, 1),
                    Name = "George",
                    Occupation = "Software Engineer"
                },

                new Person()
                {
                    DateOfBirth = new DateTime(1978, 1, 1),
                    Name = "Bob",
                    Occupation = "Software Tester"
                },

                new Person()
                {
                    DateOfBirth = new DateTime(2000, 1, 1),
                    Name = "Alex",
                    Occupation = "Car Tester"
                },
            });
        }

        private void SearchBox_SearchResultSelected(object sender, SearchResultSelectedEventArgs e)
        {
            SelectedItemTextBlock.Text = ((Person) e.SelectedItem).Name;

            Debug.WriteLine("Selected item:" + SelectedItemTextBlock.Text);
        }

        private void SearchBox_QueryChanged(object sender, SearchBoxQueryChangedEventArgs args)
        {
            Debug.WriteLine("QueryText: " + args.QueryText);
        }
    }

    public class Person
    {
        public string Name { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Occupation { get; set; }
    }
}