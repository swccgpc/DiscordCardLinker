﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Newtonsoft.Json;

namespace DiscordCardLinker
{
	public class Settings
	{
		public const string DefaultPath = "./settings.json";

		public string Token { get; set; }            = Environment.GetEnvironmentVariable("TOKEN");
		public string CardFilePath { get; set; }     = "cards.tsv";
		public string GoogleSheetID { get; set; }    //= Environment.GetEnvironmentVariable("GOOGLESHEETID");

		public void StoreSettings(string path= DefaultPath)
		{
			string json = JsonConvert.SerializeObject(this, Formatting.Indented);
			Console.WriteLine(json);
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
