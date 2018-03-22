module Client.Menu

open Fable.Helpers.React
open Fable.Helpers.Isomorphic
open Client.Style
open Client.Pages
open ServerCode.Domain

type Model = UserData option
let inline private clientView onLogout (model:Model) =
    div [ centerStyle "row" ] [
          yield viewLink Page.Home "Home"
          if model <> None then
              yield viewLink Page.WishList "Wishlist"
          if model = None then
              yield viewLink Page.Login "Login"
          else
              yield buttonLink "logout" onLogout [ str "Logout" ]
        ]

let inline private serverView onLogout (model: Model) =
    clientView onLogout None

let view onLogout model =
    isomorphicView (clientView onLogout) (serverView onLogout) model
