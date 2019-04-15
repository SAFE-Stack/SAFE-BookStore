module Client.Menu

open Fable.React
open Client.Styles
open Client.Pages
open ServerCode.Domain

type Model = UserData option

let view onLogout (model:Model) =
    div [ centerStyle "row" ] [
        yield viewLink Page.Home "Home"
        if model <> None then
            yield viewLink Page.WishList "Wishlist"
        if model = None then
            yield viewLink Page.Login "Login"
        else
            yield buttonLink "logout" onLogout [ str "Logout" ]
    ]
