FROM mcr.microsoft.com/dotnet/core/aspnet:3.0 AS runtime
COPY /deploy .
WORKDIR .
EXPOSE 8085
ENTRYPOINT ["dotnet", "Server.dll"]
