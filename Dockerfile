FROM microsoft/dotnet:1.1.2-sdk
COPY /deploy /app
WORKDIR /app
EXPOSE 8080
EXPOSE 8085
ENTRYPOINT ["dotnet", "Server.dll"]
