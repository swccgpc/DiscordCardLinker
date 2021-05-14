using DSharpPlus;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordCardLinker
{
	public class CardBot
	{

		public Settings CurrentSettings { get; }
		private DiscordClient Client { get; set; }

		private const string squareRegex = @"\[(.*)]";
		private const string curlyRegex = @"{(.*)}";
		private const string angleRegex = @"<(.*)>";

		private Regex squareCR;
		private Regex curlyCR;
		private Regex angleCR;

		public CardBot(Settings settings)
		{
			CurrentSettings = settings;
			squareCR = new Regex(squareRegex, RegexOptions.Compiled);
			curlyCR = new Regex(curlyRegex, RegexOptions.Compiled);
			angleCR = new Regex(angleRegex, RegexOptions.Compiled);
		}

		public async Task Initialize()
		{
			Client = new DiscordClient(new DiscordConfiguration()
			{
				Token = CurrentSettings.Token,
				TokenType = TokenType.Bot,
				Intents = DiscordIntents.AllUnprivileged,
				MinimumLogLevel = LogLevel.Debug
			});

			Client.MessageCreated += OnMessageCreated;

			await Client.ConnectAsync();
		}

		private async Task OnMessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs e)
		{
			if (e.Message.Author.IsBot)
				return;

			string content = e.Message.Content;

			if (squareCR.IsMatch(content))
			{
				foreach (Match match in squareCR.Matches(content))
				{
					string search = match.Groups[1].Value;
					await e.Message.RespondAsync($"Here's an image of '{search}'!");
				}
			}

			if (squareCR.IsMatch(content))
			{
				foreach (Match match in curlyCR.Matches(content))
				{
					string search = match.Groups[1].Value;
					await e.Message.RespondAsync($"Here's a wiki link for of '{search}'!");
				}
			}

			if (squareCR.IsMatch(content))
			{
				foreach (Match match in angleCR.Matches(content))
				{
					string search = match.Groups[1].Value;
					await e.Message.RespondAsync($"Here's the text for '{search}'!");
				}
			}
		}
	}
}
