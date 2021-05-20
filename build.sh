#!/bin/bash


docker ps -a | awk '{print "docker rm "$1}' | sh
docker images | grep "none" | awk '{print "docker rmi "$3;}' | sh
docker rmi discordcardlinker


cd DiscordCardLinker
docker build -t discordcardlinker -f Dockerfile .


