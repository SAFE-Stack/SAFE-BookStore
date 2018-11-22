module Client.Shared

open ServerCode.Domain

type PageModel =
    | HomePageModel
    | LoginModel of Login.Model
    | WishListModel of WishList.Model

// DEMO03 - The complete app state
type Model = { 
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

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Client.Styles

// DEMO05 - the whole world put into a single view
let view model dispatch =
    div [ Key "Application" ] [
        Menu.view (Logout >> dispatch) model.User
        hr []

        div [ centerStyle "column" ] [
            match model.PageModel with
            | HomePageModel ->
                yield Home.view ()
            | LoginModel m ->
                yield Login.view m (LoginMsg >> dispatch)
            | WishListModel m ->
                yield WishList.view m (WishListMsg >> dispatch) 
        ]
    ]
