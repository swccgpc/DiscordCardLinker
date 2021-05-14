using DSharpPlus;

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordCardLinker
{
	class Program
	{
		static async Task Main(string[] args)
		{
			CardBot bot = new CardBot(Settings.FromFile());
			await bot.Initialize();

			await Task.Delay(-1);

			//cache of the last 100 requests
			//load up spreadsheet into memory
			// analyze each message for the brackets around identifiers
			// search according to priority: title, subtitle, nickname, longabbr, shortabbr
		}
	}
}
