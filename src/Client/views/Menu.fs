module Client.Menu

open Fable.React
open Fable.React.Props
open Client.Styles
open Client.Pages
open ServerCode.Domain

type Model = {
    User : UserData option
    RenderedOnServer : bool
}

let view onLogout (model:Model) =
    div [ centerStyle "row" ] [
        yield viewLink Page.Home "Home"
        match model.User with
        | Some _ ->
            yield viewLink Page.WishList "Wishlist"
            yield menuLink onLogout "Logout"
        | _ ->
            yield viewLink Page.Login "Login"

        if model.RenderedOnServer then
            yield str "Rendered on server"
        else
            yield str "Rendered on client"
    ]
