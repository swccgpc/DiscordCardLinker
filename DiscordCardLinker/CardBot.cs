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

		private const string squareRegex = @"\[\[(?!@)(.*?)]]";
		private const string aliasRegex = @"(.*?)\s*\((.*)\)";
		private const string abbreviationReductionRegex = @"[^\w\s]+";
		private const string stripNonWordsRegex = @"\W+";

		private Regex squareCR;
		private Regex aliasCR;
		private Regex abbreviationReductionCR;
		private Regex stripNonWordsCR;

		//Maybe split this into groups: has subtitles, has nicks, etc
		private List<CardDefinition> Cards { get; set; }

		private bool Loading { get; set; }

		private Dictionary<string, List<CardDefinition>> CardTitles { get; set; }
		private Dictionary<string, List<CardDefinition>> CardSubtitles { get; set; }
		private Dictionary<string, List<CardDefinition>> CardFullTitles { get; set; }
		private Dictionary<string, List<CardDefinition>> CardNicknames { get; set; }
		private Dictionary<string, CardDefinition> CardCollInfo{ get; set; }

		//When presenting the dropdown, a card reference is provided.  This is used on the initial presentation,
		// which has no image (this could theoretically be a placeholder, but what's the point of that).
		private CardDefinition NullCard { get; } = new CardDefinition() {			
			ImageURL = ""
		};


		public CardBot(Settings settings) {
			CurrentSettings = settings;
			squareCR = new Regex(squareRegex, RegexOptions.Compiled);
			aliasCR = new Regex(aliasRegex, RegexOptions.Compiled);
			abbreviationReductionCR = new Regex(abbreviationReductionRegex, RegexOptions.Compiled);
			stripNonWordsCR = new Regex(stripNonWordsRegex, RegexOptions.Compiled);

			LoadCardDefinitions();
		}

		/*
		 * Loops through cards.tsv and converts each row to a card definition.  Each card definition is then inserted
		 * into several dictionaries, with the keys being possible search terms used to look up that card.
		 */
		public void LoadCardDefinitions() {

			Loading = true;

			var engine = new FileHelperEngine<CardDefinition>(Encoding.UTF8);
			Cards = engine.ReadFile(CurrentSettings.CardFilePath).ToList();

			CardTitles = new Dictionary<string, List<CardDefinition>>();
			CardSubtitles = new Dictionary<string, List<CardDefinition>>();
			CardFullTitles = new Dictionary<string, List<CardDefinition>>();
			CardNicknames = new Dictionary<string, List<CardDefinition>>();
			CardCollInfo = new Dictionary<string, CardDefinition>();


			foreach(var card in Cards) {
				if (string.IsNullOrWhiteSpace(card.ID) || string.IsNullOrWhiteSpace(card.CollInfo))
					continue;
				Console.WriteLine("Adding card to search...");
				Console.WriteLine($"  .. ID.........: {card.ID}");
				Console.WriteLine($"  .. ImageURL...: {card.ImageURL}");
				Console.WriteLine($"  .. WikiURL....: {card.WikiURL}");
				Console.WriteLine($"  .. CollInfo...: {card.CollInfo}");
				Console.WriteLine($"  .. DisplayName: {card.DisplayName}");
				Console.WriteLine($"  .. Title......: {card.Title}");
				Console.WriteLine($"  .. Subtitle...: {card.Subtitle}");
				Console.WriteLine($"  .. TitleSuffix: {card.TitleSuffix}");
				Console.WriteLine($"  .. Nicknames..: {card.Nicknames}");

				var collinfo = ScrubInput(card.CollInfo);
				if (!CardCollInfo.ContainsKey(collinfo))
				{
					CardCollInfo.Add(ScrubInput(card.CollInfo), card);
				}
				else
				{
					Console.Error.WriteLine($"Collector's Info collision: {collinfo} provided by {card.ID} but previously entered by {CardCollInfo[collinfo].ID}");
					continue;
				}

				//Luke Skywalker
				AddEntry(CardTitles, ScrubInput(card.Title), card);
				//Jedi Knight
				AddEntry(CardSubtitles, ScrubInput(card.Subtitle), card);

				string fulltitle = $"{card.Title}{card.Subtitle}{card.TitleSuffix}";
				//Luke Skywalker, Jedi Knight (V)
				AddEntry(CardFullTitles, ScrubInput(fulltitle), card);

				if (!String.IsNullOrWhiteSpace(card.Subtitle) && !String.IsNullOrWhiteSpace(card.TitleSuffix))
				{
					fulltitle = $"{card.Subtitle}{card.TitleSuffix}";
					AddEntry(CardFullTitles, ScrubInput(fulltitle), card);
				}

				string abbr = "";

				if (!String.IsNullOrWhiteSpace(card.Subtitle))
				{
					//JK
					abbr = GetLongAbbreviation(card.Subtitle);
					AddEntry(CardNicknames, abbr, card);
					//Luke Skywalker JK
					string titleAbbr = ScrubInput($"{card.Title}{abbr}");
					AddEntry(CardNicknames, titleAbbr, card);

					if (card.Title.Contains(" "))
					{
						foreach (string sub in card.Title.Split(" "))
						{
							if (sub.ToLower() == "the" || sub.ToLower() == "of")
								continue;
							//Luke JK
							string subAbbr = ScrubInput($"{sub}{abbr}");
							AddEntry(CardNicknames, subAbbr, card);
						}
					}

					if (!String.IsNullOrWhiteSpace(card.TitleSuffix))
					{
						//Jedi Knight (V)
						fulltitle = $"{card.Subtitle}{card.TitleSuffix}";
						AddEntry(CardFullTitles, ScrubInput(fulltitle), card);
						//JK (V)
						fulltitle = $"{abbr}{card.TitleSuffix}";
						AddEntry(CardFullTitles, ScrubInput(fulltitle), card);
					}

					//LSJK
					abbr = GetLongAbbreviation($"{card.Title} {card.Subtitle}");
					AddEntry(CardNicknames, abbr, card);
				}

				//LS
				abbr = GetLongAbbreviation(card.Title);
				AddEntry(CardNicknames, abbr, card);

				if (!String.IsNullOrWhiteSpace(card.TitleSuffix))
				{
					//LS (V)
					fulltitle = $"{abbr}{card.TitleSuffix}";
					AddEntry(CardFullTitles, ScrubInput(fulltitle), card);
				}

				foreach (string entry in card.Nicknames.Split(",")) {
					if (String.IsNullOrWhiteSpace(entry))
						continue;

					string nick = ScrubInput(entry);
					//Shotgun
					AddEntry(CardNicknames, nick, card);

					if (!String.IsNullOrWhiteSpace(card.TitleSuffix))
					{
						//Shotgun (V)
						fulltitle = $"{nick}{card.TitleSuffix}";
						AddEntry(CardFullTitles, ScrubInput(fulltitle), card);
					}
				}
			}

			Loading = false;
		}

		/*
		 * This produces a version of the card title with all spaces, punctuation, and sundry all removed from the lookup.
		 * This means that user typos that omit spaces, punctuation, or sundry will not be defeated for completely predictable reasons.
		 * This is also used to transform search queries to match the same search form.
		 */
		private string ScrubInput(string input, bool stripSymbols = true)
		{
			string output = input.ToLower();
			output = output.Trim();
			if (stripSymbols)
			{
				output = stripNonWordsCR.Replace(output, "");
			}

			return output;
		}

		/*
		 * Used to turn a card name like "Darth Vader, Dark Lord of the Sith" into "dvdlots". Treats hyphens as a 
		 * space for abbreviation purposes, so "Obi-wan Kenobi" is abbreviated as "owk" instead of just "ok".
		 */
		private string GetLongAbbreviation(string input) {
			input = input.ToLower().Trim();
			input = input.Replace("-", " ");
			input = abbreviationReductionCR.Replace(input, "");
			string abbr = new string(
				input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
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
			Client.ComponentInteractionCreated += OnUIControlInteracted;
			Client.ThreadCreated += OnThreadCreated;
			Client.ThreadUpdated += OnThreadUpdated;

			// https://dsharpplus.github.io/api/DSharpPlus.DiscordClient.html?q=ConnectAsync#DSharpPlus_DiscordClient_ConnectAsync_DiscordActivity_System_Nullable_UserStatus__System_Nullable_DateTimeOffset__
			/* connecting to discord */
			await Client.ConnectAsync();
			/* successfully connected to discord */
		}

		/*
		 * Ensures that any existing threads that were created while the bot was offline (or unaware of threads)
		 * will be subscribed to by the bot whenever someone posts in that thread (or changes its status).
		 */
		private async Task OnThreadUpdated(DiscordClient sender, ThreadUpdateEventArgs e)
		{
			await e.ThreadAfter.JoinThreadAsync();
		}

		/*
		 * Automatically joins any new threads that are created while the bot is online.
		 */
		private async Task OnThreadCreated(DiscordClient sender, ThreadCreateEventArgs e)
		{
			await e.Thread.JoinThreadAsync();
		}

		/*
		 * Handles the behavior of the bot whenever a button or dropdown is interacted with.  
		 * Each control is instantiated with an ID, which we treat as a vehicle for the user ID of the summoner and an action code 
		 * for the behavior the bot should be performing:
		 *  - "delete" indicates the Delete button was pressed for the bot to self-delete a response
		 *  - "lockin" is the Accept button, which removes any dropdowns and buttons (except the wiki button) and is for the user to
		 *	  communicate that the correct card was found.
		 *	- "dropdown" indicates that the user changed the active selection.
		 */
		private async Task OnUIControlInteracted(DiscordClient sender, ComponentInteractionCreateEventArgs e)
		{
			var match = Regex.Match(e.Id, @"(\w+?)_(.*)");
			string buttonId = match.Groups[1].Value;
			ulong authorId = Convert.ToUInt64(match.Groups[2].Value);

			switch (buttonId)
			{
				case "delete":
					if (authorId == 0 || e.User.Id == authorId || e.Message.Reference.Message?.Author?.Id == e.User.Id || e.Guild.OwnerId == e.User.Id)
					{
						await e.Message.DeleteAsync();
					}
					break;

				case "lockin":
					if (authorId == 0 || e.User.Id == authorId || e.Message.Reference.Message?.Author?.Id == e.User.Id || e.Guild.OwnerId == e.User.Id)
					{
						var builder = new DiscordMessageBuilder()
						.WithContent(e.Message.Content);

						var comps = e.Message.Components.First().Components.ToList();
						var newButtons = new List<DiscordComponent>();
						foreach (var comp in comps)
						{
							if (!String.IsNullOrWhiteSpace(comp.CustomId))
								continue;
							builder.AddComponents(comp);
						}

						await e.Message.ModifyAsync(builder);
						await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage);
					}

					break;

				case "dropdown":
					if (authorId == 0 || e.User.Id == authorId || e.Message.Reference.Message?.Author?.Id == e.User.Id || e.Guild.OwnerId == e.User.Id)
					{
						var card = CardCollInfo[ScrubInput(e.Values.First())];

						var dbuilder = BuildSingle(e.Message, card, true, true, false);

						var dropdown = e.Message.Components.Last().Components.First();
						var buttons = e.Message.Components.First().Components.ToList();

						dbuilder.WithContent(card.ImageURL)
							.AddComponents(dropdown);

						await e.Message.ModifyAsync(dbuilder);
						await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage);
					}

					break;

				default:
					break;
			}
		}

		/*
		 * Message created in discord chat, check if we should process it.
		 */
		private async Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs e) {
			if (Loading) {
				await Task.Delay(1000);
				if (Loading)
					return;
			}
			/*
			 * Do not process bot originated messages
			 */
			if (e.Message.Author.IsBot)
				return;

			string content = e.Message.Content;

			var requests = new List<(MatchType type, string searchString)>();

			foreach (Match match in squareCR.Matches(content)) {
				requests.Add((MatchType.Image, match.Groups[1].Value));
			}

			foreach (var (type, searchString) in requests) {
				/*
				 * Search for the card requested
				 */
				var candidates = PerformSearch(searchString);
				if(candidates.Count == 0) {
					await SendNotFound(e, searchString);
				}
				else if(candidates.Count == 1) {
					await SendImage(e.Message, candidates.First());
				}
				else {

					/*
					 * If more than one card was found, send a list of the crads in a dropdown as a method of allowing the 
					 * caller to select one of the cards from the list.
					 */
					await SendCollisions(e, type, searchString, candidates);
				}
			}
		}

		/*
		 * Given a scrubbed search query, searches through each dictionary for a key matching the input.
		 * The dictionaries are separate to create an implied order of priority, where a higher priority
		 * match takes precedence over a lower priority one, although this difference is more tenuous in
		 * this version of the bot than in LOTR.  This separation could be made more useful if the csv
		 * data itself was improved somewhat.
		 */
		private HashSet<CardDefinition> PerformSearch(string searchString) {
			var candidates = new HashSet<CardDefinition>();

			string lowerSearch = ScrubInput(searchString);

			if (CardCollInfo.ContainsKey(lowerSearch)) {
				candidates.Add(CardCollInfo[lowerSearch]);
			}

			if (CardFullTitles.ContainsKey(lowerSearch)) {
				candidates.AddRange(CardFullTitles[lowerSearch]);
			}

			if (CardTitles.ContainsKey(lowerSearch)) {
				candidates.AddRange(CardTitles[lowerSearch]);
			}

			if (CardSubtitles.ContainsKey(lowerSearch))
			{
				candidates.AddRange(CardSubtitles[lowerSearch]);
			}

			if (CardNicknames.ContainsKey(lowerSearch)) {
				candidates.AddRange(CardNicknames[lowerSearch]);
			}

			if (lowerSearch.Length > 2)
			{
				foreach (var key in CardTitles.Keys)
				{
					if (key.Contains(lowerSearch))
					{
						candidates.AddRange(CardTitles[key]);
					}
				};

				foreach (var key in CardSubtitles.Keys)
				{
					if (key.Contains(lowerSearch))
					{
						candidates.AddRange(CardSubtitles[key]);
					}
				};

				foreach (var key in CardFullTitles.Keys)
				{
					if (key.Contains(lowerSearch))
					{
						candidates.AddRange(CardFullTitles[key]);
					}
				};

				foreach (var key in CardNicknames.Keys)
				{
					if (key.Contains(lowerSearch))
					{
						candidates.AddRange(CardNicknames[key]);
					}
				};
			}
			return candidates;
		}

		/*
		 * Helper function for generating a consistent Delete button, which uses the ID to store the original
		 * author of the summons as well as which action to perform when interacted with.
		 */
		private DiscordButtonComponent DeleteButton(ulong AuthorID)
		{
			return new DiscordButtonComponent(ButtonStyle.Danger, $"delete_{AuthorID}", "Delete");
		}

		/*
		 * Helper function for generating a consistent Accept button, which uses the ID to store the original
		 * author of the summons as well as which action to perform when interacted with.
		 */
		private DiscordButtonComponent LockinButton(ulong AuthorID, bool disabled = false)
		{
			return new DiscordButtonComponent(ButtonStyle.Primary, $"lockin_{AuthorID}", "Accept", disabled);
		}

		/*
		 * Responds to the summons with a message consisting only of a card URL, which should automatically embed
		 * as an image in Discord.  Also ensures that a discreet wiki link button is presented.
		 */
		private async Task SendImage(DiscordMessage original, CardDefinition card)
		{
			await original.RespondAsync(BuildSingle(original, card, true, true, false));
		}

		/*
		 * Handles the construction of a well-formed response to a summons.
		 */
		private DiscordMessageBuilder BuildSingle(DiscordMessage original, CardDefinition card, bool wiki, bool buttons, bool disable)
		{
			var builder = new DiscordMessageBuilder()
				.WithReply(original.Id)
				.WithContent(card.ImageURL);

			var comps = new List<DiscordComponent>();

			if (wiki)
			{
				comps.Add(new DiscordLinkButtonComponent(card.WikiURL, "scomp", false));
			}
			if (buttons)
			{
				comps.Add(LockinButton(original.Author.Id, disable));
				comps.Add(DeleteButton(original.Author.Id));
			}

			return builder.AddComponents(comps);
		}

		/*
		 * If no card found, send a response to the caller telling them you are unable to find the card.
		 */
		private async Task SendNotFound(MessageCreateEventArgs e, string search) {
			string response = $"Sir, I am fluent in 6 million forms of communication. This signal, `{search}`, is not used by the Alliance.";
			var builder = new DiscordMessageBuilder()
				.WithReply(e.Message.Id)
				.WithContent(response)
				.AddComponents(DeleteButton(0));

			await e.Message.RespondAsync(builder);
		}

		/*
		 * If more than one card was found, send a list of the crads, using a rich drop-down for the user to select
		 */
		private const string LengthMessage = ". . . .\n\n**Too many results to list**! Try a more specific query.";
		private async Task SendCollisions(MessageCreateEventArgs e, MatchType type, string search, IEnumerable<CardDefinition> candidates)
		{
			string response = $"Found multiple potential candidates for card image `{search}`.";
			if (candidates.Count() > 25) {
				response += $"\nFound {candidates.Count()} options.  The top 25 are shown below, but you may need to try a more specific query.\n";
			}
			response += "\nSelect your choice from the dropdown below:\n\n";

			var menu = new List<string>();

			var options = candidates
				.Take(25)
				.Select(x => new DiscordSelectComponentOption($"{x.DisplayName} ({x.CollInfo})", x.CollInfo));

			var dropdown = new DiscordSelectComponent($"dropdown_{e.Message.Author.Id}", null, options);

			var builder = BuildSingle(e.Message, NullCard, false, true, true)
				.WithReply(e.Message.Id)
				.WithContent(response)
				.AddComponents(dropdown);

			await e.Message.RespondAsync(builder);

		}
	}
}
