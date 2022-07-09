using DSharpPlus;

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordCardLinker {
	class Program {
		static async Task Main(string[] args) {
			Console.WriteLine("Starting up, the Bot!");
			CardBot bot = new CardBot(Settings.FromFile());
			Console.WriteLine("Initializing Bot (asynchronously)");
			await bot.Initialize();

			await Task.Delay(-1);

			//cache of the last 100 requests
			//load up spreadsheet into memory
			// analyze each message for the brackets around identifiers
			// search according to priority: title, subtitle, nickname, longabbr, shortabbr
			// when an ambiguity is found, ask the user to choose from options with reacts
		}
	}
}
