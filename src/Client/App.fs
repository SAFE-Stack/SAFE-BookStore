module Client.App

open Fable.Core.JsInterop
open Fable.Import
open Elmish
open Elmish.React
open Elmish.HMR
open Client.Shared
open Client.Pages
open ServerCode.Domain
open Thoth.Json
open Fable.Core
open Elmish.Navigation

let handleNotFound (model: Model) =
    JS.console.error("Error parsing url: " + Browser.Dom.window.location.href)
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
            model, Cmd.OfFunc.result (Logout ())

    | Some Page.Home ->
        { model with PageModel = HomePageModel }, Cmd.none

let loadUser () : UserData option =
    let userDecoder = Decode.Auto.generateDecoder<UserData>()
    match LocalStorage.load userDecoder "user" with
    | Ok user -> Some user
    | Error _ -> None

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
    // was the page rendered server-side?
    let stateJson: string option = !!Browser.Dom.window?__INIT_MODEL__

    match stateJson with
    | Some json ->
        // SSR -> hydrate the model
        let model, cmd = hydrateModel json page
        { model with User = loadUser() }, cmd
    | None ->
        // no SSR -> show home page
        let model =
            { User = loadUser()
              PageModel = HomePageModel }

        urlUpdate page model

let update msg model =
    match msg, model.PageModel with
    | StorageFailure e, _ ->
        printfn "Unable to access local storage: %A" e
        model, Cmd.none

    | LoginMsg msg, LoginModel m ->
        match msg with
        | Login.Msg.LoginSuccess newUser ->
            model, Cmd.OfFunc.either (LocalStorage.save "user") newUser (fun _ -> LoggedIn newUser) StorageFailure
        | _ ->
            let m, cmd = Login.update msg m

            { model with
                PageModel = LoginModel m }, Cmd.map LoginMsg cmd

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
        model, Cmd.OfFunc.either LocalStorage.delete "user" (fun _ -> LoggedOut) StorageFailure


open Elmish.Debug

let withReact =
    if (!!Browser.Dom.window?__INIT_MODEL__)
    then Program.withReactHydrate
    else Program.withReactSynchronous


// App
Program.mkProgram init update view
|> Program.toNavigable Pages.urlParser urlUpdate
#if DEBUG
|> Program.withConsoleTrace
#endif
|> withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
