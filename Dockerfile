FROM microsoft/dotnet:1.1.0-sdk-msbuild-rc4
COPY . .
ENTRYPOINT ["dotnet", "Server.dll"]