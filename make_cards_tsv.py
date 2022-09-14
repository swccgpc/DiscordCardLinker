#!/usr/bin/env python3

import json
import os
import codecs
import copy
import re
import urllib.parse

class CardRow:
  def __init__(self):
    
    self.card_id       = ""
    self.img_url       = ""
    self.http_url      = ""
    self.gempid        = ""
    self.collecting    = ""
    self.displayName   = ""
    self.title         = ""
    self.subtitle      = ""
    self.title_suffix  = ""
    self.nicknames     = []
    
    self.set_id        = ""
    self.side          = ""
    self.side_abbr     = ""
    self.unique_id     = ""
    
    

def parse_json_file(json_filename):

  with codecs.open(json_filename, "r", "utf-8") as json_data:
    side_json = json.load(json_data)

  for card in side_json["cards"]:
    row = CardRow()
    row2 = None
    double_sided = False
    same_name = False
    
    row.card_id       = str(card["id"])
    row.img_url       = card["front"]["imageUrl"]
    row.gempid        = card["gempId"].split("_")
    row.displayName   = card["front"]["title"]
    row.side          = card["side"]
    row.side_abbr     = row.side.replace("Dark", "[DS]").replace("Light", "[LS]")
    row.unique_id     = row.card_id + "\t" + row.collecting + "\t" + row.displayName

    if(row.side is None or row.side == ""):
      print(row.card_id)
    ##
    ## http_url is the Wiki URL.
    ## LOTR uses a wiki, so this is the URL that would display information about that page.
    ## The string can be sent to scomp using s?=
    ##
    row.http_url   = "https://scomp.starwarsccg.org/?s="+urllib.parse.quote_plus(row.displayName)

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
    row.collecting = release_sets[card["set"]]["abbr"]+card["rarity"]+("000"+row.gempid[1])[-3:]
    if "(AI)" in row.displayName:
      row.collecting = row.collecting + "AI"
    if "(OAI)" in row.displayName:
      row.collecting = row.collecting + "OAI"

    if card["set"] == "200d":
      row.set_id = 200
    else:
      row.set_id = int(card["set"])
      
    ## if it is a non-legacy virtual set, and (V) not in the title, add it.
    if row.set_id >199 and row.set_id < 300:
      if "(V)" not in row.displayName:
        row.displayName = row.displayName + " (V)"

    ## A clean name without all the extras
    row.title = row.displayName.replace("•", "").replace(" (V)", "").replace("<>", "").replace(" (AI)", "").strip()
  

    ## for dual-title names like: •There Is No Try & •Oppressive Enforcement
    ## use the second title as the subtitle, unless they are the same
    ## like "•The Falcon, Junkyard Garbage/•The Falcon, Junkyard Garbage"
    ##
    ## for names like: Obi-Wan Kenobi, Jedi Knight
    ## use the data after the comma to be the subtitle
    ##
    ## however we don't want to split up "Anger, Fear, Aggression"
    ## and similar cards
    ##
    ## for names like: Malachor: Sith Temple Upper Chamber
    ## use the data after the colon to be the subtitle
    ##
    ## for names like: R2-D2 (Artoo-Detoo)
    ## use the parenthetical as the subtitle
    ## (this unfortunately also captures "Crossfire (Endor)" and the like,
    ##  but it shouldn't hurt the actual search results)
    
    if "/" in row.title:
      names            = row.title.split("/", 1)
      double_sided     = True
      if names[0].strip() == names[1].strip():
        row.title      = names[0].strip()
        same_name      = True
      else:
        row.title      = names[0].strip()
        row.subtitle   = names[1].strip()
    elif "&" in row.title:
      names            = row.title.split("&", 1)
      if names[0] == names[1]:
        row.title      = names[0].strip()
      else:
        row.title      = names[0].strip()
        row.subtitle   = names[1].strip()
    
    if row.subtitle == "":
      if (row.title.count(",") == 1):
        names          = row.title.split(",", 1)
        row.title      = names[0].strip()
        row.subtitle   = names[1].strip()
      elif (":" in row.title):
        names          = row.title.split(":", 1)
        row.title      = names[0].strip()
        row.subtitle   = names[1].strip()
      elif ("(" in row.title):
        names          = row.title.split("(", 1)
        row.title      = names[0].strip()
        row.subtitle   = names[1].replace(")", "").strip()
        
    
    if ("(AI)" in row.displayName):
      row.title_suffix = "(AI)"
    elif ("(V)" in row.displayName):
      row.title_suffix = "(V)"
      
    row.displayName = row.displayName.replace("<>", "♢");
    
    # nicknames already embedded in the data files
    if "abbr" in card:
      nicks = card["abbr"]
      for nick in nicks:
        # We don't care about actual abbreviations, as the main app
        # handles that.  We are however interested in aliases, which
        # aren't in all caps.  Also the abbreviations tend to add
        # random assortments of V for virtual, which again the app handles.
        nick = re.sub(r'[vV]$', '', nick)
        nick = re.sub(r' ?(V) ?', '', nick)
        if nick.isupper(): 
          continue
        
        row.nicknames.append(nick)

    # easter eggs
    if (("Obi-Wan" in row.title) or ("Kenobi" in row.title)):
      row.nicknames.append("General Kenobi")
      row.nicknames.append("Hello There")
    #
    if "Maul" in row.title:
      row.nicknames.append("Kenoobiiiiiiiiiiiiii")
      
      
    if double_sided:
      #Make a new row to handle dual-sided cards
      row2 = copy.deepcopy(row)
      
      row2.img_url = card["back"]["imageUrl"]

      #For a dual-sided card we want to swap the title/subtitle
      # in the copy.  This allows searches where one side is known to 
      # find and display the flip side.
      if same_name == False:
        row2.title = row.subtitle
        row2.subtitle = row.title
        match = re.search(r'(.*?) ?/ ?(.*?)(\(.*\)$)?$', row.displayName, re.MULTILINE)
        row2.displayName = match.group(2) + " / " + match.group(1) 
        if match.group(3) is not None:
          row2.displayName += match.group(3)
        
      #For a dual-sided card with the same title on either side, we want to
      # differentiate the front and back rather than repeat the name
      else:
        print(row2.title)
        match = re.search(r'(.*?) ?/ ?(.*?)(\(.*\)$)?$', row.displayName, re.MULTILINE)
        row.displayName = match.group(1) 
        row2.displayName = match.group(2)
        if match.group(3) is not None:
          row.displayName += match.group(3)
          row2.displayName += match.group(3)
        row.displayName  += " (Front)"
        row2.displayName += " (Back)"

      #some ids demand uniqueness, so we'll give it to them
      row.card_id += "F"
      row.collecting += "F"
      row2.card_id += "B"
      row2.collecting += "B"
      row2.unique_id += "B"

    if (row.unique_id in known_ids):
      print("\n\n!!!! " + row.unique_id)
      print("!!!! " + known_ids[row.unique_id] + "\n\n")
    else:
      known_ids[row.unique_id] = row
      # We save all cards to do a LS/DS check later
      code = row.title + row.subtitle
      if code not in collisions:
        collisions[code] = {}
        collisions[code]["Dark"] = []
        collisions[code]["Light"] = []
      
      collisions[code][row.side].append(row)
      if(row2 is not None):
        known_ids[row2.unique_id] = row2
        collisions[code][row.side].append(row2)
  
  
