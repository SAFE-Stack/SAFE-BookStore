FROM microsoft/dotnet:2.0.0-sdk
COPY /deploy /app
WORKDIR /app
EXPOSE 8080
EXPOSE 8085
ENTRYPOINT ["dotnet", "Server.dll"]
