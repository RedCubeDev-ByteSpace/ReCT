using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordRPC;

namespace ReCT_IDE
{
    public class Discord
    {
		public DiscordRpcClient client;

		//Called when your application first starts.
		//For example, just before your main loop, on OnEnable for unity.
		public void Initialize()
		{
			client = new DiscordRpcClient("756774934465871883");


			//Subscribe to events
			client.OnReady += (sender, e) =>
			{
				Console.WriteLine("Received Ready from user {0}", e.User.Username);
			};

			client.OnPresenceUpdate += (sender, e) =>
			{
				Console.WriteLine("Received Update! {0}", e.Presence);
			};

			//Connect to the RPC
			client.Initialize();
		}
	}
}
