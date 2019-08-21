module Client.Shared

open ServerCode.Domain

type PageModel =
    | HomePageModel
    | LoginModel of Login.Model
    | WishListModel of WishList.Model

type Model = {
    RenderedOnServer : bool
    User : UserData option
    PageModel : PageModel
}

/// The composed set of messages that update the state of the application
type Msg =
    | WishListMsg of WishList.Msg
    | LoginMsg of Login.Msg
    | LoggedIn of UserData
    | LoggedOut
    | StorageFailure of exn
    | Logout of unit


// VIEW

open Fable.React
open Fable.React.Props
open Client.Styles

let view model dispatch =
    div [ Key "Application" ] [
        Menu.view { Model = { User = model.User; RenderedOnServer = model.RenderedOnServer}; OnLogout = (Logout >> dispatch) }
        hr []

        div [ centerStyle "column" ] [
            match model.PageModel with
            | HomePageModel ->
                yield Home.view { Version = ReleaseNotes.Version }
            | LoginModel m ->
                yield Login.view { Model = m; Dispatch = (LoginMsg >> dispatch) }
            | WishListModel m ->
                yield WishList.view { Model = m; Dispatch = (WishListMsg >> dispatch) }
        ]
    ]
