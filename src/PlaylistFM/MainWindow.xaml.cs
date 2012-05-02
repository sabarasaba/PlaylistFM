using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using System.IO;
using System.Runtime.InteropServices;

namespace PlaylistFM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        /// <summary>
        /// Stores the information given by the user.
        /// </summary>
        private class SenderData
        {
            public string artist;
            public string musicPath;
        }

        /// <summary>
        /// Stores the full-path and title of a song.
        /// </summary>
        private class fileData
        {
            public string title;
            public string path;
        }

        #region WinAPI Functions
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr FindFirstFileW(string lpFileName, out WIN32_FIND_DATAW lpFindFileData);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            public static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATAW lpFindFileData);

            [DllImport("kernel32.dll")]
            public static extern bool FindClose(IntPtr hFindFile);

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct WIN32_FIND_DATAW
            {
                public FileAttributes dwFileAttributes;
                internal System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
                internal System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
                internal System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
                public int nFileSizeHigh;
                public int nFileSizeLow;
                public int dwReserved0;
                public int dwReserved1;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
                public string cFileName;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
                public string cAlternateFileName;
            }
        #endregion

        private BackgroundWorker backgroundWorker = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();

            this.backgroundWorker.WorkerReportsProgress = true;
            this.backgroundWorker.WorkerSupportsCancellation = true;

            backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backgroundWorker_RunWorkerCompleted);
            backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker_ProgressChanged);
        }

        /// <summary>
        /// Displays the search directory dialog.
        /// </summary>
        private void textBox2_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.label1.Content = "";

            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            this.textBox2.Text = dialog.SelectedPath;
        }

        /// <summary>
        /// Backgroundworker, which actually does almost all the job.
        /// </summary>
        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            SenderData data = (SenderData)e.Argument;

            BackgroundWorker worker = sender as BackgroundWorker;
            LastFMConnector lastFM = new LastFMConnector();

            // Validating artist.
            worker.ReportProgress(0);

            if (!lastFM.isValidArtist(data.artist))
            {
                e.Cancel = true;
            }
            else
            {
                // Get the charts of the artists.
                worker.ReportProgress(25);

                List<string> tracks = lastFM.getArtistTopTracks(data.artist);
                List<fileData> files = new List<fileData>();
                List<string> playlist = new List<string>();

                // Scan the directory recursively looking for all the valid music files and store their paths and metadata in the files list.
                worker.ReportProgress(50);

                foreach (string file in fastFileSearch(data.musicPath))
                {
                    if (file.EndsWith("mp3") || file.EndsWith("m4a") || file.EndsWith("wav") || file.EndsWith("flac"))
                    {
                        TagLib.File f = TagLib.File.Create(file);

                        files.Add(new fileData()
                        {
                            title = LastFMConnector.sanitizeSongName(f.Tag.Title),
                            path = file
                        });
                    }
                }

                // Build the playlist with the songs that the user have and the top-tracks of lastfm.
                worker.ReportProgress(75);

                foreach (string topTrack in tracks)
                {
                    var file = files.FirstOrDefault(o => o.title == topTrack);

                    if ((file != null) && (!playlist.Contains(file.path)))
                        playlist.Add(file.path);
                }

                // Dump the data into a valid playlist file.
                worker.ReportProgress(100);

                using (System.IO.StreamWriter file = new System.IO.StreamWriter(data.musicPath + @"\" + data.artist + " - Best Songs.m3u"))
                {
                    foreach (string song in playlist)
                        file.WriteLine(song);
                }

                // Gives a lil extra time to bulk the data into the file.
                System.Threading.Thread.Sleep(200);

                // Dispose the lists.
                tracks = null;
                files = null;
                playlist = null;
            }
        }

        /// <summary>
        /// Once that the backgroundworker has finished, opens the directory whitin the playlist file we've just created.
        /// </summary>
        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.loading.Visibility = System.Windows.Visibility.Hidden;

            if (e.Cancelled)
            {
                this.label1.Content = "Thats not a valid artist.";
            }
            else
            {
                this.label1.Content = "";

                System.Diagnostics.Process.Start(this.textBox2.Text);
            }
        }

        /// <summary>
        /// Reports the progress of the backgroundworker.
        /// </summary>
        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 0)
                this.label1.Content = "Validating artist..";

            if (e.ProgressPercentage == 25)
                this.label1.Content = "Getting last.fm charts..";

            if (e.ProgressPercentage == 50)
                this.label1.Content = "Scanning directory..";

            if (e.ProgressPercentage == 75)
                this.label1.Content = "Matching files with charts..";

            if (e.ProgressPercentage == 100)
                this.label1.Content = "Generating playlist..";
        }

        /// <summary>
        /// Search all the files of a given directory using the fast file searching method with the win32 API.
        /// </summary>
        /// <param name="directory">The directory of which you want all the files.</param>
        /// <returns>A list with the full path of the files.</returns>
        static List<string> fastFileSearch(string directory)
        {
            IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
            WIN32_FIND_DATAW findData;
            IntPtr findHandle = INVALID_HANDLE_VALUE;

            var info = new List<string>();
            try
            {
                findHandle = FindFirstFileW(directory + @"\*", out findData);
                if (findHandle != INVALID_HANDLE_VALUE)
                {

                    do
                    {
                        if (findData.cFileName == "." || findData.cFileName == "..") continue;

                        string fullpath = directory + (directory.EndsWith("\\") ? "" : "\\") + findData.cFileName;

                        bool isDir = false;

                        if ((findData.dwFileAttributes & FileAttributes.Directory) != 0)
                        {
                            isDir = true;
                            info.AddRange(fastFileSearch(fullpath));
                        }

                        if (!isDir)
                            info.Add(fullpath);
                    }
                    while (FindNextFile(findHandle, out findData));

                }
            }
            finally
            {
                if (findHandle != INVALID_HANDLE_VALUE) FindClose(findHandle);
            }
            return info;
        }

        /// <summary>
        /// Checks if the user entered some data in the textboxs.
        /// </summary>
        /// <returns>Returns true if the user wrote some data in the textboxs.</returns>
        private bool validateUIControls()
        {
            if (!string.IsNullOrEmpty(this.textBox1.Text))
            {
                if (!string.IsNullOrEmpty(this.textBox2.Text))
                    return true;
                else
                {
                    this.label1.Content = "Please select the music directory";
                    return false;
                }
            }
            else
                this.label1.Content = "Please insert the artist/band name";

            return false;
        }

        /// <summary>
        /// Triggers the background worker if its possible.
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SenderData data = new SenderData();
            data.artist = this.textBox1.Text;
            data.musicPath = this.textBox2.Text;
            
            if ((!this.backgroundWorker.IsBusy) && (validateUIControls()))
            {
                this.loading.Visibility = System.Windows.Visibility.Visible;
                this.backgroundWorker.RunWorkerAsync(data);
            }
        }

        /// <summary>
        /// Shows my twitter profile in the browser.
        /// </summary>
        private void twButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(@"http://www.twitter.com/sabarasaba");
        }

        /// <summary>
        /// Clear the status message.
        /// </summary>
        private void textBox1_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.label1.Content = "";
        }
    }
}
