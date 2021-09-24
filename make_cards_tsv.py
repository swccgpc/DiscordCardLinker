#!/usr/bin/env python3

import json
import os



def parse_json_file(json_filename):

  with open(json_filename) as json_data:
    side_json = json.load(json_data)

  for card in side_json["cards"]:
    nicknames  = []
    card_id    = str(card["id"])
    img_url    = card["front"]["imageUrl"]
    http_url   = card["front"]["imageUrl"]+"?need_to_add_direct_link_capability_to_scomp"
    gempid     = card["gempId"].split("_")
    #collecting = card["gempId"].replace("_", card["rarity"])
    #collecting = card["gempId"].replace("_", card["rarity"]) + " ("+release_sets[card["set"]]["abbr"]+card["rarity"]+gempid[1]+")"
    collecting = release_sets[card["set"]]["abbr"]+card["rarity"]+gempid[1]
    title      = card["front"]["title"]
    nicknames.append(title)
    nicknames.append(title.replace("•", ""))
    name       = title.replace("•", "").replace(" (V)", "").replace("<>", "").replace(" (AI)", "").replace("'", "")
    card_type  = card["front"]["type"]
    ##
    ## for names like: Obi-Wan Kenobi, Jedi Knight
    ## use the data after the comma to be the subtitle
    ##
    ## for names like: Malachor: Sith Temple Upper Chamber
    ## use the data after the colon to be the subtitle
    ##
    if ("," in name):
      names      = name.split(",")
      subtitle   = names[len(names)-1]
    else:
      names      = name.split(":")
      subtitle   = names[0]
    

    title_suffix = ""
    if ("(AI)" in title):
      title_suffix = "(AI)"
    elif ("(V)" in title):
      title_suffix = "(V)"


    if (("Obi-Wan" in title) or ("Kenobi" in title)):
      nicknames.append("Obi-Wan")
      nicknames.append("Kenobi")
      nicknames.append("General Kenobi")
      nicknames.append("Hello There")


    if (collecting in known_ids):
      print("\n\n!!!! " + card_id + "\t" + collecting + "\t" + title)
      print("!!!! " + known_ids[collecting] + "\n\n")
    else:
      known_ids[collecting] = card_id + "\t" + collecting + "\t" + title

      out = card_id + "\t" + img_url + "\t" + http_url + "\t" + collecting + "\t" + title + "\t" + name + "\t" + subtitle + "\t" + title_suffix + "\t" + ",".join(nicknames)
      print(card_id + "\t" + collecting + "\t" + title)
      fh.write(out+"\n")




##
## Download the json files if they do not exist locally
##
if (not os.path.isfile("Dark.json")):
  print(os.popen("curl -O https://raw.githubusercontent.com/swccgpc/swccg-card-json/main/Dark.json").read())
if (not os.path.isfile("DarkLegacy.json")):
  print(os.popen("curl -O https://raw.githubusercontent.com/swccgpc/swccg-card-json/main/DarkLegacy.json").read())
if (not os.path.isfile("Light.json")):
  print(os.popen("curl -O https://raw.githubusercontent.com/swccgpc/swccg-card-json/main/Light.json").read())
if (not os.path.isfile("LightLegacy.json")):
  print(os.popen("curl -O https://raw.githubusercontent.com/swccgpc/swccg-card-json/main/LightLegacy.json").read())
if (not os.path.isfile("sets.json")):
  print(os.popen("curl -O https://raw.githubusercontent.com/swccgpc/swccg-card-json/main/sets.json").read())


##
## Load the set names in to memory
##
with open("sets.json") as json_data:
  sets_json = json.load(json_data)

release_sets = {}
for s in sets_json:
  release_sets[s["id"]] = {"name":s["name"], "abbr":s["abbr"]}


##
## Open tsv file for writing
##
fh = open("DiscordCardLinker/cards.tsv", "w")
fh.write("ID\tImageURL\tWikiURL\tCollInfo\tDisplayName\tTitle\tSubtitle\tTitleSuffix\tNicknames"+"\n")

##
## Parse json files and write to tsv
##
known_ids = {}
parse_json_file("Light.json")
parse_json_file("Dark.json")

#parse_json_file("LightLegacy.json")
#parse_json_file("DarkLegacy.json")
fh.close()






