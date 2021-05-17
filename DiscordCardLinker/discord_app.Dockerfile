FROM mcr.microsoft.com/dotnet/core/runtime:3.1

#COPY publish dockerbot/

EXPOSE 80

WORKDIR /dockerbot

ENTRYPOINT ["dotnet", "DiscordCardLinker.dll"]