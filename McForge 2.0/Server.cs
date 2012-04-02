﻿/*
Copyright 2011 MCForge
Dual-licensed under the Educational Community License, Version 2.0 and
the GNU General Public License, Version 3 (the "Licenses"); you may
not use this file except in compliance with the Licenses. You may
obtain a copy of the Licenses at
http://www.opensource.org/licenses/ecl2.php
http://www.gnu.org/licenses/gpl-3.0.html
Unless required by applicable law or agreed to in writing,
software distributed under the Licenses are distributed on an "AS IS"
BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
or implied. See the Licenses for the specific language governing
permissions and limitations under the Licenses.
*/

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Timers;
using System.IO;
using MCForge.Beat;

namespace MCForge
{
    public static class Server
    {
		/// <summary>
		/// Gets or sets the server's hash
		/// </summary>
		public static string Hash { get; set; }
		/// <summary>
		/// Gets or sets the server's current URL
		/// </summary>
		public static string URL { get; set; }
		/// <summary>
        /// Get whether the server is currently shutting down
        /// </summary>
        public static bool shuttingDown;
        /// <summary>
        /// Get whether the server is currently fully started or not
        /// </summary>
        public static bool Started = false;

        private static System.Timers.Timer UpdateTimer;

        internal static List<Player> Connections = new List<Player>();
        /// <summary>
        /// Get the current list of online players, note that if your doing a foreach on this always add .ToArray() to the end, it solves a LOT of issues
        /// </summary>
        public static List<Player> Players = new List<Player>();
        /// <summary>
        /// Get the current list of banned ip addresses, note that if your doing a foreach on this (or any other public list) you should always add .ToArray() to the end so that you avoid errors!
        /// </summary>
        public static List<string> BannedIP = new List<string>();
        /// <summary>
        /// The list of MCForge developers.
        /// </summary>
        public static List<string> devs = new List<string>(new string[] { "EricKilla", "Merlin33069", "Snowl", "Gamemakergm", "cazzar", "hirsty", "Givo", "jasonbay13", "Alem_Zupa", "7imekeeper", "Shade2010", "TheMusiKid", "Nerketur"});
        /// <summary>
        /// List of players that agreed to the rules
        /// </summary>
        public static List<string> agreed = new List<string>();
        /// <summary>
        /// The main level of the server, where players spawn when they first join
        /// </summary>
        public static Level Mainlevel;

        //Voting
        /// <summary>
        /// Is the server in voting mode?
        /// </summary>
        public static bool voting;
        /// <summary>
        /// Is it a kickvote?
        /// </summary>
        public static bool kickvote;
        /// <summary>
        /// Amount of yes votes.
        /// </summary>
        public static int YesVotes;
        /// <summary>
        /// Amount of no votes.
        /// </summary>
        public static int NoVotes;
        /// <summary>
        /// The player who's getting, if it's /votekick
        /// </summary>
        public static Player kicker;
        /// <summary>
        /// The server's default color.
        /// </summary>
        public static string DefaultColor = "&e";

        internal static void Init()
        {
            Log("Creating listening socket on port " + ServerSettings.port + "... ");
			StartListening();
			Log("Done.");

            Mainlevel = Level.CreateLevel(new Point3(256, 256, 64), Level.LevelTypes.Flat, "main");

            UpdateTimer = new System.Timers.Timer(100);
            UpdateTimer.Elapsed += delegate { Update(); };
            UpdateTimer.Start();

            LoadAllDlls.Init();

            Log("[Important]: Server Started.", ConsoleColor.Black, ConsoleColor.White);
            Started = true;

            CmdReloadCmds reload = new CmdReloadCmds();
            reload.Initialize();

            //Create the directories we need...
            if (!Directory.Exists("text")) { Directory.CreateDirectory("text"); Log("Created text directory...", ConsoleColor.White, ConsoleColor.Black); }
            if (!File.Exists("text/agreed.txt")) { File.Create("text/agreed.txt").Close(); Log("Created agreed.txt", ConsoleColor.White, ConsoleColor.Black); }

            try
            {
                string[] lines = File.ReadAllLines("text/agreed.txt");
                foreach (string pl in lines) { agreed.Add(pl); }
            }
            catch { Log("[Error] Error reading agreed players!", ConsoleColor.Red, ConsoleColor.Black); }
			try {
				Heart.Init();
			} catch (Exception e) {
				Server.Log(e);
			}

        }

        static void Update()
        {
            Player.GlobalUpdate();
        }

        #region Socket Stuff
        private static TcpListener listener;
        private static void StartListening()
        {
        startretry:
            try
            {
                listener = new TcpListener(System.Net.IPAddress.Any, ServerSettings.port);
                listener.Start();
                IAsyncResult ar = listener.BeginAcceptTcpClient(new AsyncCallback(AcceptCallback), listener);
            }
            catch (SocketException E)
            {
                Server.Log(E);
            }
            catch (Exception E)
            {
                Server.Log(E);
                goto startretry;
            }
        }
        private static void AcceptCallback(IAsyncResult ar)
        {
            TcpListener listener2 = (TcpListener)ar.AsyncState;
            try
            {
                TcpClient clientSocket = listener2.EndAcceptTcpClient(ar);
                new Player(clientSocket);
            }
            catch { }
            if (!shuttingDown)
            {
                listener.BeginAcceptTcpClient(new AsyncCallback(AcceptCallback), listener);
            }
        }
        #endregion
        #region Log Stuff
        /// <summary>
        /// Write A message to the Console and the GuiLog using default (white on black) colors.
        /// </summary>
        /// <param name="message">The message to show</param>
        public static void Log(string message)
        {
            Log(message, ConsoleColor.White, ConsoleColor.Black);
        }
        /// <summary>
        /// Write an error to the Console and the GuiLog using Red on black colors
        /// </summary>
        /// <param name="E">The error exception to write.</param>
        public static void Log(Exception E)
        {
            Log("[ERROR]: ", ConsoleColor.Red, ConsoleColor.Black);
            Log(E.Message, ConsoleColor.Red, ConsoleColor.Black);
            Log(E.StackTrace, ConsoleColor.Red, ConsoleColor.Black);
        }
        /// <summary>
        /// Write a message to the console and GuiLog using a specified TextColor and BackGround Color
        /// </summary>
        /// <param name="message">The Message to show</param>
        /// <param name="TextColor">The color of the text to show</param>
        /// <param name="BackgroundColor">The color behind the text.</param>
        public static void Log(string message, ConsoleColor TextColor, ConsoleColor BackgroundColor)
        {
            Console.ForegroundColor = TextColor;
            Console.BackgroundColor = BackgroundColor;
            Console.WriteLine(message.PadRight(Console.WindowWidth - 1));
            Console.ResetColor();
        }
        #endregion
	}
}
