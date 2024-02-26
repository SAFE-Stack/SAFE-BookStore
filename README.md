## Demo

You'll need to install the following pre-requisites in order to build SAFE applications

## Install pre-requisites

You'll need to install the following pre-requisites in order to build SAFE applications

* The [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [node.js](https://nodejs.org/) (v18.x or v20.x)
* [npm](https://www.npmjs.com/) (v9.x or v10.x)
* [docker](https://www.docker.com/products/docker-desktop/)

## Getting started

Before you run the project **for the first time only** you must install dotnet "local tools" with this command:

```bash
dotnet tool restore
```

To concurrently run the server and the client components in watch mode use the following command:

```bash
dotnet run
```

This will also spin up a local docker container with Azurite storage emulator which is used to save the wishlist data.

The build project in root directory contains a couple of different build targets. You can specify them after `--` (target name is case-insensitive).

Finally, there are `Bundle` and `Azure` targets that you can use to package your app and deploy to Azure, respectively:

```bash
dotnet run -- Bundle
dotnet run -- Azure
```

## SAFE Stack Documentation

If you want to know more about the full Azure Stack and all of it's components (including Azure) visit the official [SAFE documentation](https://safe-stack.github.io/docs/).

You will find more documentation about the used F# components at the following places:

* [Saturn](https://saturnframework.org/)
* [Fable](https://fable.io/docs/)
* [Elmish](https://elmish.github.io/elmish/)
