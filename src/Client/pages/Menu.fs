module Client.Menu

open Fable.Core
open Fable.Import
open Elmish
open Fable.Import.Browser
open Fable.PowerPack
open Elmish.Browser.Navigation
open Elmish.Browser.UrlParser
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Client.Style
open System
open Fable.Core.JsInterop
open Client.Pages

/// The user data sent with every message.
type UserData = 
  { UserName : string 
    Token : ServerCode.Domain.JWT }

type Model = {
    User : UserData option
}

type Msg =
    | Logout

let init() = { User = Utils.load "user" }, Cmd.none

let update (msg:Msg) model : Model*Cmd<Msg> = 
    match msg with
    | Logout ->
        model, Cmd.none

let view (model:Model) dispatch =
    div [ centerStyle "row" ] [
          yield viewLink Page.Home "Home"
          if model.User <> None then 
              yield viewLink Page.WishList "Wishlist"
          if model.User = None then 
              yield viewLink Page.Login "Login" 
          else 
              yield buttonLink "logout" (fun _ -> dispatch Logout) [ str "Logout" ]
        ]