module Client.Menu

open Fable.React
open Client.Styles
open Client.Pages
open Client.Utils
open ServerCode.Domain

type Model = UserData option

type Props = {
    Model: Model
    OnLogout: unit -> unit
}

let view = elmishView "Menu" <| fun ({ OnLogout = onLogout; Model = model }) ->
    div [ centerStyle "row" ] [
        yield viewLink Page.Home "Home"
        if model <> None then
            yield viewLink Page.WishList "Wishlist"
        if model = None then
            yield viewLink Page.Login "Login"
        else
            yield buttonLink "logout" onLogout [ str "Logout" ]
    ]
