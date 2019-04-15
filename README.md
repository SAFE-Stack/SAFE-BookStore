# SAFE - A web stack designed for developer happiness

The following document describes the [SAFE-Stack](https://safe-stack.github.io/) sample project.
SAFE is a technology stack that brings together several technologies into a single, coherent stack for typesafe,
flexible end-to-end web-enabled applications that are written entirely in F#.

![SAFE-Stack](src/Client/Images/safe_logo.png "SAFE-Stack")

You can see it running on Microsoft Azure at https://safebookstore.azurewebsites.net/.

[![Build status](https://ci.appveyor.com/api/projects/status/ak9gjjjp32ens0e2?svg=true)](https://ci.appveyor.com/project/isaacabraham/safe-bookstore)
[![Build Status](https://travis-ci.org/SAFE-Stack/SAFE-BookStore.svg?branch=master)](https://travis-ci.org/SAFE-Stack/SAFE-BookStore)

## Note!

If you are looking to create your *own* SAFE application, we recommend that you use the official [SAFE template](https://safe-stack.github.io/docs/template-overview/) which provides a clean, flexible and regularly updated template dotnet designed for starting brand-new applications quickly and easily.

## Requirements

- [Mono](http://www.mono-project.com/) on MacOS/Linux
- [.NET Framework 4.6.2](https://support.microsoft.com/en-us/help/3151800/the--net-framework-4-6-2-offline-installer-for-windows) on Windows
- [node.js](https://nodejs.org/) - JavaScript runtime
- [yarn](https://yarnpkg.com/) - Package manager for npm modules
- [dotnet SDK 2.1.500](https://github.com/dotnet/cli/releases/tag/v2.1.500)
- Other tools like [Paket](https://fsprojects.github.io/Paket/) or [FAKE](https://fake.build/) will also be installed by the build script.
- For [deployment](#deployment) you need to have [docker](https://www.docker.com/) installed.

## Development mode

This development stack is designed to be used with minimal tooling. An instance of Visual Studio Code together with the excellent [Ionide](http://ionide.io/) plugin should be enough.

Start the development mode with:

    > ./build.cmd run // on windows
    $ ./build.sh run // on unix

This command will call the target "Run" in **build.fsx**. This will start in parallel:
- **dotnet fable webpack-dev-server** in [src/Client](src/Client) (note: the Webpack development server will serve files on http://localhost:8080)
- **dotnet watch msbuild /t:TestAndRun** in [test/serverTests](src/ServerTests) to run unit tests and then server (note: Giraffe is launched on port **8085**)

You can now edit files in `src/Server` or `src/Client` and recompile + browser refresh will be triggered automatically.
For the case of the client ["Hot Module Replacement"](https://webpack.js.org/concepts/hot-module-replacement/) is supported, which means your app state is kept over recompile.

![Development mode](https://user-images.githubusercontent.com/57396/31067904-bec572e6-a755-11e7-967d-c169724006f2.gif)

Usually you can just keep this mode running and running. Just edit files, see the browser refreshing and commit + push with git.

## Getting started

### Create a new page
This topic will guide you through creating a new page. After every section you should check whether you can see the changes in your browser.

#### Minimal setup
Let's say we want to call our new page *Tomato*

1. Adjust the `src/Client/Pages.fs` and register our Tomato page as a page and define the corresponding path.

    ```fsharp
    type Page =
        | Home
        | Login
        | WishList
        | Tomato // <- our page

    let toPath =
        function
        | Page.Home -> "/"
        | Page.Login -> "/login"
        | Page.WishList -> "/wishlist"
        | Page.Tomato -> "/tomato" // <- our page

    let pageParser : Parser<Page->_,_> =
        oneOf
            [ map Page.Home (s "home")
              map Page.Login (s "login")
              map Page.WishList (s "wishlist")
              map Page.Tomato (s "tomato") ] // <- our page
    ```

2. Adjust the model, update and view:

    - `PageModel` type inside `src/Client/Shared.fs`
        ```fsharp
        type PageModel =
            //...
            | TomatoModel
        ```
    - `view` function inside `src/Client/Shared.fs`
        ```fsharp
        let view model dispatch =
            div [ Key "Application" ] [
                //...
                div [ centerStyle "column" ] [
                    match model.PageModel with
                    //...
                    | TomatoModel ->
                        yield div [] [ words 60 "Tomatos taste good"]
        ```

3. Adjust the `urlUpdate` function inside `src/Client/App.fs`:

    - urlUpdate function
        ```fsharp
        let urlUpdate (result:Page option) model =
            match result with
            //...
            | Some Page.Tomato ->
                { model with PageModel = TomatoModel }, Cmd.none
        ```
4. Fix up incomplete pattern matches in the `hydrateModel` function inside `src/Client/App.fs`:

    - hydrateModel function
        ```fsharp
        let hydrateModel (json:string) (page: Page option) : Model * Cmd<_> =
            //...
            match page, model.PageModel with
            //...
            | Some Page.Tomato, TomatoModel -> model, Cmd.none
            | _, HomePageModel
            //...
            | _, TomatoModel ->
                // unknown page or page does not match model -> go to home page
                { User = None; PageModel = HomePageModel }, Cmd.none
        ```

5. Try it out by navigating to `http://localhost:8080/tomato`

You should see `Tomatoes taste good!`

#### Adding the page to the menu

Inside `src/Client/views/Menu.fs`:

```fsharp
let inline private clientView onLogout (model:Model) =
    div [ centerStyle "row" ] [
        //...
        yield viewLink Page.Tomato "Tomato"
        //...
```

#### Move code to separate Tomato.fs files

1. Add a new .fs file to the pages folder: `src/Client/pages/Tomato.fs`.
Add the `src/Client/pages/Tomato.fs` to your `Client.fsproj` and `Server.fsproj` files and move it above `Shared.fs`.

    - `Client.fsproj`
    ```xml
    <Compile Include="pages/Login.fs" />
    <Compile Include="pages/Tomato.fs" /> <!-- <- our page -->
    <Compile Include="Shared.fs" />
    ```
    - `Server.fsproj`
    ```xml
    <Compile Include="../Client/pages/Login.fs" />
    <Compile Include="../Client/pages/Tomato.fs" />  <!-- <- our page -->
    <Compile Include="../Client/Shared.fs" />
    ```

2. Place following code in the `src/Client/pages/Tomato.fs`:

    ```fsharp
    module Client.Tomato

    open Client.Styles

    let view() =
        words 60 "Tomatoes taste VERY good!"
    ```

3. Remove old 'view' code from the `view` function in `src/Client/Shared.fs` and replace it
    with:
    ```fsharp
    | TomatoModel ->
        Tomato.view ()
    ```

#### Define a model for the page that holds the state

1. Replace the code in `src/Client/pages/Tomato.fs` with

    ```fsharp
    module Client.Tomato

    open Client.Styles
    open Fable.React

    type Model = {
        Color:string
    }

    let init() =
        { Color = "red" }

    let view model =
        div []
            [
                div [] [words 60 "Tomatoes taste VERY good!"]
                div [] [words 20 (sprintf "The color of a tomato is %s" model.Color)]
            ]
    ```

2. Adjust the `PageModel` discriminated union in `Shared.fs`

    ```fsharp
    type PageModel =
        | HomePageModel
        | LoginModel of Login.Model
        | WishListModel of WishList.Model
        | TomatoModel of Tomato.Model
    ```

3. `urlUpdate` should now call the `init` function and set `Tomato.Model` as the new `PageModel`
    ```fsharp
    | Some Page.Tomato ->
        let m = Tomato.init()
        { model with PageModel = TomatoModel m }, Cmd.none
    ```

4. `hydrateModel` needs to explicitly ignore the payload of the TomatoModel case in its pattern match
    ```fsharp
        //...
        | _, TomatoModel _ ->
    ```

5. The `view` function in `Shared.fs` should call the `view` function of the the Tomato module and pass in the page model if it is a `TomatoModel`
    ```fsharp
    | TomatoModel m ->
        yield Tomato.view m
    ```

#### Make it interactive (update the state)

1. Add a new message DU in `src/Client/pages/Tomato.fs`
    ```fsharp
    type Msg =
        | ChangeColor of string
    ```

2. Add a message to the `Msg` DU in `src/Client/Shared.fs`
    ```fsharp
    type Msg =
        //...
        | TomatoMsg of Tomato.Msg
    ```

3. Adjust the match pattern in the `update` function of `src/Client/App.fs`
    ```fsharp
    | TomatoMsg msg, TomatoModel tm ->
        let color = match msg with Tomato.Msg.ChangeColor c -> c
        let tm = { tm with Color = color }
        { model with PageModel = TomatoModel tm }, Cmd.none

    | TomatoMsg msg, _ -> model, Cmd.none // in case we receive a delayed message originating from the previous page
    ```

4. Change the `Tomato.view` function (and open another namespace):

    ```fsharp
    open Fable.React.Props
    //...
    let view model dispatch =
        div []
            [
                words 60 "Tomatoes taste VERY good!"
                words 20 (sprintf "The color of a tomato is %s" model.Color)
                br []
                button [
                    ClassName ("btn btn-primary")
                    OnClick (fun _ -> dispatch (ChangeColor "green"))]
                    [ str "No, my tomatoes are green!" ]
            ]
    ```

5. Edit `Shared.view` and pass the `dispatch` function to `Tomato.view`, remapping `Tomato.Msg` onto `App.Msg`

    ```fsharp
    | TomatoModel m ->
        yield Tomato.view m (TomatoMsg >> dispatch)
    ```

## Debugging

The server side of the application can be debugged using Ionide.

1. Run `build.cmd` \ `build.sh` to restore everything properly
2. Open repo in VSCode
3. Open debug panel, choose `Debug` from combobox, and press green arrow (or `F5`). This will build server and start it with debugger attached. It will also start Fable watch mode for the Client side and open the browser.

Client side debugging is supported by any modern browser with any developer tools.
Fable even provides source maps which will let you put breakpoints in F# source code (in browser dev tools).
Also, we additionally suggest installing React-devtools (for better UI debugging) and Redux-devtools (time travel debugger).

## Technology stack

### Giraffe on .NET Core

The webserver backend is running as a [Giraffe](https://github.com/giraffe-fsharp/Giraffe) service on ASP.NET Core.

In development mode the server is automatically restarted whenever a file in `src/Server` is saved.

### Freya on .NET Core

The SAFE stack does not force you to use Giraffe/Saturn. Check out the [freya branch](https://github.com/SAFE-Stack/SAFE-BookStore/tree/freya) for an alternative implementation of the backend.

If you are new to Freya, you can find an introduction at:
- [Freya website](https://docs.freya.io/en/latest/tutorials/getting-started-netcore.html)
- [Building a Highly Concurrent, Functional Web Server on .NET Core](https://skillsmatter.com/skillscasts/9887-building-a-highly-concurrent-functional-web-server-on-dot-net-core)
- [Freya F# for HTTP Systems](https://www.youtube.com/watch?v=Cu10EoxRz_s)

### React/Elmish client

The client is [React](https://facebook.github.io/react/) single page application that uses [fable-elmish](https://elmish.github.io/).

The communication to the server is done via HTTPS calls to `/api/*`. If a user is logged in then a [JSON Web Token](https://jwt.io/) is sent to the server with every request.

### Fable

The [Fable](http://fable.io/) compiler is used to compile the F# client code to JavaScript so that it can run in the browser.

### Shared code between server and client

"Isomorphic F#" started a bit as a joke about [Isomorphic JavaScript](http://isomorphic.net/). The naming is really bad, but the idea to have the same code running on client and server is really interesting.
If you look at `src/Server/Shared/Domain.fs` then you will see code that is shared between client and server. On the server it is compiled to .NET core and for the client the Fable compiler is translating it into JavaScript.
This is a really convenient technique for a shared domain model.


### Server-Side Rendering

This sample uses Server-Side Rendering (SSR) with [fable-react](https://github.com/fable-compiler/fable-react). This means the starting page is rendered on the ASP.NET Core server and sent as HTML to the client.
This allows for better Search Engine Optimization and gives faster initial response, especially on mobile devices. Everything else is then rendered via [React](https://reactjs.org/) on the client.

More info can be found in the [SSR tutorial](https://github.com/fable-compiler/fable-react/blob/master/docs/server-side-rendering.md).


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

The deployment for this repo works via [docker](https://www.docker.com/) with Linux containers and therefore you need docker installed on your machine.

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
This will build the server and client, run all test, put the app into a docker container and push it to your docker hub repo.

#### Azure Portal

Go to the [Azure Portal](https://portal.azure.com) and create a new "Web App for Containers".
Configure the Web App to point to the docker repo and select `latest` channel of the container.

![Docker setup](https://user-images.githubusercontent.com/57396/31279587-e06001d0-aaa9-11e7-9b4b-a3e8278a6419.png)

Also look for the "WebHook Url" on the portal, copy that url and set it as new trigger in your Docker Hub repo.

*Note that entering a Startup File is not necessary.*

The `Dockerfile` used to create the docker image exposes port 8085 for the Giraffe server application. This port needs to be mapped to port 80 within the Azure App Service for the application to receive http traffic.

Presently this can only be done using the Azure CLI. You can do this easily in Azure Cloud Shell (accessible from the Azure Portal in the top menu bar) using the following command:

`az webapp config appsettings set --resource-group <resource group name> --name <web app name> --settings WEBSITES_PORT=8085`

The above command is effectively the same as running `docker run -p 80:8085 <image name>`.

Now you should be able to reach the website on your `.azurewebsites.net` url.

#### Further releases

Now everything is set up. By creating new entries in [RELEASE_NOTES.md] and a new run of `release.cmd` the website should update automatically.

#### Azure Storage

With the steps above the website is only using local file storage. If you want to use it together with Azure Storage, then go back to the [Azure Portal](https://portal.azure.com) and create a new "Storage account". Copy the Connection String from "Access keys" tab and move over to your Azure app service.

![Storage Account](https://user-images.githubusercontent.com/57396/31279525-a15fe1f8-aaa9-11e7-9639-f04021a1ab49.png)

### Google Cloud AppEngine

The repository comes with a sample `app.yaml` file which is used to deploy to Google Cloud AppEngine using the custom flex environment. At the moment it seems like the application must run on port `8080` and that is set as a environment variable in the `app.yaml` file. When you run the deploy command it will first look for the `app.yaml` file and then look for the `Dockerfile` for what should deploy. The container that is deploy is exactly that same as the one that should have been deployed to Azure, but it is only set up to deploy from local to Google Cloud at the moment, and not from CI server to Google Cloud.

Before you can execute the deploy command you also need to build the solution with the `Publish` target, that is so the container image will get the compiled binaries when the container build is executed via the deploy command.

To execute the deploy you need a Google Cloud account and project configured as well as the tooling installed, https://cloud.google.com/sdk/downloads. The command you need to run is:

    gcloud app deploy -q <--version=main> <--verbosity=debug>

The `version` and `verbosity` flag isn't need, but it is recommended to use the `version` flag so you don't end up with too many versions of your application without removing previous ones. Use `verbosity=debug` if you are having some problems.

Deploy to the flex environment with a custom runtime like this is might take some time, but the instructions here should work.

#### Workaround for Split Health Checks

Newly created projects have [Split Health Checks](https://cloud.google.com/appengine/docs/flexible/python/configuring-your-app-with-app-yaml#updated_health_checks) enabled by default, which causes the deployment to fail. This can be resolved by disabling them on the project.

First install the beta components if you don't already have them:

    gcloud components install beta

then run the following command on your project:

    gcloud beta app update --no-split-health-checks --project <YOUR PROJECT ID>

After that, deploying as described above should work just fine.

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

## Maintainer(s)

- [@forki](https://github.com/forki)
- [@alfonsogarciacaro](https://github.com/alfonsogarciacaro)
