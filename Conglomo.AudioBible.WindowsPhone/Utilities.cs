// -----------------------------------------------------------------------
// <copyright file="Utilities.cs" company="Conglomo">
// Copyright 2014-2021 Conglomo Limited. Please see LICENCE.md for licence details.
// </copyright>
// -----------------------------------------------------------------------

namespace Conglomo.AudioBible
{
    using System;
    using System.Linq;
    using Data;
    using Windows.Foundation.Collections;
    using Windows.Media.Playback;

    /// <summary>
    /// Audio Bible Utilities.
    /// </summary>
    internal static class Utilities
    {
        /// <summary>
        /// Plays the file.
        /// </summary>
        /// <param name="bookIndex">Index of the book.</param>
        /// <param name="chapterIndex">Index of the chapter.</param>
        internal static void PlayFile(int bookIndex, int chapterIndex)
        {
            // Get the file index
            int index = Bible.GetFileIndex(bookIndex, chapterIndex);

            // Notify the background task of the new index
            ValueSet messageDictionary = new ValueSet();
            messageDictionary.Add("CURRENTFILEINDEX", index);
            BackgroundMediaPlayer.SendMessageToBackground(messageDictionary);

            // Get the file
            string file = Bible.Files.ElementAt(index);

            // Play the file
            BackgroundMediaPlayer.Current.SetUriSource(new Uri(file, UriKind.Absolute));
            BackgroundMediaPlayer.Current.Play();
        }
    }
}
