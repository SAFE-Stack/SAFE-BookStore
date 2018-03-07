FROM microsoft/aspnetcore
COPY /deploy .
WORKDIR .
EXPOSE 8085
ENTRYPOINT ["dotnet", "Server.dll"]
