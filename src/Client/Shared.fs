module Client.Shared

open ServerCode.Domain

type PageModel =
    | HomePageModel of Home.Model
    | LoginModel of Login.Model
    | WishListModel of WishList.Model

type Model = {
    MenuModel : Menu.Model
    PageModel : PageModel
}

/// The composed set of messages that update the state of the application
type Msg =
    | AppHydrated
    | WishListMsg of WishList.Msg
    | HomePageMsg of Home.Msg
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
        Menu.view (Logout >> dispatch) model.MenuModel
        hr []

        div [ centerStyle "column" ] [
            match model.PageModel with
            | HomePageModel model ->
                yield Home.view model
            | LoginModel m ->
                yield Login.view { Model = m; Dispatch = (LoginMsg >> dispatch) }
            | WishListModel m ->
                yield WishList.view { Model = m; Dispatch = (WishListMsg >> dispatch) }
        ]
    ]
