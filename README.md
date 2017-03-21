### Attention

This is a work in progress so it may not work yet as is on your machine.

# A web stack designed for developer happiness

The following document describes the Fable + Suave sample project. You can see it running on azure at http://fable-suave.azurewebsites.net.

## Development mode

This development stack is designed to be used with minimal tooling. A instance of Visual Studio Code together with the excellent [Ionide](http://ionide.io/) plugin should be enough.

Start the development mode with:

    > build.cmd run // on windows
    $ ./build.sh run // on unix

This should download all dependencies like .NET Core SDK, node, ... and start the development server. It will also open a browser with the website on http://localhost:8080.  

If you have problems with the download of the .NET Core SDK via the `build.cmd` or `build.sh` script, please install the SDK manually from [here](https://github.com/dotnet/core/blob/master/release-notes/download-archives/1.0.4-download.md). Verify
that you have at least SDK version 1.0.1 installed (`dotnet --version`) and then rerun the build script.

You can now edit files in `src/Server` or `src/Client` and recompile + browser refresh will be triggered automatically.

![Development mode](https://cloud.githubusercontent.com/assets/57396/23174149/af93da32-f85b-11e6-8de2-01c274f54a27.gif)

Usually you can just keep this mode running and running. Just edit files, see the browser refreshing and commit + push with git.

## Requirements

* .NET or Mono
* npm
* On OS X/macOS, make sure you have OpenSSL installed and symlinked correctly, as described here: [https://www.microsoft.com/net/core#macos](https://www.microsoft.com/net/core#macos).

## Technology stack

### Suave on .NET Core

The webserver backend is running as a [Suave.io](https://suave.io/) service on .NET Core.

In development mode the server is automatically restarted whenever a file in `src/Server` is saved.

### React/Elmish client

The client is [React](https://facebook.github.io/react/) single page application that uses [fable-elmish](https://github.com/fable-compiler/fable-elmish) to provide a scaffold for the code.

The communication to the server is done via HTTPS calls to `/api/*`. If a user is logged in then a [JSON Web Token](https://jwt.io/) is sent to the server with every request.

### Fable

The [Fable](http://fable.io/) compiler is used to compile the F# client code to JavaScript so that it can run in the browser.

### Shared code between server and client

"Isomorphic F#" started a bit as a joke about [Isomorphic JavaScript](http://isomorphic.net/). The naming is really bad, but the idea to have the same code running on client and server is really interesting.
If you look at `src/Server/Shared/Domain.fs` then you will see code that is shared between client and server. On the server it is compiled to .NET core and for the client the Fable compiler is translating it into JavaScript.
This is a really convenient technique for a shared domain model.

## Testing

Start the full build (incl. UI tests) with:

    > build.cmd // on windows
    $ ./build.sh // on unix

### Expecto

[Expecto](https://github.com/haf/expecto) is a test framework like NUnit or xUnit, but much more developer friendly. With Expecto you write tests as values in normal code.
Tests can be composed, reduced, filtered, repeated and passed as values, because they are values. This gives the programmer a lot of leverage when writing tests.

If you are in [development mode](#development-mode) then you can use Expecto's focused test feature to run a selected test against the running server.

### Canopy

[canopy](https://github.com/lefthandedgoat/canopy) is a F# web automation and testing library, built on top of Selenium. In our expecto suite it looks like the following:

        testCase "login with test user" <| fun () ->
            url serverUrl
            waitForElement ".elmish-app"

            click "Login"

            "#username" << "test"
            "#password" << "test"

            click "Log In"

            waitForElement "Isaac Abraham"

![Canopy in action](https://cloud.githubusercontent.com/assets/57396/23131425/38d06e8c-f78a-11e6-9ebc-8442b0abf752.gif)

## Additional tools

### FAKE

[FAKE](http://fsharp.github.io/FAKE/) is a build automation system with capabilities which are similar to make and rake. It's used to automate the build, test and deployment process. Look into `build.fsx` for details.

### Paket

[Paket](https://fsprojects.github.io/Paket/) is a dependency manager and allows easier management of the NuGet packages.

## Build path

Mixing Fsharp with Javascript and Fable, and throwing a bit of webpack in the middle doesn't provide the most straightforward build process.
Here is how it works for [Development mode](README.md#development-mode)

    > build.cmd run // on windows
    $ ./build.sh run // on unix

This command will call in **build.fsx** the target "Run".
It will start in parallel :
* a **dotnet watch** build on the [Server.fsproj](src/Server/Server.fsproj) (note: Suave is launched on port **8085**)
* a **npm run watch** command in [src/Client](src/Client) folder
* a browser on port **8080**

On the Javascript side :
1. [Build.fsx](build.fsx) starts npm with  **run** argument telling to execute the **watch** target.
2. All npm scripts can be found in [src/Client/package.json](src/Client/package.json). **watch** will start fable compilation with **-d** for target debug and **-w** (watch) to recompile project much faster on file modification. 
3. All Fable scripts can be found in [src/Client/fableconfig.json](src/Client/fableconfig.json). Debug target (**-d**) script will start **webpack-dev-server** in watch mode and with hot reloading.
4. Config for **webpack-dev-server is found** in [src/Client/webpack.config.js](src/Client/webpack.config.js). In this config, webpack-dev-server is running on port 8080. Any call to /api/ is routed to port 8085 (our original Suave webserver)

## Maintainer(s)

- [@forki](https://github.com/forki)
- [@alfonsogarciacaro](https://github.com/alfonsogarciacaro)
