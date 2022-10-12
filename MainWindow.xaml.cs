using SourceChord.FluentWPF;
using System;
using System.Windows;
using System.IO;
using System.Windows.Media;
using System.Text.Json;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using SharpOSC;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Security.AccessControl;

namespace SpotifyOSC_WPF    
{
    public partial class MainWindow : AcrylicWindow
    {
        private bool foundSpotify = false;
        private (string lastSong, string lastFormat) lastListen = ("", "No Cached Song");
        private bool typingState = true;
        private bool saveState = false;
        private bool prefixState = true;
        private string prefixTxt = "PLAYING:";
        private bool completedLoading = false;
        private bool preventAll = false;
        string saveDirectory = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "/spotifyOSC";
        FileInfo saveJSON = new FileInfo(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "/spotifyOSC/settings.json");
        public MainWindow()
        {
            InitializeComponent();
            loadSettings();
            Thread backgroundApp = new Thread(() => beginCheck(saveState, typingState, prefixState, prefixTxt));
            backgroundApp.Start();
        }

        private void openDiscord_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://discord.gg/4pzjP679Mv");
        }

        private void syncApp()
        {
            Process[] spotifyProcess = FindProcess();
            if (spotifyProcess.Length > 0)
            {
                if (foundSpotify == false)
                {
                    foundSpotify = true;
                    this.Dispatcher.Invoke(() => {
                        AppState.Text = "Spotify Found";
                        AppState.Foreground = Brushes.Green;
                    });
                }
                (int responseCode, string formatedText, string[] rawResponse) spotifyResponse = fetchSong(spotifyProcess);
                if (spotifyResponse.responseCode == 0)
                {
                    if (saveState == true)
                    {
                        updateApp("Paused: " + truncateString(20, lastListen.lastFormat));
                        if (!prefixState)
                        {
                            updateOSC(lastListen.lastFormat, false);
                        }
                        else
                        {
                            updateOSC("PAUSED: " + lastListen.lastFormat, false);
                        }
                    }
                    else
                    {
                        updateApp("Spotify Paused");
                    }
                }
                else
                {
                    updateApp(truncateString(30, spotifyResponse.formatedText));
                    if (!prefixState)
                    {
                        updateOSC(spotifyResponse.formatedText, typingState);
                    }
                    else
                    {
                        updateOSC(prefixTxt + " " + spotifyResponse.formatedText, typingState);
                    }
                }
            }
            else
            {
                if (foundSpotify == true)
                {
                    foundSpotify = false;
                    this.Dispatcher.Invoke(() => {
                        AppState.Text = "Open Spotify";
                        AppState.Foreground = Brushes.Red;
                    });
                }
                updateApp("No Song");
            }
        }

        private void updateApp(string FormattedText)
        {
            if (!preventAll)
            {
                this.Dispatcher.Invoke(() => {
                    CurrentSong.Text = FormattedText;
                    this.SizeToContent = SizeToContent.Width;
                });
            }
        }
        private void updateOSC(string formatedText, bool typingEnabled)
        {
            if (!preventAll)
            {
                try
                {
                    OscMessage oscMsg = new OscMessage("/chatbox/input", formatedText, true);
                    UDPSender udpSend = new UDPSender("127.0.0.1", 9000);
                    udpSend.Send(oscMsg);
                    OscMessage oscMsg1 = new OscMessage("/chatbox/typing", typingEnabled);
                    UDPSender udpSend1 = new UDPSender("127.0.0.1", 9000);
                    udpSend1.Send(oscMsg1);
                }
                catch
                {
                    preventAll = true;
                    MessageBox.Show("Please check firewall settings for port 9000 and make sure to disconnect from any VPNs that may block ports.\n\nRestart SpotifyOSC after making changes.\n\nSpotifyOSC relies on communicating to 127.0.0.1:9000 which VRChat listens on.", "Unable to bind to port 9000");
                    this.Dispatcher.Invoke(() => {
                        AppState.Text = "Port Blocked";
                        AppState.Foreground = Brushes.Red;
                        CurrentSong.Text = "Restart SpotifyOSC";
                        CurrentSong.Foreground = Brushes.Red;
                    });
                }
            }
        }

        private void stateSaveCheck(object sender, RoutedEventArgs e)
        {
            saveState = true;
            saveSettings();
        }
        private void stateSaveUncheck(object sender, RoutedEventArgs e)
        {
            saveState = false;
            saveSettings();
        }
        private void stateTypeCheck(object sender, RoutedEventArgs e)
        {
            typingState = true;
            saveSettings();
        }
        private void stateTypeUncheck(object sender, RoutedEventArgs e)
        {
            typingState = false;
            saveSettings();
        }

