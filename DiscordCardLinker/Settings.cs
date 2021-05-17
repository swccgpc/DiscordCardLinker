using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

namespace DiscordCardLinker
{
	public class Settings
	{
		public const string DefaultPath = "./settings.json";

		public string Token { get; set; } = "SET TOKEN HERE";
		public long ClientID { get; set; } = 842629929328836628;
		public long Permissions { get; set; } = 355328;
		public string BaseImageURL { get; set; }
		public string BaseWikiURL { get; set; }
		public string CardFilePath { get; set; } = "cards.tsv";

		public int MaxImagesPerMessage { get; set; }

		public void StoreSettings(string path= DefaultPath)
		{
			string json = JsonConvert.SerializeObject(this, Formatting.Indented);
			File.WriteAllText(path, json);
		}

		public static Settings FromFile(string path= DefaultPath)
		{
			if(!File.Exists(path))
			{
				Console.WriteLine("Settings file does not exist.  Creating...");
				Settings settings = new Settings();

				string dirpath = Path.GetDirectoryName(path);
				if(!string.IsNullOrWhiteSpace(dirpath))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(path));
				}

				settings.StoreSettings(path);
			}
			return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path));
		}
	}
}
