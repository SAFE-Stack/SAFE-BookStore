module Client.Menu

open Fable.React
open Client.Styles
open Client.Pages
open Client.Utils
open ServerCode.Domain

type Model = UserData option

type Props = {
    OnLogout: unit -> unit
    Model: Model
}

let view = elmishView "Menu" <| fun p ->
    div [ centerStyle "row" ] [
        yield viewLink Page.Home "Home"
        if p.Model <> None then
            yield viewLink Page.WishList "Wishlist"
        if p.Model = None then
            yield viewLink Page.Login "Login"
        else
            yield buttonLink "logout" p.OnLogout [ str "Logout" ]
    ]
