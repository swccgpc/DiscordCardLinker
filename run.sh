#!/bin/bash

docker run -ti \
  -e TOKEN="${TOKEN}" \
  -e CLIENTID="${CLIENTID}" \
  -e PERMISSIONS="${PERMISSIONS}" \
  -e BASEIMAGEURL="${BASEIMAGEURL}" \
  -e BASEWIKIURL="${BASEWIKIURL}" \
  -e MAXIMAGESPERMESSAGE="${MAXIMAGESPERMESSAGE}" \
  discordcardlinker:latest
