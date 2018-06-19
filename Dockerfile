FROM microsoft/dotnet:2.1.1-aspnetcore-runtime-alpine3.7
WORKDIR /bookstore
COPY /deploy .
EXPOSE 8085
ENTRYPOINT ["dotnet", "Server.dll"]
