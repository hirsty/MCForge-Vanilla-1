using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Net;

namespace MCForge.Beat {
	public static class Heart {
		//static int _timeout = 60 * 1000;

		private static int max_retries = 3;

		static string hash = null;
		public static string serverURL;
		static string DefaultParameters;
		//static string players = "";
		//static string worlds = "";

		//static BackgroundWorker worker;
		static Random MCForgeBeatSeed = new Random(Process.GetCurrentProcess().Id);
		static StreamWriter beatlogger;

		static System.Timers.Timer MinecraftBeatTimer = new System.Timers.Timer(500);
		static System.Timers.Timer MCForgeBeatTimer;

		static object Lock = new object();

		public static void Init() {
			if (ServerSettings.logbeat) {
				if (!File.Exists("heartbeat.log")) {
					File.Create("heartbeat.log").Close();
				}
			}
			//MCForgeBeatTimer = new System.Timers.Timer(1000 + MCForgeBeatSeed.Next(0, 2500));
			DefaultParameters = "port=" + ServerSettings.port +
							"&max=" + ServerSettings.MaxPlayers +
							"&name=" + UrlEncode(ServerSettings.NAME) +
							"&public=" + ServerSettings.isPublic +
							"&version=" + ServerSettings.version;

			Thread backupThread = new Thread(new ThreadStart(delegate {
				MinecraftBeatTimer.Elapsed += delegate {
					MinecraftBeatTimer.Interval = 50000;
					try {
						Pump(new MinecraftBeat());
						//Pump(new WOMBeat());
					} catch (Exception e) { Server.Log(e); }
				};
				MinecraftBeatTimer.Start();

				Thread.Sleep(5000);

				//MCForgeBeatTimer.Elapsed += delegate {
				//    MCForgeBeatTimer.Interval = 10 * 60 * 1000; // 10 minutes
				//    try {
				//        Pump(new MCForgeBeat());
				//    } catch (Exception e) {
				//        Server.ErrorLog(e);
				//    }
				//};
				//MCForgeBeatTimer.Start();

				System.Timers.Timer WomBeat = new System.Timers.Timer(500);
			}));
			backupThread.Start();
		}

		public static bool Pump(Beat beat) {
			//lock (Lock)
			//{
			String beattype = beat.GetType().Name;

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(beat.URL));

			beat.Parameters = DefaultParameters;

			if (beat.Log) {
				beatlogger = new StreamWriter("heartbeat.log", true);
			}

			int totalTries = 0;
			int totalTriesStream = 0;