        private void statePrefixCheck(object sender, RoutedEventArgs e)
        {
            prefixState = true;
            saveSettings();
        }

        private void statePrefixUncheck(object sender, RoutedEventArgs e)
        {
            prefixState = false;
            saveSettings();
        }

        private void updatePrefix(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                prefixTxt = UpdateTxt.Text;
                saveSettings();
            });
        }

        private void loadSettings()
        {
            if (!Directory.Exists(saveDirectory))
            {
                Directory.CreateDirectory(saveDirectory);
                if (!File.Exists(saveDirectory + "/settings.json"))
                {
                    completedLoading = true;
                    saveSettings();
                    return;
                }
                else
                {
                    return;
                }
            }
            else
            {
                if (!File.Exists(saveDirectory + "/settings.json"))
                {
                    completedLoading = true;
                    saveSettings();
                    return;
                }
            };
            try
            {
                FileStream readFromFile = saveJSON.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                StreamReader sr = new StreamReader(readFromFile);
                string rawJson = sr.ReadToEnd();
                Item? newItem = JsonSerializer.Deserialize<Item>(rawJson);
                sr.Close();
                readFromFile.Close();
                saveState = newItem.saveStateGlobal;
                typingState = newItem.typeStateGlobal;
                prefixState = newItem.prefixStateGlobal;
                if (!(newItem.prefixTxtGlobal == null))
                {
                    prefixTxt = newItem.prefixTxtGlobal;
                }
                completedLoading = true;
            }
            catch
            {
                completedLoading = true;
                saveSettings();
                return;
            }
        }
        private void saveSettings()
        {
            if (completedLoading && !preventAll)
            {
                Item saveItem = new Item { saveStateGlobal = saveState, typeStateGlobal = typingState, prefixStateGlobal = prefixState, prefixTxtGlobal = prefixTxt };
                string rawJson = JsonSerializer.Serialize(saveItem);
                FileStream writeToFile = saveJSON.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                StreamWriter sw = new StreamWriter(writeToFile);
                sw.Write(rawJson);
                sw.Close();
                writeToFile.Close();
            }
        }


        private (int,string, string[]) fetchSong(Process[] spotifyProcess)
        {
            string[] spotifyTitleArray = spotifyProcess[0].MainWindowTitle.Split(" - ");
            string[] emptyString = { "" };
            (int,string, string[]) parsedReturn = (0, "", emptyString);
            if (spotifyTitleArray.Length > 1)
            {
                if (spotifyTitleArray.Length > 2)
                {
                    string songString = truncateString(80, spotifyTitleArray[1] + " - " + spotifyTitleArray[2]);
                    string songAuthor = truncateString(45, spotifyTitleArray[0]);
                    parsedReturn = (1, "\"" + songString + "\" by " + songAuthor, spotifyTitleArray);
                    lastListen.lastSong = songString;
                    lastListen.lastFormat = "\"" + songString + "\" by " + songAuthor;
                }
                else
                {
                    string songString = truncateString(80, spotifyTitleArray[1]);
                    string songAuthor = truncateString(45, spotifyTitleArray[0]);
                    parsedReturn = (1, "\"" + songString + "\" by " + songAuthor, spotifyTitleArray);
                    lastListen.lastSong = songString;
                    lastListen.lastFormat = "\"" + songString + "\" by " + songAuthor;
                }
            }

            return parsedReturn;
        }

        private string truncateString(int maxLength, string text)
        {
            if (text.Length > maxLength)
            {
                return text.Substring(0, maxLength) + "..";
            } else
            {
                return text;
            }
        }

        static Process[] FindProcess()
        {
            Process[] p = Process.GetProcessesByName("Spotify");
            return p;
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else
                {
                    throw;
                }
            }
        }

        private void beginCheck(bool saveState, bool typingState, bool prefixState, string prefixTxt)
        {
            this.Dispatcher.Invoke(() =>
            {
                SaveBtn.IsChecked = saveState;
                TypingBtn.IsChecked = typingState;
                PrefixBtn.IsChecked = prefixState;
                UpdateTxt.Text = prefixTxt;

            });
            while (true)
            {
                if (!preventAll)
                {
                    syncApp();
                    Thread.Sleep(3000);
                }
            }
        }
    }

    public class Item
    {
        public bool saveStateGlobal { get; set; }
        public bool typeStateGlobal { get; set; }
        public bool prefixStateGlobal { get; set; }
        public string? prefixTxtGlobal { get; set; }
    }
}
