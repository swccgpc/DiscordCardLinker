using System;
using System.Collections.Generic;
using System.Text;

using FileHelpers;

namespace DiscordCardLinker
{
	[DelimitedRecord("\t")]
	[IgnoreFirst]
	public class CardDefinition
	{
		public string ID;
		public string ImageURL;
		public string WikiURL;
		public string CollInfo;

		public string DisplayName;
		public string Title;
		public string Subtitle;
		public string TitleSuffix;

		public string Nicknames;
		public string Personas;
	}
}
