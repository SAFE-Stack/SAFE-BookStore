module Client.App

open Fable.Core
open Fable.Core.JsInterop

open Fable.Core
open Fable.Import
open Elmish
open Elmish.React
open Fable.Import.Browser
open Fable.PowerPack
open Elmish.Browser.Navigation
open Client.Messages
open Elmish.Browser.UrlParser

// Model

type SubModel =
  | NoSubModel
  | LoginModel of Login.Model
  | WishListModel of WishList.Model

type Model =
  { Page : Page
    Menu : Menu.Model
    SubModel : SubModel }


/// The URL is turned into a Result.
let pageParser : Parser<Page->_,_> =
    oneOf
        [ map Home (s "home")
          map Page.Login (s "login")
          map WishList (s "wishlist") ]

let urlUpdate (result:Page option) model =
    match result with
    | None ->
        Browser.console.error("Error parsing url")
        ( model, Navigation.modifyUrl (toHash model.Page) )

    | Some (Page.Login as page) ->
        let m,cmd = Login.init model.Menu.User
        { model with Page = page; SubModel = LoginModel m }, Cmd.map LoginMsg cmd

    | Some (Page.WishList as page) ->
        match model.Menu.User with
        | Some user ->
            let m,cmd = WishList.init user
            { model with Page = page; SubModel = WishListModel m }, Cmd.map WishListMsg cmd
        | None ->
            model, Cmd.ofMsg Logout

    | Some (Home as page) ->
        { model with Page = page; Menu = { model.Menu with query = "" } }, []

let init result =
    let menu,menuCmd = Menu.init()
    let m =
        { Page = Home
          Menu = menu
          SubModel = NoSubModel }

    let m,cmd = urlUpdate result m
    m,Cmd.batch[cmd; menuCmd]

let update msg model =
    match msg with
    | AppMsg.OpenLogIn ->
        let m,cmd = Login.init None
        { model with
            Page = Page.Login
            SubModel = LoginModel m }, Cmd.batch [cmd; Navigation.modifyUrl (toHash Page.Login) ]

    | StorageFailure e ->
        printfn "Unable to access local storage: %A" e
        model, []

    | LoginMsg msg ->
        match model.SubModel with
        | LoginModel m ->
            let m,cmd = Login.update msg m
            let cmd = Cmd.map LoginMsg cmd
            match m.State with
            | Login.LoginState.LoggedIn token ->
                let newUser : UserData = { UserName = m.Login.UserName; Token = token }
                let cmd =
                    if model.Menu.User = Some newUser then cmd else
                    Cmd.batch [cmd
                               Cmd.ofFunc (Utils.save "user") newUser (fun _ -> LoggedIn) StorageFailure ]

                { model with
                    SubModel = LoginModel m
                    Menu = { model.Menu with User = Some newUser }}, cmd
            | _ ->
                { model with
                    SubModel = LoginModel m
                    Menu = { model.Menu with User = None } }, cmd
        | _ -> model, Cmd.none

    | WishListMsg msg ->
        match model.SubModel with
        | WishListModel m ->
            let m,cmd = WishList.update msg m
            let cmd = Cmd.map WishListMsg cmd
            { model with
                SubModel = WishListModel m }, cmd
        | _ -> model, Cmd.none

    | AppMsg.LoggedIn ->
        let nextPage = Page.WishList
        let m,cmd = urlUpdate (Some nextPage) model
        match m.Menu.User with
        | Some user ->
            m, Cmd.batch [cmd; Navigation.modifyUrl (toHash nextPage) ]
        | None ->
            m, Cmd.ofMsg Logout

    | AppMsg.LoggedOut ->
        { model with
            Page = Page.Home
            SubModel = NoSubModel
            Menu = { model.Menu with User = None } },
        Navigation.modifyUrl (toHash Page.Home)

    | AppMsg.Logout ->
        model, Cmd.ofFunc Utils.delete "user" (fun _ -> LoggedOut) StorageFailure

// VIEW

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Client.Style

/// Constructs the view for a page given the model and dispatcher.
let viewPage model dispatch =
    match model.Page with
    | Page.Home ->
        [ words 60 "Welcome!"
          a [ Href "http://fable.io" ] [ words 20 "Learn Fable at fable.io" ] ]

    | Page.Login ->
        match model.SubModel with
        | LoginModel m ->
            [ div [ ] [ Login.view m dispatch ]]
        | _ -> [ ]

    | Page.WishList ->
        match model.SubModel with
        | WishListModel m ->
            [ div [ ] [ lazyView2 WishList.view m dispatch ]]
        | _ -> [ ]

/// Constructs the view for the application given the model.
let view model dispatch =
  div []
    [ lazyView2 Menu.view model.Menu dispatch
      hr []
      div [ centerStyle "column" ] (viewPage model dispatch)
    ]

open Elmish.React
open Elmish.Debug

// App
Program.mkProgram init update view
|> Program.toNavigable (parseHash pageParser) urlUpdate
|> Program.withConsoleTrace
|> Program.withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run