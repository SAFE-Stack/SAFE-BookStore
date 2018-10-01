module Client.App

open Fable.Core
open Fable.Core.JsInterop

open Fable.Import
open Fable.PowerPack
open Elmish
open Elmish.React
open Elmish.Browser.Navigation
open Elmish.HMR
open Client.Shared
open Client.Pages
open ServerCode.Domain
open Thoth.Json

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
            { model with PageModel = WishListModel m }, Cmd.map WishListMsg cmd
        | None ->
            model, Cmd.ofMsg (Logout ())

    | Some Page.Home ->
        { model with PageModel = HomePageModel }, Cmd.none

let loadUser () : UserData option =
    let userDecoder = Decode.Auto.generateDecoder<UserData>()
    match BrowserLocalStorage.load userDecoder "user" with
    | Ok user -> Some user
    | Error _ -> None

let saveUserCmd user =
    Cmd.ofFunc (BrowserLocalStorage.save "user") user (fun _ -> LoggedIn user) StorageFailure

let deleteUserCmd =
    Cmd.ofFunc BrowserLocalStorage.delete "user" (fun _ -> LoggedOut) StorageFailure


let hydrateModel (json:string) (page: Page option) : Model * Cmd<_> =
    // The page was rendered server-side and now react client-side kicks in.
    // If needed, the model could be fixed up here.
    // In this case we just deserialize the model from the json and don't need to to anything special.
    let model: Model = Decode.Auto.unsafeFromString(json)
    match page, model.PageModel with
    | Some Page.Home, HomePageModel -> model, Cmd.none
    | Some Page.Login, LoginModel _ -> model, Cmd.none
    | Some Page.WishList, WishListModel _ -> model, Cmd.none
    | _, HomePageModel |  _, LoginModel _ |  _, WishListModel _ ->
        // unknown page or page does not match model -> go to home page
        { User = None; PageModel = HomePageModel }, Cmd.none

let init page =
    let user = loadUser ()
    // was the page rendered server-side?
    let stateJson: string option = !!Browser.window?__INIT_MODEL__
    match stateJson with
    | Some json ->
        // SSR -> hydrate the model
        let model, cmd = hydrateModel json page
        { model with User = user }, cmd
    | None ->
        // no SSR -> show home page
        let model =
            { User = user
              PageModel = HomePageModel }

        urlUpdate page model

let update msg model =
    printfn "update"
    match msg, model.PageModel with
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
                    Cmd.map LoginMsg cmd
                    cmd2 ]

    | LoginMsg _, _ -> model, Cmd.none

    | WishListMsg msg, WishListModel m ->
        let m, cmd = WishList.update msg m
        { model with
            PageModel = WishListModel m }, Cmd.map WishListMsg cmd

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

let withReact =
    if (!!Browser.window?__INIT_MODEL__)
    then Program.withReactHydrate
    else Program.withReact


// App
Program.mkProgram init update view
|> Program.toNavigable Pages.urlParser urlUpdate
#if DEBUG
|> Program.withConsoleTrace
|> Program.withHMR
#endif
|> withReact "elmish-app"
#if DEBUG
// The debugger isn't support yet for Fable 2
// |> Program.withDebugger
#endif
|> Program.run
