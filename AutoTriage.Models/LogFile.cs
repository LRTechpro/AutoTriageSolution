using System;
using System.Collections.Generic;
using System.IO;

namespace AutoTriage.Models
{
    /// <summary>
    /// Represents a single log file within a vehicle case.
    /// Contains metadata about the file and all parsed log lines.
    /// </summary>
    public class LogFile
    {
        /// <summary>
        /// Full file path of the original log file on disk.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Just the filename without path (e.g., "log_2024-01-15.txt").
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Date extracted from the log file (from filename, header, or first timestamp).
        /// </summary>
        public DateTime LogDate { get; set; }

        /// <summary>
        /// Size of the file in bytes (for display and validation).
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Collection of all lines parsed from this log file.
        /// Each line maintains its original line number within this file.
        /// </summary>
        public List<LogLine> Lines { get; set; }

        /// <summary>
        /// Optional metadata: encoding detected when reading the file.
        /// </summary>
        public string EncodingName { get; set; } = "UTF-8";

        /// <summary>
        /// Constructor initializes collections and sets default values.
        /// </summary>
        public LogFile()
        {
            // Initialize empty collection to prevent null reference errors
            Lines = new List<LogLine>();
            // Default to today's date if no date can be extracted
            LogDate = DateTime.Today;
        }

        /// <summary>
        /// Constructor that takes a file path and auto-extracts metadata.
        /// </summary>
        /// <param name="filePath">Full path to the log file</param>
        public LogFile(string filePath)
        {
            // Initialize collections first
            Lines = new List<LogLine>();
            
            // Set the full file path
            FilePath = filePath;
            
            // Extract just the filename from the full path
            FileName = Path.GetFileName(filePath);
            
            // Get file size if the file exists
            if (File.Exists(filePath))
            {
                // Read file info to get size
                var fileInfo = new FileInfo(filePath);
                FileSizeBytes = fileInfo.Length;
                
                // Use file's last write time as default log date
                LogDate = fileInfo.LastWriteTime;
            }
            else
            {
                // File doesn't exist, use defaults
                FileSizeBytes = 0;
                LogDate = DateTime.Today;
            }
        }

        /// <summary>
        /// Gets a human-readable file size string (e.g., "2.5 MB").
        /// </summary>
        public string FileSizeDisplay
        {
            get
            {
                // Convert bytes to appropriate unit
                if (FileSizeBytes < 1024)
                    return $"{FileSizeBytes} B";
                else if (FileSizeBytes < 1024 * 1024)
                    return $"{FileSizeBytes / 1024.0:F1} KB";
                else
                    return $"{FileSizeBytes / (1024.0 * 1024.0):F2} MB";
            }
        }

        /// <summary>
        /// Gets the number of lines in this log file.
        /// </summary>
        public int LineCount => Lines.Count;
    }
}
