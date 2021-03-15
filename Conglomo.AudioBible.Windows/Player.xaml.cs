// -----------------------------------------------------------------------
// <copyright file="Player.xaml.cs" company="Conglomo">
// Copyright 2014-2021 Conglomo Limited. Please see LICENCE.md for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace Conglomo.AudioBible
{
    using System;
    using System.Linq;
    using Data;
    using Windows.ApplicationModel;
    using Windows.Media;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;

    /// <summary>
    /// The player control.
    /// </summary>
    /// <remarks>
    /// The User Control item template is documented at <c>http://go.microsoft.com/fwlink/?LinkId=234236</c>.
    /// </remarks>
    public sealed partial class Player : UserControl
    {
        /// <summary>
        /// The system media controls.
        /// </summary>
        private SystemMediaTransportControls systemControls;

        /// <summary>
        /// The current file index.
        /// </summary>
        private int currentFileIndex;

        /// <summary>
        /// Initialises a new instance of the <see cref="Player"/> class.
        /// </summary>
        public Player()
        {
            // Initialise the component
            this.InitializeComponent();

            // Set up the transport controls, if we are not in design mode
            if (!DesignMode.DesignModeEnabled)
            {
                this.InitialiseTransportControls();
            }
        }

        /// <summary>
        /// Skips to the next track.
        /// </summary>
        public async void Next()
        {
            await Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    // Get the next file index
                    this.currentFileIndex = Bible.GetNextFileIndex(this.currentFileIndex);

                    // Play the file
                    this.Media.Source = new Uri(Bible.Files.ElementAt(this.currentFileIndex), UriKind.Absolute);
                    this.Media.Play();
                });
        }

        /// <summary>
        /// Pauses the music playing.
        /// </summary>
        public async void Pause()
        {
            await Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    this.Media.Pause();
                });
        }

        /// <summary>
        /// Plays the music.
        /// </summary>
        public async void Play()
        {
            await Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    // If not media open, open this book
                    if (this.systemControls.PlaybackStatus == MediaPlaybackStatus.Closed
                        || this.systemControls.PlaybackStatus == MediaPlaybackStatus.Stopped)
                    {
                        this.Media.Source = new Uri(Bible.Files.ElementAt(this.currentFileIndex), UriKind.Absolute);
                        this.Media.Play();
                    }
                    else if (this.systemControls.PlaybackStatus == MediaPlaybackStatus.Paused)
                    {
                        this.Media.Play();
                    }
                });
        }

        /// <summary>
        /// Skips to the previous track.
        /// </summary>
        public async void Previous()
        {
            await Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    // Get the next file index
                    this.currentFileIndex = Bible.GetPreviousFileIndex(this.currentFileIndex);

                    // Play the file
                    this.Media.Source = new Uri(Bible.Files.ElementAt(this.currentFileIndex), UriKind.Absolute);
                    this.Media.Play();
                });
        }

        /// <summary>
        /// Plays the file.
        /// </summary>
        /// <param name="bookIndex">Index of the book.</param>
        /// <param name="chapterIndex">Index of the chapter.</param>
        public async void PlayFile(int bookIndex, int chapterIndex)
        {
            await Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    // Get the file index
                    this.currentFileIndex = Bible.GetFileIndex(bookIndex, chapterIndex);

                    // Play the file
                    this.Media.Source = new Uri(Bible.Files.ElementAt(this.currentFileIndex), UriKind.Absolute);
                    this.Media.Play();
                });
        }

        /// <summary>
        /// Initialises the transport controls.
        /// </summary>
        private void InitialiseTransportControls()
        {
            // Hook up app to system transport controls.
            this.systemControls = SystemMediaTransportControls.GetForCurrentView();
            this.systemControls.ButtonPressed += this.SystemControls_ButtonPressed;

            // Register to handle the following system transport control buttons.
            this.systemControls.IsPlayEnabled = true;
            this.systemControls.IsPauseEnabled = true;
            this.systemControls.IsNextEnabled = true;
            this.systemControls.IsPreviousEnabled = true;
        }

        /// <summary>
        /// Handles the CurrentStateChanged event of the MusicPlayer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Media_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            switch (this.Media.CurrentState)
            {
                case MediaElementState.Playing:
                    this.systemControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                    this.systemControls.DisplayUpdater.Type = MediaPlaybackType.Music;
                    this.systemControls.DisplayUpdater.MusicProperties.Title = Bible.GetTrackName(this.currentFileIndex);
                    this.systemControls.DisplayUpdater.Update();
                    break;
                case MediaElementState.Paused:
                    this.systemControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                    break;
                case MediaElementState.Stopped:
                    this.systemControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
                    break;
                case MediaElementState.Closed:
                    this.systemControls.PlaybackStatus = MediaPlaybackStatus.Closed;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Handles the MediaEnded event of the Media control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Media_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Get the next file index
            this.currentFileIndex = Bible.GetNextFileIndex(this.currentFileIndex);

            // Get the file
            string file = Bible.Files.ElementAt(this.currentFileIndex);

            // Play the file
            this.Media.Source = new Uri(file, UriKind.Absolute);
            this.Media.Play();
        }

        /// <summary>
        /// Handles the button pressed event for the system controls.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SystemMediaTransportControlsButtonPressedEventArgs"/> instance containing the event data.</param>
        private void SystemControls_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs e)
        {
            switch (e.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    this.Play();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    this.Pause();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    this.Previous();
                    break;
                case SystemMediaTransportControlsButton.Next:
                    this.Next();
                    break;
                default:
                    break;
            }
        }
    }
}
