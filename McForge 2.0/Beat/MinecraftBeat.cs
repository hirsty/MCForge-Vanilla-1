using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCForge.Beat {
	class MinecraftBeat : Beat {
		public string URL { get { return "http://www.minecraft.net/heartbeat.jsp"; } }
		public string Parameters { get; set; }
		public bool Log { get { return false; } }

		public void Prepare() {
			Parameters += "&salt=" + ServerSettings.salt +
				"&users=" + Player.currNum;
		}

		public void OnPump(string line) {
			// Only run the code below if we receive a response
			if (!String.IsNullOrEmpty(line.Trim())) {
				string newHash = line.Substring(line.LastIndexOf('/') + 1);

				// Run this code if we don't already have a hash or if the hash has changed
				if (String.IsNullOrEmpty(Server.Hash) || !newHash.Equals(Server.Hash)) {
					Server.Hash = newHash;
					Server.URL = line;

					//serverURL = "http://" + serverURL.Substring(serverURL.IndexOf('.') + 1);
					//Server.UpdateUrl(Server.URL);
					//File.WriteAllText("text/externalurl.txt", Server.URL);
					Server.Log("URL found: " + Server.URL);
				}
			}
		}
	}
}
