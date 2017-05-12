FROM microsoft/dotnet:1.1.1-sdk-1.0.1
COPY /deploy /app
WORKDIR /app
EXPOSE 8085
ENTRYPOINT ["dotnet", "Server.dll"]
