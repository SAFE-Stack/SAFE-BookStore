FROM microsoft/dotnet:2.2.0-runtime
COPY /deploy .
WORKDIR .
EXPOSE 8085
ENTRYPOINT ["dotnet", "Server.dll"]
