// -----------------------------------------------------------------------
// <copyright file="MainPage.xaml.cs" company="Conglomo">
// Copyright 2014-2021 Conglomo Limited. Please see LICENCE.md for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace Conglomo.AudioBible
{
    using System;
    using System.Linq;
    using Windows.Foundation.Collections;
    using Windows.Media.Playback;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// <remarks>
    /// The Blank Page item template is documented at <c>http://go.microsoft.com/fwlink/?LinkId=234238</c>.
    /// </remarks>
    public sealed partial class MainPage : Page
    {
        /// <summary>
        /// The pivot index.
        /// </summary>
        private static int pivotIndex;

        /// <summary>
        /// Initialises a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            // Initialise the component
            this.InitializeComponent();

            // Set the selected pivot index
            this.Loaded += delegate { this.MainPivot.SelectedIndex = pivotIndex; };

            // Start the background task
            this.StartBackgroundTask();

            // Receive play state change events
            BackgroundMediaPlayer.Current.CurrentStateChanged += this.BackgroundMediaPlayer_CurrentStateChanged;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Call the base method
            base.OnNavigatedTo(e);

            // Make sure the play/pause button is correct
            if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Buffering
                || BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Opening
                || BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Playing)
            {
                this.Play.Icon = new SymbolIcon(Symbol.Pause);
                this.Play.Label = "pause";
            }
        }

        /// <summary>
        /// Starts the background task.
        /// </summary>
        private async void StartBackgroundTask()
        {
            // Initialise the background audio player
            await this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                    {
                        ValueSet message = new ValueSet();
                        message.Add("START", "0");
                        BackgroundMediaPlayer.SendMessageToBackground(message);
                    });
        }

        /// <summary>
        /// Handles the Click event of the AppBarButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Windows.UI.Xaml.RoutedEventArgs"/> instance containing the event data.</param>
        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            await Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    AppBarButton button = e.OriginalSource as AppBarButton;
                    if (button.Label == "previous")
                    {
                        // Notify the background task to move previous
                        ValueSet messageDictionary = new ValueSet();
                        messageDictionary.Add("PREVIOUS", "0");
                        BackgroundMediaPlayer.SendMessageToBackground(messageDictionary);

                        // Update the play icon
                        this.Play.Icon = new SymbolIcon(Symbol.Pause);
                        this.Play.Label = "pause";
                    }
                    else if (button.Label == "play")
                    {
                        // If not media open, open this book
                        if (BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Closed
                            || BackgroundMediaPlayer.Current.CurrentState == MediaPlayerState.Stopped)
                        {
                            // Get the book index
                            int bookIndex = 0;
                            if (this.MainPivot.SelectedIndex == 1)
                            {
                                bookIndex += 39;
                            }

                            // Play the file
                            Utilities.PlayFile(bookIndex, 0);
                        }
                        else
                        {
                            BackgroundMediaPlayer.Current.Play();
                        }

                        this.Play.Icon = new SymbolIcon(Symbol.Pause);
                        this.Play.Label = "pause";
                    }
                    else if (button.Label == "pause")
                    {
                        BackgroundMediaPlayer.Current.Pause();
                        this.Play.Icon = new SymbolIcon(Symbol.Play);
                        this.Play.Label = "play";
                    }
                    else if (button.Label == "next")
                    {
                        // Notify the background task to move next
                        ValueSet messageDictionary = new ValueSet();
                        messageDictionary.Add("NEXT", "0");
                        BackgroundMediaPlayer.SendMessageToBackground(messageDictionary);

                        // Update the play icon
                        this.Play.Icon = new SymbolIcon(Symbol.Pause);
                        this.Play.Label = "pause";
                    }
                });
        }

        /// <summary>
        /// Handles the CurrentStateChanged event of the MusicPlayer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void BackgroundMediaPlayer_CurrentStateChanged(MediaPlayer sender, object e)
        {
            if (sender.CurrentState == MediaPlayerState.Playing)
            {
                await this.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () =>
                    {
                        this.Play.Icon = new SymbolIcon(Symbol.Pause);
                        this.Play.Label = "pause";
                    });
            }
            else if (sender.CurrentState == MediaPlayerState.Paused
                || sender.CurrentState == MediaPlayerState.Stopped
                || sender.CurrentState == MediaPlayerState.Closed)
            {
                await this.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () =>
                    {
                        this.Play.Icon = new SymbolIcon(Symbol.Play);
                        this.Play.Label = "play";
                    });
            }
        }

        /// <summary>
        /// Handles the ItemClick event of the ListView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemClickEventArgs"/> instance containing the event data.</param>
        private async void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            await Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    // Remember the pivot index
                    pivotIndex = this.MainPivot.SelectedIndex;

                    // Get the item index, this is used to determine which book to select
                    ListView listView = sender as ListView;
                    int index = listView.Items.IndexOf(listView.Items.First(i => (string)i == (string)e.ClickedItem));

                    // If we are in the New Testament, add the number of Old Testament books
                    if (this.MainPivot.SelectedIndex == 1)
                    {
                        index += 39;
                    }

                    // If this is a one chapter book, start playing the MP3
                    if (index == 30 || index == 56 || index == 62 || index == 63 || index == 64)
                    {
                        // Start playing this book
                        Utilities.PlayFile(index, 0);

                        // Show the the file is playing
                        this.Play.Icon = new SymbolIcon(Symbol.Pause);
                        this.Play.Label = "pause";
                    }
                    else
                    {
                        // Navigate to the appropriate book
                        this.Frame.Navigate(typeof(SelectFile), index);
                    }
                });
        }
    }
}
