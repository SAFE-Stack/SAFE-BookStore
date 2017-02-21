FROM microsoft/dotnet:core
COPY . .
EXPOSE 8085
ENTRYPOINT ["dotnet", "Server.dll"]