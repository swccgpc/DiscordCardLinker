using System;
using System.Collections.Generic;
using System.Text;

using FileHelpers;

namespace DiscordCardLinker {
	[DelimitedRecord("\t")]
	[IgnoreFirst]
	public class CardDefinition {
		[FieldTrim(TrimMode.Both)]
		public string ID;
		[FieldTrim(TrimMode.Both)]
		public string ImageURL;
		[FieldTrim(TrimMode.Both)]
		public string WikiURL;
		[FieldTrim(TrimMode.Both)]
		public string CollInfo;

		[FieldTrim(TrimMode.Both)]
		public string DisplayName;
		[FieldTrim(TrimMode.Both)]
		public string Title;
		[FieldTrim(TrimMode.Both)]
		public string Subtitle;
		[FieldTrim(TrimMode.Both)]
		public string TitleSuffix;

		public string Nicknames;
	}
}
