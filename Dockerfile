FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS runtime
COPY /deploy .
WORKDIR .
EXPOSE 8085
ENTRYPOINT ["dotnet", "Server.dll"]
