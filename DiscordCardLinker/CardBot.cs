/*
 * DSharpPlus
 * An unofficial .NET wrapper for the Discord API, 
 * based off DiscordSharp, but rewritten to fit the API standards.
 * https://github.com/DSharpPlus/DSharpPlus
 * https://dsharpplus.github.io/
 */
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

using FileHelpers;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordCardLinker {
	public enum MatchType {
		Image,
		Wiki,
		Text
	}

	public class CardBot {

		public Settings CurrentSettings { get; }
		private DiscordClient Client { get; set; }

		private const string squareRegex = @"\[(?!@)(.*?)]";
		private const string curlyRegex = @"{(?!@)(.*?)}";
		private const string angleRegex = @"<(?!@)(.*?)>";
		private const string collInfoRegex = @"\((\d+[\w\+]+\d+\w?)\)";

		private Regex squareCR;
		private Regex curlyCR;
		private Regex angleCR;
		private Regex collInfoCR;

		//Maybe split this into groups: has subtitles, has nicks, etc
		private List<CardDefinition> Cards { get; set; }

		private Dictionary<string, List<CardDefinition>> CardTitles { get; set; }
		private Dictionary<string, List<CardDefinition>> CardSubtitles { get; set; }
		private Dictionary<string, List<CardDefinition>> CardFullTitles { get; set; }
		private Dictionary<string, List<CardDefinition>> CardNicknames { get; set; }
		private Dictionary<string, CardDefinition> CardCollInfo{ get; set; }

		
		private Queue<(string searchString, CardDefinition card)> Cache { get; set; }

		public CardBot(Settings settings) {
			CurrentSettings = settings;
			squareCR = new Regex(squareRegex, RegexOptions.Compiled);
			curlyCR = new Regex(curlyRegex, RegexOptions.Compiled);
			angleCR = new Regex(angleRegex, RegexOptions.Compiled);
			collInfoCR = new Regex(collInfoRegex, RegexOptions.Compiled);

			LoadCardDefinitions();
			Cache = new Queue<(string searchString, CardDefinition card)>();
		}

		public void LoadCardDefinitions() {
			var engine = new FileHelperEngine<CardDefinition>(Encoding.UTF8);
			Cards = engine.ReadFile(CurrentSettings.CardFilePath).ToList();

			CardTitles = new Dictionary<string, List<CardDefinition>>();
			CardSubtitles = new Dictionary<string, List<CardDefinition>>();
			CardFullTitles = new Dictionary<string, List<CardDefinition>>();
			CardNicknames = new Dictionary<string, List<CardDefinition>>();
			CardCollInfo = new Dictionary<string, CardDefinition>();

			string fulltitle = "";

			foreach(var card in Cards) {
				AddEntry(CardTitles, card.Title.ToLower().Trim(), card);
				AddEntry(CardSubtitles, card.Subtitle.ToLower().Trim(), card);
				if(!String.IsNullOrWhiteSpace(card.Subtitle)) {
					fulltitle = $"{card.Title.Trim()}, {card.Subtitle.Trim()}{card.TitleSuffix.Trim()}";
				}
				else {
					fulltitle = $"{card.Title.Trim()}{card.TitleSuffix.Trim()}";
				}
				AddEntry(CardFullTitles, $"{fulltitle.ToLower().Trim()}", card);


				if(!String.IsNullOrWhiteSpace(card.Subtitle)) {
					AddEntry(CardNicknames, GetLongAbbreviation(card.Subtitle.ToLower().Trim()), card);
				}
				else {
					AddEntry(CardNicknames, GetLongAbbreviation(card.Title.ToLower().Trim()), card);
				}

				foreach (string entry in card.Nicknames.Split(",")) {
					AddEntry(CardNicknames, entry.ToLower().Trim(), card);
				}

				CardCollInfo.Add(card.CollInfo.ToLower().Trim(), card);
			}
		}

		private string GetLongAbbreviation(string input) {
			string abbr = new string(
				input.Split(new char[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries)
							.Where(s => s.Length > 0 && char.IsLetter(s[0]))
							.Select(s => s[0])
							.ToArray());

			return abbr;
		}

		/*
		 * Load cards from TSV in to our in-memory index.
		 */
		private void AddEntry(Dictionary<string, List<CardDefinition>> collection, string key, CardDefinition card) {
			/*
			 * Do not add blank or null lines.
			 */
			if (String.IsNullOrWhiteSpace(key)) {
				return;
			}

			/*
			 * Only add the card key once.
			 */
			if(!collection.ContainsKey(key)) {
				Console.WriteLine("Adding Key: ["+key+"]");
				collection.Add(key, new List<CardDefinition>());
			}

			Console.WriteLine("  .. ID.........: "+card.ID);
			Console.WriteLine("  .. ImageURL...: "+card.ImageURL);
			Console.WriteLine("  .. WikiURL....: "+card.WikiURL);
			Console.WriteLine("  .. CollInfo...: "+card.CollInfo);
			Console.WriteLine("  .. DisplayName: "+card.DisplayName);
			Console.WriteLine("  .. Title......: "+card.Title);
			Console.WriteLine("  .. Subtitle...: "+card.Subtitle);
			Console.WriteLine("  .. TitleSuffix: "+card.TitleSuffix);
			Console.WriteLine("  .. Nicknames..: "+card.Nicknames);
			collection[key].Add(card);
		}

		public async Task Initialize() {
			Client = new DiscordClient(new DiscordConfiguration() {
				Token = CurrentSettings.Token,
				TokenType = TokenType.Bot,
				Intents = DiscordIntents.AllUnprivileged, // All intents are granted on discord dev page
				MinimumLogLevel = LogLevel.Debug
			});
			Client.MessageCreated += OnMessageCreated;
			Client.MessageReactionAdded += OnReactionAdded;

			// https://dsharpplus.github.io/api/DSharpPlus.DiscordClient.html?q=ConnectAsync#DSharpPlus_DiscordClient_ConnectAsync_DiscordActivity_System_Nullable_UserStatus__System_Nullable_DateTimeOffset__
			/* connecting to discord */
			await Client.ConnectAsync();
			/* successfully connected to discord */
		}

		/*
		 * process reaction to discord chat message
		 */
		private async Task OnReactionAdded(DiscordClient sender, MessageReactionAddEventArgs e) {

			if (e.Message.Author != Client.CurrentUser)
				return;

			if (e.Message.ReferencedMessage.Author != e.User && !e.Message.ReferencedMessage.Author.IsBot)
				return;

			/*
			 * Check if the user is looking for an Image or a Wiki page.
			 */
			MatchType type;
			if(e.Message.Content.Contains("Found multiple potential candidates for card image")) {
				type = MatchType.Image;
			}
			else if (e.Message.Content.Contains("Found multiple potential candidates for card wiki page")) {
				type = MatchType.Wiki;
			}
			else {
				// It's not an Image or a Wiki page? How odd...
				return;
			}

			/*
			 * Loop through the lines of the message sent to the user originally looking for the line with the emoji on it.
			 * Once the correct emoji line is found, pull the collection code from it.
			 * Do a new search with the collection code and send th results.
			 * Emoji chosen by user: e.Emoji.GetDiscordName()
			 */
			foreach(string line in e.Message.Content.Split("\n")) {

				/*
				 * If the current line contains the emoji, process that line.
				 * Lines look like: ":one: : •Ben Kenobi (SER004)"
				 * The emoji will be ":one:"
				 */
				if (line.Contains(e.Emoji.GetDiscordName())) {
					Console.WriteLine("Line contains "+e.Emoji.GetDiscordName()+":");
					Console.WriteLine(line);
					/*
					 * Use regex to pull the collection code out of the line
					 * Lines look like: ":one: : •Ben Kenobi (SER004)"
					 * The collection code in this case would be SER004
					 */
					string search = Regex.Match(line, @"\(([A-Z0-9]+)\)$").Value.Replace("(", "").Replace(")", "");

					/*
					 * All card indexes are imported as lowercase, so the search must be made to be lowercase as well.
					 * SER004 -> ser004
					 */
					string lowerSearch = search.ToLower().Trim();
					var card = CardCollInfo[lowerSearch];

					/*
					 * Delete the emojis from the message.
					 */
					await e.Message.DeleteAllReactionsAsync();

					/*
					 * Send the image or wiki link back to the user.
					 */
					switch (type) {
						case MatchType.Image:
							await e.Message.ModifyAsync(card.ImageURL);
							break;
						case MatchType.Wiki:
							await e.Message.ModifyAsync("Information about " + card.Title + " is available on scomp at: " + card.WikiURL);
							break;
					} // switch

					/*
					 * Add found card to cache.
					 */
					AddSuccessfulSearch(search, card);

				} // if

			} // foreach

			
		} // OnReactionAdded

		/*
		 * Message created in discord chat, check if we should process it.
		 */
		private async Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs e) {
			/*
			 * Do not process bot originated messages
			 */
			if (e.Message.Author.IsBot)
				return;

			string content = e.Message.Content;

			var requests = new List<(MatchType type, string searchString)>();

			foreach (Match match in curlyCR.Matches(content)) {
				requests.Add((MatchType.Wiki, match.Groups[1].Value));

				//await e.Message.RespondAsync($"Here's a wiki link for ''!");
			}

			foreach (Match match in squareCR.Matches(content)) {
				requests.Add((MatchType.Image, match.Groups[1].Value));
				//await e.Message.RespondAsync($"https://lotrtcgwiki.com/images/LOTR.jpg");
			}

			//foreach (Match match in angleCR.Matches(content))
			//{
			//	requests.Add((MatchType.Text, match.Groups[1].Value));
			//	//await e.Message.RespondAsync($"Here's the text for ''!");
			//}
			foreach (var (type, searchString) in requests) {
				/*
				 * Search for the card requested
				 */
				var candidates = await PerformSearch(searchString);
				if(candidates.Count == 0) {
					await SendNotFound(e, searchString);
				}
				else if(candidates.Count == 1) {
					await SendSingle(e, candidates.First(), type, searchString);
				}
				else {
					string title = candidates.First().Title;
					if(candidates.All(x => x.Title == title)) {
						var cutdown = candidates.Where(x => string.IsNullOrWhiteSpace(x.TitleSuffix)).ToList();
						if(cutdown.Count == 1) {
							await SendSingle(e, cutdown.First(), type, searchString);
							continue;
						}

						string fulltitle = cutdown.First().DisplayName;
						if(cutdown.All(x => x.DisplayName == fulltitle)) {
							await SendSingle(e, cutdown.First(), type, searchString);
							continue;
						}
					}

					/*
					 * If more than one card was found, send a list of the crads with emoji as a method of allowing the 
					 * caller to select one of the cards from the list.
					 */
					await SendCollisions(e, type, searchString, candidates);
				}
			}
				

		}

		private void AddSuccessfulSearch(string search, CardDefinition card) {
			search = search.ToLower().Trim();
			Cache.Enqueue((search, card));
			if(Cache.Count > 100) {
				Cache.Dequeue();
			}
		}

		private async Task<List<CardDefinition>> PerformSearch(string searchString) {
			var candidates = new List<CardDefinition>();

			string lowerSearch = searchString.ToLower().Trim();
			if(Cache.Any(x => x.searchString == searchString)) {
				var card = Cache.Where(x => x.searchString == lowerSearch).First().card;
				candidates.Add(card);
				return candidates;
			}

			if (CardSubtitles.ContainsKey(lowerSearch)) {
				candidates.AddRange(CardSubtitles[lowerSearch]);
				return candidates;
			}

			if (CardCollInfo.ContainsKey(lowerSearch)) {
				candidates.Add(CardCollInfo[lowerSearch]);
				return candidates;
			}

			if (CardFullTitles.ContainsKey(lowerSearch)) {
				candidates.AddRange(CardFullTitles[lowerSearch]);
				return candidates;
			}

			if (CardTitles.ContainsKey(lowerSearch)) {
				candidates.AddRange(CardTitles[lowerSearch]);
				return candidates;
			}

			if (CardNicknames.ContainsKey(lowerSearch)) {
				candidates.AddRange(CardNicknames[lowerSearch]);
				return candidates;
			}

			//TODO: fuzzy search on all of the above
			return candidates;
		}

		private async Task SendSingle(MessageCreateEventArgs e, CardDefinition card, MatchType type, string search) {
			switch (type) {
				case MatchType.Image:
					await SendImage(e, card);
					break;
				case MatchType.Wiki:
					await SendWikiLink(e, card);
					break;
			}

			AddSuccessfulSearch(search, card);
		}

		private async Task SendImage(MessageCreateEventArgs e, CardDefinition card) {
			await e.Message.RespondAsync(card.ImageURL);
		}

		private async Task SendWikiLink(MessageCreateEventArgs e, CardDefinition card) {
			await e.Message.RespondAsync("Information about " + card.Title + " is available on scomp at: " + card.WikiURL);
			//var msg = await new DiscordMessageBuilder()
			//	.With
			//e.Message.RespondAsync()
		}

		/*
		 * If no card found, send a response to the caller telling them you are unable to find the card.
		 */
		private async Task SendNotFound(MessageCreateEventArgs e, string search) {
			string response = $"Sir, I am fluent in 6 million forms of communication. This signal, `{search}`, is not used by the Alliance.";
			await e.Message.RespondAsync(response);
		}

		/*
		 * If more than one card was found, send a list of the crads with emoji as a method of allowing the 
		 * caller to select one of the cards from the list.
		 */
		private async Task SendCollisions(MessageCreateEventArgs e, MatchType type, string search, List<CardDefinition> candidates) {
			string response = "";

			switch (type) {
				case MatchType.Image:
					response += $"Found multiple potential candidates for card image `{search}`.\nReact with the option you'd like to display:\n\n";
					break;
				case MatchType.Wiki:
					response += $"Found multiple potential candidates for card wiki page `{search}`.\nReact with the option you'd like to display:\n\n";
					break;
			}
			

			var menu = new List<string>();

			int count = 1;
			foreach(var card in candidates) {
				string emoji = GetEmoji(count++);
				menu.Add(emoji);

				if(count == 22) {
					response += "\n Maximum menu limit reached.  More cards were found:\n";
				}

				if(count < 22) {
					response += $"\t{emoji} : {card.DisplayName} ({card.CollInfo})\n"; // :one: : •Ben Kenobi (SER004)
				}
				else {
					response += $"\t{card.DisplayName} ({card.CollInfo})\n";
				}

				
			}
			var reply = await e.Message.RespondAsync(response);

			foreach (var option in menu) {
				if (option == "-")
					continue;

				await reply.CreateReactionAsync(DiscordEmoji.FromName(Client, option));
			}
		}

		//There's a limit of 20 reactions to any one message on Discord
		private Dictionary<int, string> IDEmoji = new Dictionary<int, string>() {
			[1] = ":one:",
			[2] = ":two:",
			[3] = ":three:",
			[4] = ":four:",
			[5] = ":five:",
			[6] = ":six:",
			[7] = ":seven:",
			[8] = ":eight:",
			[9] = ":nine:",
			[10] = ":regional_indicator_a:",
			[11] = ":regional_indicator_b:",
			[12] = ":regional_indicator_c:",
			[13] = ":regional_indicator_d:",
			[14] = ":regional_indicator_e:",
			[15] = ":regional_indicator_f:",
			[16] = ":regional_indicator_g:",
			[17] = ":regional_indicator_h:",
			[18] = ":regional_indicator_i:",
			[19] = ":regional_indicator_j:",
			[20] = ":regional_indicator_k:",

		};

		private string GetEmoji(int count) {
			if (count <= 0 || count > 20)
				return "-";

			return IDEmoji[count];
		}
	}
}
