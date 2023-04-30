using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PaintProject
{
    public class RecentFilesManager
    {
        private const string RECENT_FILES_FILENAME = "recent_files.txt";
        private const int MAX_RECENT_FILES = 10;

        public ObservableCollection<RecentFile> RecentFiles { get; private set; }

        public RecentFilesManager()
        {
            RecentFiles = new ObservableCollection<RecentFile>();
        }

        public void AddRecentFile(string fileName, string filePath)
        {
            // Remove the file if it's already in the recent files list
            var existingFile = RecentFiles.FirstOrDefault(f => f.FileName == fileName && f.FilePath == filePath);
            if (existingFile != null)
            {
                RecentFiles.Remove(existingFile);
            }

            // Add the file to the beginning of the recent files list
            RecentFiles.Insert(0, new RecentFile { FileName = fileName, FilePath = filePath });

            // Remove any files beyond the maximum recent files limit
            while (RecentFiles.Count > MAX_RECENT_FILES)
            {
                RecentFiles.RemoveAt(RecentFiles.Count - 1);
            }

            // Save the recent files list to a text file
            //SaveRecentFiles();
        }

        public void LoadRecentFiles()
        {
            RecentFiles.Clear();

            try
            {
                // Load the recent files list from a text file
                var recentFilesJson = File.ReadAllText(RECENT_FILES_FILENAME);
                var recentFiles = JsonConvert.DeserializeObject<List<RecentFile>>(recentFilesJson);

                // Add the recent files to the list
                if (recentFiles != null)
                {
                    foreach (var recentFile in recentFiles)
                    {
                        RecentFiles.Add(recentFile);
                    }
                }
            }
            catch
            {
                // Ignore any errors when loading the recent files list
            }
        }

        public void SaveRecentFiles()
        {
            try
            {
                // Serialize the recent files
                var recentFileString = JsonConvert.SerializeObject(RecentFiles);
                File.WriteAllText(RECENT_FILES_FILENAME, recentFileString);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
