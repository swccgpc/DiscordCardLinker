
##
## docker build -t DiscordCardLinker -f Dockerfile .
##
## The dotnetcore images are based on Debian GNU/Linux 10 (buster)
##
## docker run -ti discordcardlinker:latest /bin/sh
##

##
## Build
##
FROM mcr.microsoft.com/dotnet/core/sdk AS build-env
WORKDIR /app

## Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

## Copy everything else and build
COPY ./* ./
RUN dotnet publish -c Release -o out


##
## Build runtime image
##
FROM mcr.microsoft.com/dotnet/core/runtime

ENV TOKEN               ""
ENV CLIENTID            ""
ENV PERMISSIONS         ""
ENV MAXIMAGESPERMESSAGE "5"


WORKDIR /app

COPY --from=build-env /app/out .

CMD ["dotnet", "DiscordCardLinker.dll"]