		retry: try {
				totalTries++;
				totalTriesStream = 0;

				beat.Prepare();
				if (beat.GetType() == typeof(MinecraftBeat))
					File.WriteAllText("text/heartbeaturl.txt", beat.URL + "?" + beat.Parameters, Encoding.UTF8);

				// Set all the request settings
				//Server.s.Log(beat.Parameters);
				request.Method = "POST";
				request.ContentType = "application/x-www-form-urlencoded";
				request.UserAgent = "Opera/9.23 (Nintendo Wii; U; ; 1038-58; Wii Internet Channel/1.0; en)"; // LOLOLOLOLOLOL
				request.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
				byte[] formData = Encoding.ASCII.GetBytes(beat.Parameters);
				request.ContentLength = formData.Length;
				request.Timeout = 15000; // 15 seconds

	  retryStream: try {
					totalTriesStream++;
					using (Stream requestStream = request.GetRequestStream()) {
						requestStream.Write(formData, 0, formData.Length);
						if (ServerSettings.logbeat && beat.Log) {
							BeatLog(beat, beattype + " request sent at " + DateTime.Now.ToString());
						}
						requestStream.Flush();
						requestStream.Close();
					}
				} catch (WebException e) {
					//Server.ErrorLog(e);
					if (e.Status == WebExceptionStatus.Timeout) {
						if (ServerSettings.logbeat && beat.Log) {
#if DEBUG
							Server.Log(beattype + " timeout detected at " + DateTime.Now.ToString());
#endif
							BeatLog(beat, beattype + " timeout detected at " + DateTime.Now.ToString());
						}
						if (totalTriesStream < max_retries) {
							goto retryStream;
						} else {
							if (ServerSettings.logbeat && beat.Log)
								BeatLog(beat, beattype + " timed out " + max_retries + " times. Aborting this request. " + DateTime.Now.ToString());
							Server.Log(beattype + " timed out " + max_retries + " times. Aborting this request.");
							//throw new WebException("Failed during request.GetRequestStream()", e.InnerException, e.Status, e.Response);
							beatlogger.Close();
							return false;
						}
					} else if (ServerSettings.logbeat && beat.Log) {
#if DEBUG
						Server.Log(beattype + " non-timeout exception detected: " + e.Message);
#endif
						BeatLog(beat, beattype + " non-timeout exception detected: " + e.Message);
						BeatLog(beat, "Stack Trace: " + e.StackTrace);
					}
				}

				//if (hash == null)
				//{
				using (WebResponse response = request.GetResponse()) {
					using (StreamReader responseReader = new StreamReader(response.GetResponseStream())) {
						if (ServerSettings.logbeat && beat.Log) {
#if DEBUG
							Server.Log(beattype + " response received at " + DateTime.Now.ToString());
#endif
							BeatLog(beat, beattype + " response received at " + DateTime.Now.ToString());
						}

						if (String.IsNullOrEmpty(hash) && response.ContentLength > 0) {
							// Instead of getting a single line, get the whole damn thing and we'll strip stuff out
							string line = responseReader.ReadToEnd().Trim();
							if (ServerSettings.logbeat && beat.Log) {
								BeatLog(beat, "Received: " + line);
							}

							beat.OnPump(line);
						} else {
							beat.OnPump(String.Empty);
						}
					}
				}
			} catch (WebException e) {
				if (e.Status == WebExceptionStatus.Timeout) {
					if (ServerSettings.logbeat && beat.Log) {
#if DEBUG
						Server.Log(beattype + " timeout detected at " + DateTime.Now.ToString());
#endif
						BeatLog(beat, "Timeout detected at " + DateTime.Now.ToString());
					}
					Pump(beat);
				}
			} catch (Exception) {
				if (ServerSettings.logbeat && beat.Log) {
					BeatLog(beat, beattype + " failure #" + totalTries + " at " + DateTime.Now.ToString());
				}
				if (totalTries < max_retries) goto retry;
				if (ServerSettings.logbeat && beat.Log) {
#if DEBUG
					Server.Log(beattype + " failed " + max_retries + " times.  Stopping.");
#endif
					BeatLog(beat, "Failed " + max_retries + " times.  Stopping.");
					beatlogger.Close();
				}
				return false;
			} finally {
				request.Abort();
			}
			if (beatlogger != null) {
				beatlogger.Close();
			}
			//}
			return true;
		}

		public static string UrlEncode(string input) {
			StringBuilder output = new StringBuilder();
			for (int i = 0; i < input.Length; i++) {
				if ((input[i] >= '0' && input[i] <= '9') ||
					(input[i] >= 'a' && input[i] <= 'z') ||
					(input[i] >= 'A' && input[i] <= 'Z') ||
					input[i] == '-' || input[i] == '_' || input[i] == '.' || input[i] == '~') {
					output.Append(input[i]);
				} else if (Array.IndexOf<char>(reservedChars, input[i]) != -1) {
					output.Append('%').Append(((int)input[i]).ToString("X"));
				}
			}
			return output.ToString();
		}

		private static void BeatLog(Beat beat, string text) {
			if (ServerSettings.logbeat && beat.Log && beatlogger != null) {
				try {
					beatlogger.WriteLine(text);
				} catch { }
			}
		}

		public static char[] reservedChars = { ' ', '!', '*', '\'', '(', ')', ';', ':', '@', '&',
                                                 '=', '+', '$', ',', '/', '?', '%', '#', '[', ']' };
	}

	public enum BeatType { Minecraft, TChalo, MCForge }
}
