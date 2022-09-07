using System;
using System.Diagnostics;
using SharpOSC;
using System.Drawing;
using Console = Colorful.Console;
using Colorful;

class SpotifyOSC
{

    static string lastSong = "";
    static bool foundSpotify = false;

    static void Main()
    {
        String entryMsg = "Spotify OSC by Skye/Vespei";
        StyleSheet entryStyle = new StyleSheet(Color.White);
        entryStyle.AddStyle("Spotify OSC[a-z]*", Color.FromArgb(30, 215, 96), match => match);
        entryStyle.AddStyle("Skye/Vespei[a-z]*", Color.FromArgb(162, 155, 254), match => match);
        Console.WriteLineStyled(entryMsg, entryStyle);
        String rawDiscord = "Join us on Discord! https://discord.gg/qRPkbhK49c";
        StyleSheet discordStyle = new StyleSheet(Color.White);
        discordStyle.AddStyle("Discord[a-z]*", Color.FromArgb(114, 137, 218), match => match);
        discordStyle.AddStyle("https://discord.gg/qRPkbhK49c[a-z]*", Color.FromArgb(9, 132, 227), match => match);
        Console.WriteLineStyled(rawDiscord, discordStyle);
        Console.Title = "VRChat Spotify OSC - (Not Playing Anything)";
        Console.WriteLine("Scanning for Spotify Process", Color.FromArgb(129, 236, 236));
        Process[] spotifyProcess = FindProcess();

        if (spotifyProcess.Length > 0)
        {
            Console.WriteLine("Spotify process found", Color.FromArgb(30, 215, 96));
            foundSpotify = true;
            string[] spotifyTitleArray = spotifyProcess[0].MainWindowTitle.Split(" - ");
            if (spotifyTitleArray.Length > 1)
            {
                Console.Title = "VRChat Spotify OSC - (" + spotifyTitleArray[1] + " by " + spotifyTitleArray[0] + ")";
                lastSong = spotifyTitleArray[1];
                updateOSC(spotifyTitleArray);
                preventCloseLoop();
            } else
            {
                preventCloseLoop();
            }
        }
        else
        {
            Console.WriteLine("Spotify process not found, Please open Spotify", Color.FromArgb(255, 118, 117));
            preventCloseLoop();
        }
    }

    static Process[] FindProcess()
    {
        Process[] p = Process.GetProcessesByName("Spotify");
        return p;
    }

    static void updateOSC(string[] spotifyArray)
    {
        string rawMsg = "\"" + spotifyArray[1] + "\" by " + spotifyArray[0];
        OscMessage oscMsg = new OscMessage("/chatbox/input", rawMsg, true);
        UDPSender udpSend = new UDPSender("127.0.0.1", 9000);
        udpSend.Send(oscMsg);
    }

    static void preventCloseLoop()
    {
        while (true)
        {
            Thread.Sleep(5000);
            Process[] spotifyProcess = FindProcess();
            if (spotifyProcess.Length > 0)
            {
                if (!foundSpotify)
                {
                    Console.WriteLine("Spotify process found", Color.FromArgb(30, 215, 96));
                    foundSpotify = true;
                }
                string[] spotifyTitleArray = spotifyProcess[0].MainWindowTitle.Split(" - ");
                if (spotifyTitleArray.Length > 1)
                {
                    Console.Title = "VRChat Spotify OSC - (" + spotifyTitleArray[1] + " by " + spotifyTitleArray[0] + ")";
                    lastSong = spotifyTitleArray[1];
                    updateOSC(spotifyTitleArray);
                }
                else
                {
                    Console.Title = "VRChat Spotify OSC - (Not Playing Anything)";
                    lastSong = "";
                }
            } else
            {
                if (foundSpotify)
                {
                    Console.WriteLine("Spotify process not found, Please open Spotify", Color.FromArgb(255, 118, 117));
                    foundSpotify = false;
                    Console.Title = "VRChat Spotify OSC - (Not Playing Anything)";
                    lastSong = "";
                }
            }
        }
    }

}
