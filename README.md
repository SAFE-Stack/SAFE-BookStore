# SAFE - A web stack designed for developer happiness

The following document describes the [SAFE-Stack](https://safe-stack.github.io/) sample project. 
SAFE is a technology stack that brings together several technologies into a single, coherent stack for typesafe, 
flexible end-to-end web-enabled applications that are written entirely in F#.

![SAFE-Stack](src/Client/images/safe_logo.png "SAFE-Stack")

You can see it running on Microsoft Azure at http://fable-suave.azurewebsites.net.

[![Build status](https://ci.appveyor.com/api/projects/status/ak9gjjjp32ens0e2?svg=true)](https://ci.appveyor.com/project/isaacabraham/safe-bookstore)
[![Build Status](https://travis-ci.org/SAFE-Stack/SAFE-BookStore.svg?branch=master)](https://travis-ci.org/SAFE-Stack/SAFE-BookStore)

## Requirements

- [Mono](http://www.mono-project.com/) on MacOS/Linux
- [.NET Framework 4.6.2](https://support.microsoft.com/en-us/help/3151800/the--net-framework-4-6-2-offline-installer-for-windows) on Windows
- [node.js](https://nodejs.org/) - JavaScript runtime
- [yarn](https://yarnpkg.com/) - Package manager for npm modules

> On OS X/macOS, make sure you have OpenSSL installed and symlinked correctly, as described here: [https://www.microsoft.com/net/core#macos](https://www.microsoft.com/net/core#macos).

[dotnet SDK 2.0.0](https://www.microsoft.com/net/core) is required but it will be downloaded automatically by the build script if not installed (see below). 
Other tools like [Paket](https://fsprojects.github.io/Paket/) or [FAKE](https://fake.build/) will also be installed by the build script.

For [deployment](#deployment) you need to have [docker](https://www.docker.com/) installed.

## Development mode

This development stack is designed to be used with minimal tooling. An instance of Visual Studio Code together with the excellent [Ionide](http://ionide.io/) plugin should be enough.

Start the development mode with:

    > build.cmd run // on windows
    $ ./build.sh run // on unix

This command will call in **build.fsx** the target "Run". It will start in parallel:
- **dotnet fable webpack-dev-server** in [src/Client](src/Client) (note: the Webpack development server will serve files on http://localhost:8080)
- **dotnet watch msbuild /t:TestAndRun** in [test/serverTests](src/ServerTests) to run unit tests and then server (note: Suave is launched on port **8085**)

Previously, the build script should download all dependencies like .NET Core SDK, Fable... If you have problems with the download of the .NET Core SDK via the `build.cmd` or `build.sh` script, please install the SDK manually from [here](https://github.com/dotnet/core/blob/master/release-notes/download-archives/1.0.4-download.md). Verify
that you have at least SDK version 2.0.0 installed (`dotnet --version`) and then rerun the build script.

You can now edit files in `src/Server` or `src/Client` and recompile + browser refresh will be triggered automatically.

![Development mode](https://cloud.githubusercontent.com/assets/57396/23174149/af93da32-f85b-11e6-8de2-01c274f54a27.gif)

Usually you can just keep this mode running and running. Just edit files, see the browser refreshing and commit + push with git.

## Getting started
### Create a new page
This topic will guide you through creating a new page. After every section you should check whether you can see the changes in your browser.
#### Minimal setup
Let's say we want to call our new page *Tomato*

1. Adjust the `Messages.fs` and register our Tomato page as a page and define the corresponding hash value.
    ```fsharp
    type Page = 
        | Home 
        | Login
        | WishList
        | Tomato // <- our page

    let toHash =
        function
        | Home -> "#home"
        | Login -> "#login"
        | WishList -> "#wishlist"
        | Tomato -> "#tomato" // <- our page
    ```
2. Adjust the following functions inside the `App.fs`:

    - pageParser function
        ```fsharp
        let pageParser : Parser<Page->_,_> =
            oneOf
                [ map Home (s "home")
                map Page.Login (s "login")                   
                map WishList (s "wishlist")
                map Page.Tomato (s "tomato") ]
        ```
    - urlUpdate function
        ```fsharp
        let urlUpdate (result:Page option) model =
            match result with
            // ...
            | Some (Tomato as page) ->
                { model with Page = page }, []
        ```
    - viewPage function
        ```fsharp
        let viewPage model dispatch =
            match model.Page with
            //...
            | Page.Tomato ->
                [ words 60 "Tomatoes taste good!"]
            // ...
        ```
3. Try it out by navigating to `http://localhost:8080/#tomato`
You should see `Tomatoes taste good!`
#### Adding the page to the menu
Inside `src/Client/pages/Menu.fs`
```fsharp
let view (model:Model) dispatch =
    div [ centerStyle "row" ] [ 
        // ...
        yield viewLink Page.Tomato "Tomato"
        // ..
```
#### Move code to separate Tomato.fs files
1. Add a new .fs file to the pages folder: `src/Client/pages/Tomato.fs`. 
Add the `Tomato.fs` to your .fsproj file and move it above Apps.fs.

        <Compile Include="pages/Tomato.fs" />
        <Compile Include="App.fs" />

2. Place following code in the `Tomato.fs`     
    ```fsharp
    module Client.Tomato
    open Style
    let view() = 
        [ words 60 "Tomatoes taste VERY good!"]
    ```

3. remove old 'view' code from the  viewPage function in `App.fs` and replace it 
    with:
    ```fsharp
    | Page.Tomato ->
        Tomato.view()
    ```

#### Define a model for the page that holds the state
1. Replace the code in `Tomato.fs` with
    ```fsharp
    module Client.Tomato
    open Style

    type Model = {
        Color:string
    }
    let init() = 
        { Color = "red" }
    let view model = 
        [ 
            words 60 "Tomatoes taste VERY good!"
            words 20 (sprintf "The color of a tomato is %s"  model.Color)
        ]
    ```

2. Adjust the SubModel DU
    ```fsharp
    type SubModel =
        | NoSubModel
        | LoginModel of Login.Model
        | WishListModel of WishList.Model
        | TomatoModel of Tomato.Model
    ```

3. updateUrl should now call the init function and place the Tomato.Model as SubModel
    ```fsharp
    | Some (Tomato as page) ->
        let m = Tomato.init()
        { model with Page = page; SubModel = TomatoModel m }, []
    ```


4. viewPage function should call the view function of the the Tomato module and pass in the submodel if it is a TomatoModel
    ```fsharp
    | Page.Tomato ->
        match model.SubModel with
        | TomatoModel m -> Tomato.view m
        | _ -> [ ]
    ```
#### Make it interactive (update the state)
1. add new message DU in `Messages.fs`
    ```fsharp
    type TomatoMsg =
        | ChangeColor of string
    ```

2. add message to AppMsg DU in `Messages.fs`
    ```fsharp
    type AppMsg = 
    // ..
        | TomatoMsg of TomatoMsg
    // ..
    ```

3. adjust the match pattern in the update function of `App.fs`
    ```fsharp
    | AppMsg.TomatoMsg msg, TomatoModel tm ->
        let color = match msg with ChangeColor c -> c
        let tm = { tm with Color = color }
        { model with SubModel = TomatoModel tm }, []

    | AppMsg.TomatoMsg msg, _ -> model, [] // just to make the compiler happy
    ```

4. Change the `Tomato.view` function to:
    ```fsharp
    let view model dispatch = 
        [ 
            words 60 "Tomatoes taste VERY good!"
            words 20 (sprintf "The color of a tomato is %s" model.Color)
            br []
            button [
                ClassName ("btn btn-primary") 
                OnClick (fun _ -> dispatch (TomatoMsg (TomatoMsg.ChangeColor "green")))] 
                [ str "No, my tomatoes are green!" ]
        ]
    ```

## Debugging

The server side of the application can be debugged using Ionide.

1. Open repo in VSCode
2. Open debug panel, choose `Debug` from combobox, and press green arrow (or `F5`). This will build server and start it with debugger attached. It will also start Fable watch mode for the Client side and open the browser.

Client side debugging is supported by any modern browser with any developer tools. Fable even provides source maps which will let you put breakpoints in F# source code (in browser dev tools). Also, we additionally suggest installing React-devtools (for better UI debugging) and Redux-devtools (time travel debuger).

## Technology stack

### Suave on .NET Core

The webserver backend is running as a [Suave.io](https://suave.io/) service on .NET Core.

In development mode the server is automatically restarted whenever a file in `src/Server` is saved.

### React/Elmish client

The client is [React](https://facebook.github.io/react/) single page application that uses [fable-elmish](https://github.com/fable-compiler/fable-elmish).

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
```fsharp
testCase "login with test user" <| fun () ->
    url serverUrl
    waitForElement ".elmish-app"

    click "Login"

    "#username" << "test"
    "#password" << "test"

    click "Log In"

    waitForElement "Isaac Abraham"
```
![Canopy in action](https://cloud.githubusercontent.com/assets/57396/23131425/38d06e8c-f78a-11e6-9ebc-8442b0abf752.gif)

## Additional tools

### FAKE

[FAKE](http://fsharp.github.io/FAKE/) is a build automation system with capabilities which are similar to make and rake. It's used to automate the build, test and deployment process. Look into `build.fsx` for details.

### Paket

[Paket](https://fsprojects.github.io/Paket/) is a dependency manager and allows easier management of the NuGet packages.

## Deployment

The deployment for this repo works via [docker](https://www.docker.com/) and therefore you need docker installed on your machine.

### Microsoft Azure

The following part shows how to set up automatic deployment to [Microsoft Azure](https://azure.microsoft.com).

![Auto Deployment to Azure](https://user-images.githubusercontent.com/57396/30860733-740e509c-a2c8-11e7-88c6-0341c4beab38.gif)

#### Docker Hub

Create a new [Docker Hub](https://hub.docker.com) account and a new public repository on Docker Hub.

#### Release script

Create a file called `release.cmd` with the following content and configure your DockerHub credentials:

    @echo off
    cls

    .paket\paket.exe restore
    if errorlevel 1 (
      exit /b %errorlevel%
    )

    packages\build\FAKE\tools\FAKE.exe build.fsx Deploy "DockerLoginServer=docker.io" "DockerImageName=****" "DockerUser=****" "DockerPassword=***" %*

Don't worry the file is already in `.gitignore` so your password will not be commited.

#### Initial docker push

In order to release a container you need to create a new entry in [RELEASE_NOTES.md] and run `release.cmd`. 
This will build the server and client, run all test, put the app into a docker container and push it to your docker hub repro.

#### Azure Portal

Go to the [Azure Portal](https://portal.azure.com) and create a new "Web App for Containers". 
Configure the Web App to point to the docker repo and select `latest` channel of the container.
Also look for the "WebHook Url" on the portal, copy that url and set it as new trigger in your Docker Hub repo.

Now you should be able to reach the website ony your `.azurewebsites.net` url.

#### Further releases

Now everything is set up. By creating new entries in [RELEASE_NOTES.md] and a new run of `release.cmd` the website should update automatically.

### Google Cloud AppEngine

The repository comes with a sample `app.yaml` file which is used to deploy to Google Cloud AppEngine using the custom flex environment. At the moment it seems like the application must run on port `8080` and that is set as a environment variable in the `app.yaml` file. When you run the deploy command it will first look for the `app.yaml` file and then look for the `Dockerfile` for what should deploy. The container that is deploy is exactly that same as the one that should have been deployed to Azure, but it is only set up to deploy from local to Google Cloud at the moment, and not from CI server to Google Cloud.

Before you can execute the deploy command you also need to build the solution with the `Publish` target, that is so the container image will get the compiled binaries when the container build is executed via the deploy command.

To execute the deploy you need a Google Cloud account and project configured as well as the tooling installed, https://cloud.google.com/sdk/downloads. The command you need to run is:

    gcloud app deploy -q <--version=main> <--verbosity=debug>

The `version` and `verbosity` flag isn't need, but it is recommended to use the `version` flag so you don't end up with too many versions of your application without removing previous ones. Use `verbosity=debug` if you are having some problems.

Deploy to the flex environment with a custom runtime like this is might take some time, but the instructions here should work.

## Known Issues

### Getting rid of errors in chrome

- Either comment out the lines in `App.fs`:

```fsharp
#if DEBUG
|> Program.withDebugger
#endif
```

- Or install the [Redux DevTools](http://extension.remotedev.io/) as a Chrome Extensions (recommended)
Only one error remains, when visiting the WebApp the first time.

### TypeProviders in the backend

Sadly, .NET Core 2.0 currently does not support Type Providers, so you can not just add the NuGet packages like FSharp.Data to your backend project.

### Additional Notes

- You can not call the functions of Fable inside the F# interactive.

## Maintainer(s)

- [@forki](https://github.com/forki)
- [@alfonsogarciacaro](https://github.com/alfonsogarciacaro)
