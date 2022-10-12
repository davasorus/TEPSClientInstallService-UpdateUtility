﻿using Ionic.Zip;
using System;
using System.IO;

namespace TEPSClientInstallService_UpdateUtility.Classes
{
    internal class compressionClass
    {
        private loggingClass loggingClass = new loggingClass();

        private readonly string serviceBackUpPath = @"C:\ProgramData\Tyler Technologies\Public Safety\Tyler-Client-Install-Agent\BackUps";

        //does the actual compression of found folders
        //will get the full folder path and then create substrings to compare against the entered state ID
        //if the substring starts with the state ID the folder is compressed and labeled: ORI + Date time(yyyyMMdd_HHmmss) + machine name
        //dropped into the NWS Hold\Field Reporting\Back Ups Folder
        //   if that folder doesn't exist it is created and the process is started a new.
        public void compression(string startPath, string zipPath)
        {
            try
            {
                loggingClass.logEntryWriter($"backing up {startPath} to {zipPath}", "info");

                System.IO.Compression.ZipFile.CreateFromDirectory(startPath, zipPath);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("_-1_.zip"))
                {
                    createDir(serviceBackUpPath);
                }
                else
                {
                    string logEntry = ex.ToString();

                    loggingClass.logEntryWriter(logEntry, "error");
                    //loggingClass.ezViewLogWriter(logEntry);
                    //loggingClass.remoteErrorReporting("ORI Copy Utility", Assembly.GetExecutingAssembly().GetName().Version.ToString(), ex.ToString(), "Automated Error Reported by " + Environment.UserName);
                }
            }
        }

        //this is used to create directory's
        public void createDir(string folder)
        {
            try
            {
                DirectoryInfo di = Directory.CreateDirectory(folder);

                string logEntry = folder + " was created.";
                loggingClass.logEntryWriter(logEntry, "info");
                //loggingClass.ezViewLogWriter(logEntry);
            }
            catch (Exception ex)
            {
                string logEntry = ex.ToString();

                loggingClass.logEntryWriter(logEntry, "error");
                //loggingClass.ezViewLogWriter(logEntry);
                //loggingClass.remoteErrorReporting("ORI Copy Utility", Assembly.GetExecutingAssembly().GetName().Version.ToString(), ex.ToString(), "Automated Error Reported by " + Environment.UserName);
            }
        }

        //this will decompress the file found in start path to zip path
        //once this is done the file is relabeled to the correct ORI
        public void decompress(string zipPath, string extractPath)
        {
            try
            {
                Ionic.Zip.ZipFile zip = Ionic.Zip.ZipFile.Read(zipPath);
                //Directory.CreateDirectory(outputDirectory);
                foreach (ZipEntry e in zip)
                {
                    // check if you want to extract e or not
                    e.Extract(extractPath, ExtractExistingFileAction.OverwriteSilently);

                    loggingClass.logEntryWriter($"Extracting {e}", "info");
                }
            }
            catch (Exception ex)
            {
                string logEntry = ex.ToString();

                loggingClass.logEntryWriter(logEntry, "error");
                //loggingClass.ezViewLogWriter(logEntry);
                //loggingClass.remoteErrorReporting("ORI Copy Utility", Assembly.GetExecutingAssembly().GetName().Version.ToString(), ex.ToString(), "Automated Error Reported by " + Environment.UserName);
            }
        }
    }
}