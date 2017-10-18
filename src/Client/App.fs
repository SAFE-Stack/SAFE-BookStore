module Client.App

open Fable.Core
open Fable.Core.JsInterop

open Fable.Import
open Elmish
open Elmish.React
open Fable.Import.Browser
open Elmish.Browser.Navigation
open Elmish.HMR
open Client.Pages

JsInterop.importSideEffects "whatwg-fetch"
JsInterop.importSideEffects "babel-polyfill"

// Model

type PageModel =
    | HomePageModel
    | LoginModel of Login.Model
    | WishListModel of WishList.Model

type Msg =
    | LoggedIn
    | LoggedOut
    | StorageFailure of exn
    | OpenLogIn
    | MenuMsg of Menu.Msg
    | LoginMsg of Login.Msg
    | WishListMsg of WishList.Msg
    | Logout

type Model =
  { Menu : Menu.Model
    PageModel : PageModel }

let urlUpdate (result:Page option) model =
    match result with
    | None ->
        Browser.console.error("Error parsing url: " + Browser.window.location.href)
        ( model, Navigation.modifyUrl (toHash Page.Home) )

    | Some Page.Login ->
        let m,cmd = Login.init model.Menu.User
        { model with PageModel = LoginModel m }, Cmd.map LoginMsg cmd

    | Some Page.WishList ->
        match model.Menu.User with
        | Some user ->
            let m,cmd = WishList.init user
            { model with PageModel = WishListModel m }, Cmd.map WishListMsg cmd
        | None ->
            model, Cmd.ofMsg Logout

    | Some Page.Home ->
        { model with PageModel = HomePageModel }, Cmd.none

let init result =
    let menu,menuCmd = Menu.init()
    let m =
        { Menu = menu
          PageModel = HomePageModel }

    let m,cmd = urlUpdate result m
    m,Cmd.batch[cmd; menuCmd]

let update msg model =
    match msg, model.PageModel with
    | OpenLogIn, _ ->
        let m,cmd = Login.init None
        { model with PageModel = LoginModel m }, Cmd.batch [cmd; Navigation.newUrl (toHash Page.Login) ]

    | StorageFailure e, _ ->
        printfn "Unable to access local storage: %A" e
        model, []

    | LoginMsg msg, LoginModel m ->
        let m,cmd = Login.update msg m
        let cmd = Cmd.map LoginMsg cmd
        match m.State with
        | Login.LoginState.LoggedIn token ->
            let newUser : Menu.UserData = { UserName = m.Login.UserName; Token = token }
            let cmd =
                if model.Menu.User = Some newUser then cmd else
                Cmd.batch [cmd
                           Cmd.ofFunc (Utils.save "user") newUser (fun _ -> LoggedIn) StorageFailure ]

            { model with
                PageModel = LoginModel m
                Menu = { model.Menu with User = Some newUser }}, cmd
        | _ ->
            { model with
                PageModel = LoginModel m
                Menu = { model.Menu with User = None } }, cmd

    | LoginMsg _, _ -> model, Cmd.none

    | MenuMsg msg, _ ->
        match msg with
        | Menu.Msg.Logout ->
            model, Cmd.ofMsg Logout

    | WishListMsg msg, WishListModel m ->
        let m,cmd = WishList.update msg m
        let cmd = Cmd.map WishListMsg cmd
        { model with
            PageModel = WishListModel m }, cmd

    | WishListMsg _, _ -> model, Cmd.none

    | LoggedIn, _ ->
        let nextPage = Page.WishList
        let m,cmd = urlUpdate (Some nextPage) model
        match m.Menu.User with
        | Some _ ->
            m, Cmd.batch [cmd; Navigation.newUrl (toHash nextPage) ]
        | None ->
            m, Cmd.ofMsg Logout

    | LoggedOut, _ ->
        { model with
            PageModel = HomePageModel
            Menu = { model.Menu with User = None } },
        Navigation.newUrl (toHash Page.Home)

    | Logout, _ ->
        model, Cmd.ofFunc Utils.delete "user" (fun _ -> LoggedOut) StorageFailure

// VIEW

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Client.Style

/// Constructs the view for a page given the model and dispatcher.
let viewPage model dispatch =
    match model.PageModel with
    | HomePageModel ->
        Home.view ()

    | LoginModel m ->
        [ Login.view m (LoginMsg >> dispatch) ]

    | WishListModel m ->
        [ WishList.view m (WishListMsg >> dispatch) ]

/// Constructs the view for the application given the model.
let view model dispatch =
  div []
    [ Menu.view model.Menu (MenuMsg >> dispatch)
      hr []
      div [ centerStyle "column" ] (viewPage model dispatch)
    ]

open Elmish.React
open Elmish.Debug

// App
Program.mkProgram init update view
|> Program.toNavigable Pages.urlParser urlUpdate
#if DEBUG
|> Program.withConsoleTrace
|> Program.withHMR
#endif
|> Program.withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
