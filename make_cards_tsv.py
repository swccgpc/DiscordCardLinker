#!/usr/bin/env python3

import json
import os
import urllib.parse

def parse_json_file(json_filename):

  with open(json_filename) as json_data:
    side_json = json.load(json_data)

  for card in side_json["cards"]:
    nicknames  = []
    card_id    = str(card["id"])
    img_url    = card["front"]["imageUrl"]
    gempid     = card["gempId"].split("_")
    title      = card["front"]["title"]
    side       = card["side"]
    ##
    ## http_url is the Wiki URL.
    ## LOTR uses a wiki, so this is the URL that would display information about that page.
    ## The string can be sent to scomp using s?=
    ##
    http_url   = "https://scomp.starwarsccg.org/?s="+urllib.parse.quote_plus(title)

    ## Generate a collecting code like they have with MTG.
    ## The collecting code is basically: ReleaseAbbr+Rarity+CardID
    ## The interesting part is that we have 3 different levels of "Unlimited" cards: U, U1, and U2.
    ## If you take the Card ID 14 and the rarity code of U, you get: U14
    ## If you take the Card ID 4 and the rarity code U1, you get: U14
    ##   •Lt. Poldin Lehuse            V11U14  V11 U1  4
    ##   •Trade Federation Tactics (V) V11U14  V11 U  14
    ## Ensure that the card ID is always 3 numbers:
    ##   •Lt. Poldin Lehuse            V11U1004  V11 U1  4
    ##   •Trade Federation Tactics (V) V11U014   V11 U  14
    collecting = release_sets[card["set"]]["abbr"]+card["rarity"]+("000"+gempid[1])[-3:]
    if "(AI)" in title:
      collecting = collecting + "AI"
    if "(OAI)" in title:
      collecting = collecting + "OAI"


    if card["set"] == "200d":
      set_id = 200
    else:
      set_id = int(card["set"])

    ##
    ## Process the card titles.
    ## This method of processing accomodates cards with multiple titles like:
    ##   "•The Mythrol/•The Mythrol"
    ##   "•The Falcon, Junkyard Garbage/•The Falcon, Junkyard Garbage"
    ##   "•There Is No Try & •Oppressive Enforcement"
    ##
    titles = [title]
    if "/" in title:
      titles = title.split("/")

    for title in titles:
      #print("  * Processing Title: ["+title+"]")
      ## if it is a non-legacy virtual set, and (V) not in the title, add it.
      if set_id >199 and set_id < 300:
        if "(V)" not in title:
          title = title+" (V)"

      ##
      side_title = side+" "+title
      short_title = side+""+title
      abbr_side_title = side.replace("Dark", "DS").replace("Light", "LS")+" "+title
      short_abbr_side_title = side.replace("Dark", "DS").replace("Light", "LS")+""+title

      ## full name, with <>, •, and the "(V)" at the end
      nicknames.append(title)
      nicknames.append(side_title)
      nicknames.append(short_title)
      nicknames.append(abbr_side_title)
      nicknames.append(short_abbr_side_title)

      ## preserve the "(V)" at the end
      if "•" in title:
        nicknames.append(title.replace("•", ""))
        nicknames.append(side_title.replace("•", ""))
        nicknames.append(short_title.replace("•", ""))
        nicknames.append(abbr_side_title.replace("•", ""))
        nicknames.append(short_abbr_side_title.replace("•", ""))

      ## preserve the "(V)" at the end
      if "<>" in title:
        nicknames.append(title.replace("<>", ""))
        nicknames.append(side_title.replace("<>", ""))
        nicknames.append(short_title.replace("<>", ""))
        nicknames.append(abbr_side_title.replace("<>", ""))
        nicknames.append(short_abbr_side_title.replace("<>", ""))

      ## Apple machines replace regular quotes with Smartquotes
      if "'" in title:
        nicknames.append(title.replace("'", "’"))
        nicknames.append(title.replace("'", "‘"))

        nicknames.append(side_title.replace("'", "’"))
        nicknames.append(side_title.replace("'", "‘"))

        nicknames.append(short_title.replace("'", "’"))
        nicknames.append(short_title.replace("'", "‘"))

        nicknames.append(abbr_side_title.replace("'", "’"))
        nicknames.append(abbr_side_title.replace("'", "‘"))

        nicknames.append(short_abbr_side_title.replace("'", "’"))
        nicknames.append(short_abbr_side_title.replace("'", "‘"))

      if '"' in title:
        nicknames.append(title.replace('"', '“'))
        nicknames.append(title.replace('"', '”'))

        nicknames.append(side_title.replace('"', '“'))
        nicknames.append(side_title.replace('"', '”'))

        nicknames.append(short_title.replace('"', '“'))
        nicknames.append(short_title.replace('"', '”'))

        nicknames.append(abbr_side_title.replace('"', '“'))
        nicknames.append(abbr_side_title.replace('"', '”'))

        nicknames.append(short_abbr_side_title.replace('"', '“'))
        nicknames.append(short_abbr_side_title.replace('"', '”'))


      if ("'" in title) and ('"' in title):
        nicknames.append(title.replace("'", "’").replace('"', '“').replace('"', '”'))
        nicknames.append(title.replace("'", "‘").replace('"', '“').replace('"', '”'))

        nicknames.append(side_title.replace("'", "’").replace('"', '“').replace('"', '”'))
        nicknames.append(side_title.replace("'", "‘").replace('"', '“').replace('"', '”'))

        nicknames.append(short_title.replace("'", "’").replace('"', '“').replace('"', '”'))
        nicknames.append(short_title.replace("'", "‘").replace('"', '“').replace('"', '”'))

        nicknames.append(abbr_side_title.replace("'", "’").replace('"', '“').replace('"', '”'))
        nicknames.append(abbr_side_title.replace("'", "‘").replace('"', '“').replace('"', '”'))

        nicknames.append(short_abbr_side_title.replace("'", "’").replace('"', '“').replace('"', '”'))
        nicknames.append(short_abbr_side_title.replace("'", "‘").replace('"', '“').replace('"', '”'))





      ## Callable by the collecting ID
      nicknames.append(title+" ("+collecting+")")
      nicknames.append(collecting)
      nicknames.append("("+collecting+")")

      ## A clean name without all the extras
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

    if "Maul" in title:
      nicknames.append("Kenobi!")
      nicknames.append("Kenobii")
      nicknames.append("Kenobiii")
      nicknames.append("Kenobiiii")
      nicknames.append("Kenobiiiii")
      nicknames.append("Kenobiiiiii")
      nicknames.append("Kenobiiiiiii")
      nicknames.append("Kenobiiiiiiii")
      nicknames.append("Kenobiiiiiiiii")
      nicknames.append("Kenobiiiiiiiiii")
      nicknames.append("Kenobii!")
      nicknames.append("Kenobiii!")
      nicknames.append("Kenobiiii!")
      nicknames.append("Kenobiiiii!")
      nicknames.append("Kenobiiiiii!")
      nicknames.append("Kenobiiiiiii!")
      nicknames.append("Kenobiiiiiiii!")
      nicknames.append("Kenobiiiiiiiii!")
      nicknames.append("Kenobiiiiiiiiii!")
      nicknames.append("Kenobi!")
      nicknames.append("Kenobi!!")
      nicknames.append("Kenobi!!!")
      nicknames.append("Kenobi!!!!")
      nicknames.append("Kenobi!!!!!")
      nicknames.append("Kenobi!!!!!!")
      nicknames.append("Kenobi!!!!!!!")
      nicknames.append("Kenobi!!!!!!!!")
      nicknames.append("Kenobi!!!!!!!!!")


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