def output_rows():
  for code in collisions:
    if len(collisions[code]["Dark"]) > 0 and len(collisions[code]["Light"]) > 0:
      #print(code + " collision")
      for row in collisions[code]["Dark"]:
        row.displayName = row.side_abbr + " " + row.displayName
      for row in collisions[code]["Light"]:
        row.displayName = row.side_abbr + " " + row.displayName

  for uid in known_ids:   
    row = known_ids[uid]
    out = row.card_id + "\t" + row.img_url + "\t" + row.http_url + "\t" + row.collecting + "\t" + row.displayName + "\t" + row.title + "\t" + row.subtitle + "\t" + row.title_suffix + "\t" + ",".join(row.nicknames)
    #print(uid)
    fh.write(out+"\n")

if __name__ == "__main__":
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
  fh = codecs.open("DiscordCardLinker/cards.tsv", "w", "utf-8")
  fh.write("ID\tImageURL\tWikiURL\tCollInfo\tDisplayName\tTitle\tSubtitle\tTitleSuffix\tNicknames"+"\n")

  ##
  ## Parse json files and write to tsv
  ##
  known_ids = {}
  collisions = {}
  parse_json_file("Light.json")
  parse_json_file("Dark.json")
  output_rows()

  #parse_json_file("LightLegacy.json")
  #parse_json_file("DarkLegacy.json")
  fh.close()

