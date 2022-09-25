using Newtonsoft.Json;
using Syroot.Windows.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace TEPSClientInstallService_UpdateUtility.Classes
{
    internal class apiClass
    {
        private readonly string badAppName = "Client.Admin.Tool.exe";
        private readonly string goodAppName = "Client Admin Tool.exe";
        private readonly string getByIDNum = "34";
        private readonly string downloadByIDNum = "23";
        private readonly string jsonFile = "ClientAdminAppsettings.json";
        private readonly string externalURL1 = "https://github.com/davasorus/FileRepository/releases/download/1.5/NWPS.Client.Admin.Tool.exe";

        private int i = 0;

        private loggingClass loggingClass = new loggingClass();

        private BackgroundWorker getByIDbg;

        private jsonObj JO1 = new jsonObj();

        private void InitializeBackgroundWorker()
        {
            //Background checker for interacting with and comparing against the API for version number
            getByIDbg = new BackgroundWorker();
            getByIDbg.DoWork += getByIDbg_DoWork;
        }

        //app start up API checker
        public void updateAPICheck()
        {
            InitializeBackgroundWorker();

            getByIDbg.RunWorkerAsync();
        }

        //returns update result to be viewed in the UI
        public async Task<string> returnMessage()
        {
            var info = updateResult.updateMessage;

            return info;
        }

        //will query the API - test
        public async Task getByID(string ID)
        {
            try
            {
                var httpClient = new HttpClient();
                var defaultRequestHeaders = httpClient.DefaultRequestHeaders;

                if (defaultRequestHeaders.Accept == null ||
                   !defaultRequestHeaders.Accept.Any(m => m.MediaType == "application/json"))
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new
                      MediaTypeWithQualityHeaderValue("application/json"));
                }

                HttpResponseMessage response = await httpClient.GetAsync("https://davasoruswebapi.azurewebsites.net/api/webapi/" + ID);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();

                    compare(json);
                }
                else
                {
                    string logEntry1 = $" Failed to call the Web Api: {response.StatusCode}";

                    loggingClass.logEntryWriter(logEntry1, "error");

                    string content = await response.Content.ReadAsStringAsync();
                    string logEntry2 = $" Content: {content}";

                    loggingClass.logEntryWriter(logEntry2, "error");
                }
            }
            catch (TaskCanceledException)
            {
                //await loggingClass.remoteErrorReporting("Client Admin Tool", Assembly.GetExecutingAssembly().GetName().Version.ToString(), "Task Cancellation error, must be awaited all the way down", "Automated Error Reported by " + Environment.UserName);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Unable to connect to the remote server"))
                {
                    i = 100;

                    loggingClass.logEntryWriter("Unable to connect to end point, unable to check for updates", "error");
                }
                else if (ex.Message.Contains("The underlying connection was closed: A connection that was expected to be kept alive was closed by the server"))
                {
                    i = 100;

                    loggingClass.logEntryWriter("Unable to connect to end point, unable to check for updates", "error");
                }
                else if (ex.ToString().Contains("System.IO.DirectoryNotFoundException"))
                {
                    i = 100;
                    loggingClass.logEntryWriter("Unable to find config file directory, unable to check for updates", "error");
                    //loggingClass.queEntrywriter("Please verify that client and config file are in the same folder");
                }
                else if (ex.Message.Contains("The underlying connection was closed: A connection that was expected to be kept alive was closed by the server"))
                {
                }
                else
                {
                    i = 100;

                    string logEntry1 = ex.ToString();

                    loggingClass.logEntryWriter(logEntry1, "error");

                    //await loggingClass.remoteErrorReporting("Client Admin Tool", Assembly.GetExecutingAssembly().GetName().Version.ToString(), ex.ToString(), "Automated Error Reported by " + Environment.UserName);
                }
            }
        }

        //will query the file controller API
        public async Task downloadByID(string ID)
        {
            try
            {
                var httpClient = new HttpClient();
                var defaultRequestHeaders = httpClient.DefaultRequestHeaders;

                if (defaultRequestHeaders.Accept == null ||
                   !defaultRequestHeaders.Accept.Any(m => m.MediaType == "application/json"))
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new
                      MediaTypeWithQualityHeaderValue("application/json"));
                }

                HttpResponseMessage response = await httpClient.GetAsync("https://davasoruswebapi.azurewebsites.net/api/webapi/filecontroller/" + ID);

                string downloadsPath = new KnownFolder(KnownFolderType.Downloads).Path;

                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var fileInfo = new FileInfo(downloadsPath + "\\" + badAppName);
                    using (var fileStream = fileInfo.OpenWrite())
                    {
                        await stream.CopyToAsync(fileStream);
                    }
                }

                if (response.IsSuccessStatusCode)
                {
                    string logEntry = "File downloaded";
                    loggingClass.logEntryWriter(logEntry, "info");
                }
                else
                {
                    string logEntry1 = $"Failed to call the Web Api: {response.StatusCode}";

                    loggingClass.logEntryWriter(logEntry1, "error");

                    string content = await response.Content.ReadAsStringAsync();
                    string logEntry2 = $"Content: {content}";

                    loggingClass.logEntryWriter(logEntry2, "info");
                }
            }
            catch (TaskCanceledException)
            {
                //await loggingClass.remoteErrorReporting("Client Admin Tool", Assembly.GetExecutingAssembly().GetName().Version.ToString(), "Task Cancellation error, must be awaited all the way down", "Automated Error Reported by " + Environment.UserName);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Unable to connect to the remote server"))
                {
                    loggingClass.logEntryWriter("Unable to connect to end point, unable to check for update history", "error");
                }
                else if (ex.Message.Contains("The configuration file 'ClientAdminAppsettings.json' was not found and is not optional"))
                {
                    loggingClass.logEntryWriter("There was an error searching for update history, will re attempt.", "error");

                    Task task3 = Task.Factory.StartNew(() => getAll());
                }
                else
                {
                    string logEntry1 = ex.ToString();

                    loggingClass.logEntryWriter(logEntry1, "error");

                    //await loggingClass.remoteErrorReporting("Client Admin Tool", Assembly.GetExecutingAssembly().GetName().Version.ToString(), ex.ToString(), "Automated Error Reported by " + Environment.UserName);
                }
            }
        }

        //actual async task to get all db entries (will return entries as well)
        public async Task getAll()
        {
            try
            {
                var httpClient = new HttpClient();
                var defaultRequestHeaders = httpClient.DefaultRequestHeaders;

                if (defaultRequestHeaders.Accept == null ||
                   !defaultRequestHeaders.Accept.Any(m => m.MediaType == "application/json"))
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new
                      MediaTypeWithQualityHeaderValue("application/json"));
                }

                HttpResponseMessage response = await httpClient.GetAsync("https://davasoruswebapi.azurewebsites.net/api/webapi/public/history");

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    string logEntry = json;

                    List<apiObj> list = new List<apiObj>();

                    var json1 = json;
                    var objects = JsonConvert.DeserializeObject<List<apiObj>>(json1);

                    foreach (var obj in objects)
                    {
                        if (obj.appName.Contains("Client Admin Tool") || obj.appName.Contains("NWPS Client Admin Tool"))
                        {
                            string date = obj.tansactionDateTime;

                            var parsedDate = DateTime.Parse(date);

                            DateTime jsonDate = parsedDate.ToLocalTime();

                            //this.Dispatcher.Invoke(() => apiHistoryObjs.Collection.Add(new apiHistoryObj { AppName = obj.appName, AppVersion = obj.appVersion, ReleaseNotes = obj.releaseNotes, Date = jsonDate.ToString() }));
                        }
                    }
                }
                else
                {
                    string logEntry1 = $" Failed to call the Web Api: {response.StatusCode}";

                    loggingClass.nLogLogger(logEntry1, "error");

                    string content = await response.Content.ReadAsStringAsync();
                    string logEntry2 = $" Content: {content}";

                    loggingClass.nLogLogger(logEntry2, "error");
                }
            }
            catch (TaskCanceledException)
            {
                //await loggingClass.remoteErrorReporting("Client Admin Tool", Assembly.GetExecutingAssembly().GetName().Version.ToString(), "Task Cancellation error, must be awaited all the way down", "Automated Error Reported by " + Environment.UserName);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Unable to connect to the remote server"))
                {
                    loggingClass.logEntryWriter("Unable to connect to end point, unable to check for update history", "error");

                    //this.Dispatcher.Invoke(() => apiHistoryObjs.Collection.Add(new apiHistoryObj { AppName = "Error", AppVersion = "0", ReleaseNotes = "Unable to retrieve update history", Date = DateTime.Now.ToString() }));
                }
                else if (ex.Message.Contains("The configuration file 'ClientAdminAppsettings.json' was not found and is not optional"))
                {
                    loggingClass.logEntryWriter("There was an error searching for update history, will re attempt.", "error");
                    //loggingClass.queEntrywriter("There was an error searching for update history, will re attempt.");

                    // jsonClass.createConfigJSON();

                    Task task3 = Task.Factory.StartNew(() => getAll());
                }
                else
                {
                    string logEntry1 = ex.ToString();

                    loggingClass.logEntryWriter(logEntry1, "error");

                    //await loggingClass.remoteErrorReporting("Client Admin Tool", Assembly.GetExecutingAssembly().GetName().Version.ToString(), ex.ToString(), "Automated Error Reported by " + Environment.UserName);
                }
            }
        }

        //background worker code for API querying
        private async void getByIDbg_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                await getByID(getByIDNum);
            }
            catch (Exception ex)
            {
                string logEntry1 = ex.ToString();

                loggingClass.logEntryWriter(logEntry1, "error");

                //await loggingClass.remoteErrorReporting("Client Admin Tool", Assembly.GetExecutingAssembly().GetName().Version.ToString(), ex.ToString(), "Automated Error Reported by " + Environment.UserName);
            }
        }

        //converts XML to json
        public async void convertToJson(string document)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(document);

                var json = JsonConvert.SerializeXmlNode(doc.FirstChild.NextSibling, Newtonsoft.Json.Formatting.Indented, true);

                //this is required IF the XML doc DOES NOT have the XML version and encoding declaration at the top of the file.
                //otherwise it will just grab/convert the first XML node
                if (json == "null")
                {
                    var json1 = JsonConvert.SerializeXmlNode(doc.FirstChild, Newtonsoft.Json.Formatting.Indented, true);

                    File.WriteAllText(jsonFile, json1);
                }
                else
                {
                    File.WriteAllText(jsonFile, json);
                }

                string logEntry = "AppSettings JSON File Created.";

                loggingClass.logEntryWriter(logEntry, "info");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Could not find a part of the path"))
                {
                }
                else
                {
                    string logEntry = "Error Creating JSON file. ERROR: " + ex.ToString();

                    loggingClass.logEntryWriter(logEntry, "error");

                    //await loggingClass.remoteErrorReporting("Client Admin Tool", Assembly.GetExecutingAssembly().GetName().Version.ToString(), ex.ToString(), "Automated Error Reported by " + Environment.UserName);
                }
            }
        }

        //compares application version number to API version number
        private void compare(string json)
        {
            deserializeJSON(json);

            string A = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            //this removes the separators in the version number
            string B = A.Replace(".", "");
            string subB = JO1.appVersion.Replace(".", "");

            //this converts the string numbers into ints
            int a = int.Parse(B);
            int b = int.Parse(subB);

            //this compares the incoming number from the app version number
            int x = a - b;

            if (x > 0)
            {
                updateResult.updateMessage = "Dev Special :)";

                return;
            }
            else if (x == 0)
            {
                updateResult.updateMessage = "Up-to-Date";

                return;
            }
            else if (x < 0)
            {
                string logEntry = Assembly.GetExecutingAssembly().GetName().Name.ToString() + " current version is on older release";
                loggingClass.logEntryWriter(logEntry, "info");

                updateResult.updateMessage = "Newer Version Found";

                downloadTask(badAppName, externalURL1, Directory.GetCurrentDirectory());

                return;
            }
        }

        //deserializes JSON package and dynamically creates objects
        private async void deserializeJSON(string json)
        {
            try
            {
                var appInfo = JsonConvert.DeserializeObject<dynamic>(json);

                if (appInfo.id != null)
                {
                    JO1.id = appInfo.id;
                    JO1.appName = appInfo.appName;
                    JO1.appVersion = appInfo.appVersion;
                    JO1.releaseNotes = appInfo.releaseNotes;
                    JO1.tansactionDateTime = appInfo.tansactionDateTime;

                    string logEntry1 = ("    ID " + appInfo.id);
                    string logEntry2 = ("    App Name " + appInfo.appName);
                    string logEntry3 = ("    App Version " + appInfo.appVersion);
                    string logEntry4 = ("    Release Notes " + appInfo.releaseNotes);
                    string logEntry5 = ("    Date of Update " + appInfo.tansactionDateTime);

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("JSON Package Parsed below ");
                    sb.AppendLine(logEntry1);
                    sb.AppendLine(logEntry2);
                    sb.AppendLine(logEntry3);
                    sb.AppendLine(logEntry4);
                    sb.Append(logEntry5);
                    loggingClass.nLogLogger(sb.ToString(), "info");
                }
                else
                {
                    throw new ArgumentNullException(json + " was not correctly parsed into " + appInfo + " ID was null.");
                }
            }
            catch (Exception ex)
            {
                string logEntry = "error parsing information from JSON package: " + ex.Message;
                loggingClass.logEntryWriter(logEntry, "error");

                //await loggingClass.remoteErrorReporting("Client Admin Tool", Assembly.GetExecutingAssembly().GetName().Version.ToString(), ex.ToString(), "Automated Error Reported by " + Environment.UserName);
            }
        }

        //prompts the user with a message to update or not
        public void downloadTask(string programName, string URL, string location)
        {
            string date = JO1.tansactionDateTime;
            var parsedDate = DateTime.Parse(date);
            DateTime jsonDate = parsedDate.ToLocalTime();

            string logEntry1 = (" There is a new version of the client available. Please go to the Update Pending Tab to download.");
            string logEntry2 = ("    -App Name: " + JO1.appName.ToString());
            string logEntry3 = ("   - Current version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + "  -> New Version: " + JO1.appVersion.ToString());
            string logEntry4 = ("   - Release Notes: " + JO1.releaseNotes.ToString());
            string logEntry5 = ("   - Date of Update: " + jsonDate);

            double logEntry6 = (DateTime.Now.Subtract(jsonDate).Days);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(logEntry1);
            sb.AppendLine(logEntry2);
            sb.AppendLine(logEntry3);
            sb.AppendLine(logEntry4);
            sb.AppendLine(logEntry5);
            sb.AppendLine("    -approximately " + logEntry6 + " day/s Ago");

            string title = "Upgrade Dialog";
            string message = sb.ToString();
        }

        //does the actual downloading
        public async Task downloadExternal(string programName, string URL, string location)
        {
            try
            {
                string logEntry = "Attempting to Download " + programName;

                loggingClass.logEntryWriter(logEntry, "info");
                //loggingClass.queEntrywriter(logEntry);

                //this will grab the local user downloads folder
                string downloadsPath = new KnownFolder(KnownFolderType.Downloads).Path;

                //will check the download folder to make sure the download folder is clear of an older version of the app
                downloadPreChecker(downloadsPath);

                //this will download the application from the passed URL
                await downloadByID(downloadByIDNum);

                string logEntry1 = programName + " downloaded";
                loggingClass.logEntryWriter(logEntry1, "info");
                //loggingClass.queEntrywriter(logEntry1);

                //this will attempt to copy the program from the downloads folder to a folder
                //this then will run that application in the new location
                try
                {
                    if (File.Exists(Path.Combine(location, Path.GetFileName(badAppName))))
                    {
                        File.Copy(Path.Combine(downloadsPath, programName), Path.Combine(location, Path.GetFileName(badAppName)), true);

                        relableandMove(location, goodAppName, programName, goodAppName);

                        Process.Start(goodAppName);

                        string logEntry2 = goodAppName + " started in location " + location;
                        loggingClass.logEntryWriter(logEntry2, "info");

                        Environment.Exit(0);
                    }
                    else
                    {
                        Task Check = Task.Factory.StartNew(() => externalDownloadChecker(downloadsPath));

                        Task.WaitAll(Check);

                        File.Copy(Path.Combine(downloadsPath, programName), Path.Combine(location, Path.GetFileName(badAppName)), true);

                        relableandMove(location, goodAppName, programName, goodAppName);

                        Process.Start(goodAppName);

                        string logEntry2 = goodAppName + " started in location " + location;
                        loggingClass.logEntryWriter(logEntry2, "info");

                        Environment.Exit(0);
                    }
                }
                catch (IOException ex)
                {
                    loggingClass.logEntryWriter(ex.ToString(), "error");
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Access to the path"))
                    {
                        loggingClass.logEntryWriter(ex.ToString(), "error");

                        Thread.Sleep(5000);

                        Process.Start(goodAppName);
                    }
                    else
                    {
                        string logEntry9 = ex.ToString();

                        loggingClass.logEntryWriter(logEntry9, "error");

                        //await loggingClass.remoteErrorReporting("Client Admin Tool", Assembly.GetExecutingAssembly().GetName().Version.ToString(), ex.ToString(), "Automated Error Reported by " + Environment.UserName);
                    }
                }
            }
            catch (IOException ex)
            {
                loggingClass.logEntryWriter(ex.ToString(), "error");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Access to the path"))
                {
                    downloadExternal(programName, URL, location);
                }
                else
                {
                    string logEntry = ex.ToString();

                    loggingClass.logEntryWriter(logEntry, "error");

                    //await loggingClass.remoteErrorReporting("Client Admin Tool", Assembly.GetExecutingAssembly().GetName().Version.ToString(), logEntry, "Automated Error Reported by " + Environment.UserName);
                }
            }
        }

        //this will check to make sure the application that is being downloaded is actually present in the download folder.
        //this will wait 6 - 10 Second increments after the initial 10 second window.
        private void externalDownloadChecker(string location)
        {
            for (int i = 1; i <= 10; i++)
            {
                if (!File.Exists(Path.Combine(location, Path.GetFileName(badAppName))))
                {
                    string logEntry = "waiting to find downloaded application for " + i.ToString() + "0 seconds";
                    loggingClass.logEntryWriter(logEntry, "info");

                    Thread.Sleep(5000);
                }
                else
                {
                    return;
                }
            }
        }

        //this will look for the old client (currently running) and relabel it to appname_oldversion
        //if an older version exists it is deleted.
        private async void relableandMove(string location, string app, string sourceFile, string destinationFile)
        {
            string appFolder = Path.GetDirectoryName(location);
            string appName = Path.GetFileNameWithoutExtension(app);
            string appExtension = Path.GetExtension(location);
            string archivePath = Path.Combine(appFolder, appName + "_OldVersion" + appExtension);
            try
            {
                if (File.Exists(archivePath))
                {
                    string logEntry3 = "Old backup found. Will attempt to delete";
                    loggingClass.logEntryWriter(logEntry3, "info");

                    File.Delete(archivePath);

                    string logEntry2 = archivePath + " deleted";
                    loggingClass.logEntryWriter(logEntry2, "info");
                }

                File.Move(app, archivePath);

                string logEntry = app + " renamed to " + appName + "_OldVersion" + appExtension;
                loggingClass.logEntryWriter(logEntry, "info");

                File.Move(sourceFile, destinationFile);

                string logEntry1 = sourceFile + " renamed to " + destinationFile;
                loggingClass.logEntryWriter(logEntry1, "info");
            }
            catch (Exception ex)
            {
                string logEntry = ex.ToString();

                loggingClass.logEntryWriter(logEntry, "error");

                //await loggingClass.remoteErrorReporting("Client Admin Tool", Assembly.GetExecutingAssembly().GetName().Version.ToString(), ex.ToString(), "Automated Error Reported by " + Environment.UserName);
            }
        }

        //will check the download folder to make sure the download folder is clear of an older version of the app
        private void downloadPreChecker(string downloadsPath)
        {
            string logEntry = "Checking download folder to make sure " + badAppName + " is not already downloaded.";

            loggingClass.logEntryWriter(logEntry, "info");

            string[] programName = { badAppName, goodAppName };

            string[] files = Directory.GetFiles(downloadsPath, "*.*");

            //for each file in the downloads folder the name is compared against either the of the known versions of the name
            //if it is found the file is deleted to prepare for the download from the API or GitHub
            foreach (var f in files)
            {
                foreach (var name in programName)
                {
                    if (f.Contains(name))
                    {
                        string DeletedName = " " + name;

                        File.Delete(Path.Combine(downloadsPath, name));

                        string logEntry1 = DeletedName + " was found in and deleted from " + downloadsPath;

                        loggingClass.logEntryWriter(logEntry1, "info");
                    }
                }
            }
        }
    }
}

internal class apiObj
{
    public string id { get; set; }
    public string appName { get; set; }
    public string appVersion { get; set; }
    public string releaseNotes { get; set; }
    public string tansactionDateTime { get; set; }
}

internal class jsonObj
{
    public string id { get; set; }
    public string appName { get; set; }
    public string appVersion { get; set; }
    public string releaseNotes { get; set; }
    public string tansactionDateTime { get; set; }
}

internal class updateResult
{
    public static string updateMessage { get; set; }
}