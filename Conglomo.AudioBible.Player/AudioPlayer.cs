// -----------------------------------------------------------------------
// <copyright file="AudioPlayer.cs" company="Conglomo">
// Copyright 2014-2021 Conglomo Limited. Please see LICENCE.md for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace Conglomo.AudioBible.Player
{
    using System;
    using System.Linq;
    using System.Threading;
    using Data;
    using Windows.ApplicationModel.Background;
    using Windows.Media;
    using Windows.Media.Playback;

    /// <summary>
    /// The audio player background task.
    /// </summary>
    public sealed class AudioPlayer : IBackgroundTask, IDisposable
    {
        /// <summary>
        /// The background task is running.
        /// </summary>
        private bool backgroundTaskRunning;

        /// <summary>
        /// The background task started.
        /// </summary>
        private AutoResetEvent backgroundTaskStarted = new AutoResetEvent(false);

        /// <summary>
        /// The deferral.
        /// </summary>
        private BackgroundTaskDeferral deferral;

        /// <summary>
        /// Tracks whether Dispose has been called.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// The current file index.
        /// </summary>
        private int currentFileIndex;

        /// <summary>
        /// The system media transport control.
        /// </summary>
        private SystemMediaTransportControls systemMediaTransportControl;

        /// <summary>
        /// Finalises an instance of the <see cref="AudioPlayer"/> class.
        /// </summary>
        ~AudioPlayer()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            this.Dispose(false);
        }

        /// <summary>
        /// Disposes of the current object.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            // This object will be cleaned up by the Dispose method. 
            // Therefore, you should call GC.SupressFinalize to 
            // take this object off the finalization queue 
            // and prevent finalization code for this object 
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs the work of a background task. The system calls this method when the associated background task has been triggered.
        /// </summary>
        /// <param name="taskInstance">An interface to an instance of the background task. The system creates this instance when the task has been triggered to run.</param>
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            if (taskInstance != null)
            {
                // Set up the task
                this.deferral = taskInstance.GetDeferral();
                taskInstance.Canceled += this.TaskInstance_Canceled;
                this.backgroundTaskRunning = true;
                this.currentFileIndex = 0;

                // Set up the system play controls
                this.systemMediaTransportControl = SystemMediaTransportControls.GetForCurrentView();
                this.systemMediaTransportControl.ButtonPressed += this.SystemMediaTransportControl_ButtonPressed;
                this.systemMediaTransportControl.PropertyChanged += this.SystemMediaTransportControl_PropertyChanged;
                this.systemMediaTransportControl.IsEnabled = true;
                this.systemMediaTransportControl.IsPauseEnabled = true;
                this.systemMediaTransportControl.IsPlayEnabled = true;
                this.systemMediaTransportControl.IsNextEnabled = true;
                this.systemMediaTransportControl.IsPreviousEnabled = true;

                // Initialize message channel 
                BackgroundMediaPlayer.MessageReceivedFromForeground += this.BackgroundMediaPlayer_MessageReceivedFromForeground;

                // Set up the background music event handlers
                BackgroundMediaPlayer.Current.MediaEnded += this.BackgroundMediaPlayer_MediaEnded;
                BackgroundMediaPlayer.Current.CurrentStateChanged += this.BackgroundMediaPlayer_CurrentStateChanged;
            }
        }

        /// <summary>
        /// Disposes the current instance.
        /// </summary>
        /// <param name="disposing">If <c>true</c>, dispose all managed and unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed and unmanaged resources. 
                if (disposing)
                {
                    // Dispose managed resources.
                    this.backgroundTaskStarted.Dispose();
                }

                // Note disposing has been done.
                this.disposed = true;
            }
        }

        /// <summary>
        /// Handles the CurrentStateChanged event of the Background Media Player.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        private void BackgroundMediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            if (sender.CurrentState == MediaPlayerState.Playing)
            {
                this.systemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Playing;
                this.systemMediaTransportControl.DisplayUpdater.Type = MediaPlaybackType.Music;
                this.systemMediaTransportControl.DisplayUpdater.MusicProperties.Title = Bible.GetTrackName(this.currentFileIndex);
                this.systemMediaTransportControl.DisplayUpdater.Update();
            }
            else if (sender.CurrentState == MediaPlayerState.Paused)
            {
                this.systemMediaTransportControl.PlaybackStatus = MediaPlaybackStatus.Paused;
            }
        }

        /// <summary>
        /// Handles the MediaEnded event of the Background Media Player.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void BackgroundMediaPlayer_MediaEnded(MediaPlayer sender, object e)
        {
            // Get the next file index
            this.currentFileIndex = Bible.GetNextFileIndex(this.currentFileIndex);

            // Get the file
            string file = Bible.Files.ElementAt(this.currentFileIndex);

            // Play the file
            BackgroundMediaPlayer.Current.SetUriSource(new Uri(file, UriKind.Absolute));
            BackgroundMediaPlayer.Current.Play();
        }

        /// <summary>
        /// Fires when a message is received from the foreground app
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="MediaPlayerDataReceivedEventArgs"/> instance containing the event data.</param>
        private void BackgroundMediaPlayer_MessageReceivedFromForeground(object sender, MediaPlayerDataReceivedEventArgs e)
        {
            foreach (string key in e.Data.Keys)
            {
                switch (key.ToUpperInvariant())
                {
                    case "CURRENTFILEINDEX":
                        if (!int.TryParse(e.Data[key].ToString(), out this.currentFileIndex))
                        {
                            this.currentFileIndex = 0;
                        }

                        break;
                    case "RELOAD":
                        // Reload the current file
                        BackgroundMediaPlayer.Current.SetUriSource(new Uri(Bible.Files.ElementAt(this.currentFileIndex), UriKind.Absolute));
                        break;
                    case "NEXT":
                        // Get the next file index
                        this.currentFileIndex = Bible.GetNextFileIndex(this.currentFileIndex);

                        // Play the file
                        BackgroundMediaPlayer.Current.SetUriSource(new Uri(Bible.Files.ElementAt(this.currentFileIndex), UriKind.Absolute));
                        BackgroundMediaPlayer.Current.Play();
                        break;
                    case "PREVIOUS":
                        // Get the previous file index
                        this.currentFileIndex = Bible.GetPreviousFileIndex(this.currentFileIndex);

                        // Play the file
                        BackgroundMediaPlayer.Current.SetUriSource(new Uri(Bible.Files.ElementAt(this.currentFileIndex), UriKind.Absolute));
                        BackgroundMediaPlayer.Current.Play();
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Fires when any SystemMediaTransportControl property is changed by system or user
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="SystemMediaTransportControlsPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void SystemMediaTransportControl_PropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs args)
        {
            // TODO: If soundlevel turns to muted, app can choose to pause the music
        }

        /// <summary>
        /// This function controls the button events from UVC.
        /// This code if not run in background process, will not be able to handle button pressed events when app is suspended.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="SystemMediaTransportControlsButtonPressedEventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.ArgumentException">Background Task did not initialise in time</exception>
        private void SystemMediaTransportControl_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:

                    // If music is in paused state, for a period of more than 5 minutes, 
                    // app will get task cancellation and it cannot run code. 
                    // However, user can still play music by pressing play via UVC unless a new app comes in clears UVC.
                    // When this happens, the task gets re-initialized and that is asynchronous and hence the wait
                    if (!this.backgroundTaskRunning)
                    {
                        bool result = this.backgroundTaskStarted.WaitOne(2000);
                        if (!result)
                        {
                            throw new ArgumentException("Background Task did not initialise in time");
                        }
                    }

                    BackgroundMediaPlayer.Current.SetUriSource(new Uri(Bible.Files.ElementAt(this.currentFileIndex), UriKind.Absolute));
                    BackgroundMediaPlayer.Current.Play();

                    break;
                case SystemMediaTransportControlsButton.Pause:
                    BackgroundMediaPlayer.Current.Pause();
                    break;
                case SystemMediaTransportControlsButton.Next:

                    // Get the next file index
                    this.currentFileIndex = Bible.GetNextFileIndex(this.currentFileIndex);

                    // Play the file
                    BackgroundMediaPlayer.Current.SetUriSource(new Uri(Bible.Files.ElementAt(this.currentFileIndex), UriKind.Absolute));
                    BackgroundMediaPlayer.Current.Play();
                    break;
                case SystemMediaTransportControlsButton.Previous:

                    // Get the previous file index
                    this.currentFileIndex = Bible.GetPreviousFileIndex(this.currentFileIndex);

                    // Play the file
                    BackgroundMediaPlayer.Current.SetUriSource(new Uri(Bible.Files.ElementAt(this.currentFileIndex), UriKind.Absolute));
                    BackgroundMediaPlayer.Current.Play();
                    break;
            }
        }

        /// <summary>
        /// Handles the cancellation event of the task instance.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="reason">The cancellation reason.</param>
        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            this.backgroundTaskRunning = false;
            this.deferral.Complete();
        }
    }
}
