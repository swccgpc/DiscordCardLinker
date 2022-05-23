#!/bin/bash

docker run -ti \
  -e TOKEN="${TOKEN}" \
  -e CLIENTID="${CLIENTID}" \
  -e PERMISSIONS="${PERMISSIONS}" \
  -e MAXIMAGESPERMESSAGE="${MAXIMAGESPERMESSAGE}" \
  discordcardlinker:latest
