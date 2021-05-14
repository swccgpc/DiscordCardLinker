using System;
using System.Collections.Generic;
using System.Text;

using FileHelpers;

namespace DiscordCardLinker
{
	[DelimitedRecord("\t")]
	public class CardDefinition
	{
		public string ID;
		public string ImageURL;
		public string WikiURL;
		public string Title;
		public string Subtitle;
		public string CollInfo;
		public string ShortAbbr;
		public string LongAbbr;
		public string Nicknames;
	}
}
