// -----------------------------------------------------------------------
// <copyright file="MainPage.xaml.cs" company="Conglomo">
// Copyright 2014-2021 Conglomo Limited. Please see LICENCE.md for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace Conglomo.AudioBible
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// <remarks>
    /// The Blank Page item template is documented at <c>http://go.microsoft.com/fwlink/?LinkId=234238</c>.
    /// </remarks>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// The section index.
        /// </summary>
        private static int sectionIndex;

        /// <summary>
        /// Initialises a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            // Initialise the component
            this.InitializeComponent();

            // Set the selected section index
            this.Loaded += delegate { this.MainHub.ScrollToSection(this.MainHub.Sections[sectionIndex]); };
        }

        /// <summary>
        /// Handles the Click event of the AppBarButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Windows.UI.Xaml.RoutedEventArgs"/> instance containing the event data.</param>
        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            AppBarButton button = e.OriginalSource as AppBarButton;
            if (button.Label == "Previous")
            {
                this.MainPlayer.Previous();
            }
            else if (button.Label == "Play")
            {
                this.MainPlayer.Play();
                this.Play.Icon = new SymbolIcon(Symbol.Pause);
                this.Play.Label = "Pause";
            }
            else if (button.Label == "Pause")
            {
                this.MainPlayer.Pause();
                this.Play.Icon = new SymbolIcon(Symbol.Play);
                this.Play.Label = "Play";
            }
            else if (button.Label == "Next")
            {
                this.MainPlayer.Next();
            }
        }

        /// <summary>
        /// Handles the Click event of the Back button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void Back_Click(object sender, RoutedEventArgs e)
        {
            // Show the list of books
            this.MainZoom.IsZoomedInViewActive = false;

            // This delay is relevant for the success of the scrolling
            await Task.Delay(200);

            // Scroll to the appropriate testament
            this.MainHub.ScrollToSection(this.MainHub.Sections[sectionIndex]);
        }

        /// <summary>
        /// Handles the ItemClick event of the Chapter GridView.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemClickEventArgs"/> instance containing the event data.</param>
        private void GridViewChapter_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Get the item index, this is used to determine which book to select
            GridView gridView = sender as GridView;
            int index = gridView.Items.IndexOf(gridView.Items.First(i => (string)i == (string)e.ClickedItem));

            // Remember the section index
            int bookIndex = Convert.ToInt32(gridView.Tag, CultureInfo.CurrentCulture);

            // Start playing this chapter
            this.MainPlayer.PlayFile(bookIndex, index);

            // Show the the file is playing
            this.Play.Icon = new SymbolIcon(Symbol.Pause);
            this.Play.Label = "Pause";
        }

        /// <summary>
        /// Handles the ItemClick event of the GridView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemClickEventArgs"/> instance containing the event data.</param>
        private async void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Get the item index, this is used to determine which book to select
            GridView gridView = sender as GridView;
            int index = gridView.Items.IndexOf(gridView.Items.First(i => (string)i == (string)e.ClickedItem));

            // Remember the section index
            sectionIndex = Convert.ToInt32(gridView.Tag, CultureInfo.CurrentCulture);

            // If we are in the New Testament, add the number of Old Testament books
            if (sectionIndex == 1)
            {
                index += 39;
            }

            // If this is a one chapter book, start playing the MP3
            if (index == 30 || index == 56 || index == 62 || index == 63 || index == 64)
            {
                // Start playing this book
                this.MainPlayer.PlayFile(index, 0);

                // Show the the file is playing
                this.Play.Icon = new SymbolIcon(Symbol.Pause);
                this.Play.Label = "Pause";
            }
            else
            {
                // Show the chapters
                this.MainZoom.IsZoomedInViewActive = true;

                // This delay is relevant for the success of the scrolling
                await Task.Delay(200);

                // Navigate to the appropriate book
                this.SecondaryHub.ScrollToSection(this.SecondaryHub.Sections[index]);
            }
        }
    }
}
