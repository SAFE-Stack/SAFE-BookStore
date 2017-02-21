# Attention

This is a work in progress so it may not work yet as is on your machine.

# A web stack designed for developer happiness

The following document describes the Fable + Suave sample project. You can see it running on azure at http://fable-suave.azurewebsites.net.

## Development mode

This development stack is designed to be used with minimal tooling. A instance of Visual Studio Code together with the excellent [Ionide](http://ionide.io/) plugin should be enough.

Start the development mode with:

    > build.cmd run // on windows
    $ ./build.sh run // on unix

This should download all dependencies like .NET Core SDK, node, ... and start the development server. It will also open a browser with the website on http://localhost:8080.

You can now edit files in `src/Server` or `src/Client` and recompile + browser refresh will be triggered automatically. 
Usually you can just keep this mode running and running. Just edit files, see the browser refreshing and commit + push with git.

## Requirements

* .NET or Mono
* npm

## Technology stack

### Suave on .NET Core 

The webserver backend is running as a [Suave.io](https://suave.io/) service on .NET Core.

In development mode the server is automatically restarted whenever a file in `src/Server` is saved.

### React/Elmish client 

The client is [React](https://facebook.github.io/react/) single page application that uses [fable-elmish](https://github.com/fable-compiler/fable-elmish) to provide a scaffold for the code.

The communication to the server is done via HTTPS calls to `/api/*`. If a user is logged in then a [JSON Web Token](https://jwt.io/) is sent to the server with every request.

### Fable

The [Fable](http://fable.io/) compiler is used to compile the F# client code to JavaScript so that it can run in the browser.

## Testing

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

## Maintainer(s)

- [@forki](https://github.com/forki)
- [@alfonsogarciacaro](https://github.com/alfonsogarciacaro)

