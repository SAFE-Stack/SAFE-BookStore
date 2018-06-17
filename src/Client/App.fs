module Client.App

open Fable.Core
open Fable.Core.JsInterop

open Fable.Import
open Fable.PowerPack
open Elmish
open Elmish.React
open Elmish.Remoting
open Elmish.Browser.Navigation
open Elmish.HMR
open Client.Shared
open Client.Pages
open ServerCode.Domain

JsInterop.importSideEffects "whatwg-fetch"
JsInterop.importSideEffects "babel-polyfill"

let handleNotFound (model: Model) =
    Browser.console.error("Error parsing url: " + Browser.window.location.href)
    ( model, Navigation.modifyUrl (toPath Page.Home) )

/// The navigation logic of the application given a page identity parsed from the .../#info
/// information in the URL.
let urlUpdate (result:Page option) (model: Model) =
    match result with
    | None ->
        handleNotFound model

    | Some Page.Login ->
        let m, cmd = Login.init model.User
        { model with PageModel = LoginModel m }, Cmd.map LoginMsg cmd

    | Some Page.WishList ->
        match model.User with
        | Some user ->
            let m, cmd = WishList.init user
            { model with PageModel = WishListModel m }, Cmd.map (function C msg -> WishListMsg msg | S _ -> NoOp) cmd
        | None ->
            model, Cmd.ofMsg (Logout ())

    | Some Page.Home ->
        { model with PageModel = HomePageModel }, Cmd.none

let loadUser () : UserData option =
    BrowserLocalStorage.load "user"

let saveUserCmd user =
    Cmd.ofFunc (BrowserLocalStorage.save "user") user (fun _ -> LoggedIn user) StorageFailure

let deleteUserCmd =
  Cmd.batch [
    Cmd.ofFunc BrowserLocalStorage.delete "user" (fun _ -> C LoggedOut) (StorageFailure>>C)
    Cmd.ofMsg (S ClearUser)
    ]
let init result =
    let user = loadUser ()
    let stateJson: string option = !!Browser.window?__INIT_MODEL__
    match stateJson, result with
    | Some json, Some Page.Home ->
        let model: Model = ofJson json
        { model with User = user }, Cmd.none
    | _ ->
        let model =
            { User = user
              PageModel = HomePageModel }
        let model,cmd = urlUpdate result model
        model, Cmd.map C cmd

let update msg model =
    match msg, model.PageModel with
    | Connected, _ ->
        model,
            match model.User with
            |Some {Token=token} -> Cmd.ofMsg (S (SendToken token))
            |None -> Cmd.none
    | NoOp, _ -> model, Cmd.none
    | StorageFailure e, _ ->
        printfn "Unable to access local storage: %A" e
        model, Cmd.none

    | LoginMsg msg, LoginModel m ->
        let m, cmd, externalMsg = Login.update msg m

        let cmd2 =
            match externalMsg with
            | Login.ExternalMsg.NoOp ->
                Cmd.none
            | Login.ExternalMsg.UserLoggedIn newUser ->
                saveUserCmd newUser

        { model with
            PageModel = LoginModel m },
                Cmd.batch [
                    Cmd.remoteMap LoginServerMsg LoginMsg cmd
                    Cmd.map C cmd2 ]

    | LoginMsg _, _ -> model, Cmd.none

    | WishListMsg msg, WishListModel m ->
        let m, cmd = WishList.update msg m
        { model with
            PageModel = WishListModel m }, Cmd.remoteMap WishListServerMsg WishListMsg cmd

    | WishListMsg _, _ ->
        model, Cmd.none

    | LoggedIn newUser, _ ->
        let nextPage = Page.WishList
        { model with User = Some newUser },
        Navigation.newUrl (toPath nextPage)

    | LoggedOut, _ ->
        { model with
            User = None
            PageModel = HomePageModel },
        Navigation.newUrl (toPath Page.Home)

    | Logout(), _ ->
        model, deleteUserCmd


open Elmish.Debug
open ServerCode.ServerUrls

let withReact =
    if (!!Browser.window?__INIT_MODEL__)
    then Program.withReactHydrate
    else Program.withReact


// App
RemoteProgram.mkProgram init update view
|> RemoteProgram.programBridgeWithMap UserMsg (Program.toNavigable Pages.urlParser urlUpdate)
#if DEBUG
|> RemoteProgram.programBridge Program.withConsoleTrace
|> RemoteProgram.programBridgeWithMap Program.UserMsg Program.withHMR
#endif
|> RemoteProgram.programBridge (withReact "elmish-app")
#if DEBUG
|> RemoteProgram.programBridge Program.withDebugger
#endif
|> RemoteProgram.runAt APIUrls.Socket
