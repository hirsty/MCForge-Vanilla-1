using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCForge;

namespace CommandDll.Moderation {
	public class CmdKick : ICommand {
		string _Name = "Kick";
		public string Name { get { return _Name; } }

		CommandTypes _Type = CommandTypes.mod;
		public CommandTypes Type { get { return _Type; } }

		string _Author = "Nerketur";
		public string Author { get { return _Author; } }

		int _Version = 1;
		public int Version { get { return _Version; } }

		string _CUD = "";
		public string CUD { get { return _CUD; } }

		string[] CommandStrings = new string[1] { "kick" };

		public void Use(Player p, string[] args) {
			if (args.Length == 0) {
				//Kick the user
				p.Kick("Congrats!  You kicked yourself!");
			} else {
				//assume first argument is name, andd rest is message
				//First, see if the specified user is on.
				Player kickee = Player.Find(args[0]);
				if (kickee == null) {
					p.SendMessage("Sorry, but the specified player is not online!");
					return;
				}
				String reason;
				StringBuilder sb = new StringBuilder();
				foreach (String s in args.Skip(1)) {
					sb.Append(s);
					if (s != args[1]) {
						sb.Append(" ");
					}
				}
				if (sb.Length == 0)
					reason = "You were kicked by " + p.USERNAME;
				else
					reason = sb.ToString();
				kickee.Kick(reason);
			}
		}

		public void Help(Player p) {
			p.SendMessage("/kick [username [message]] - Kicks a username from the server");
			p.SendMessage("/kick - Kicks YOU from the server.");
		}

		public void Initialize() {
			Command.AddReference(this, CommandStrings);
		}
	}
}
