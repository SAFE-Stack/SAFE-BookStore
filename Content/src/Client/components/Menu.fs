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
open Client.Model
open Fable.Core.JsInterop

// Messages.

type Msg = 
  | LogOut


// Model.

type Model = {
    User : UserData option
    query : string
}

// State.

let init() = { User = Utils.load "user"; query = "" },Cmd.none

// View

let view (model:Model) dispatch =
    div [ centerStyle "row" ] [ 
          yield viewLink Home "Home"
          if model.User <> None then 
              yield viewLink Page.WishList "Wishlist"
          if model.User = None then 
              yield viewLink (Login) "Login" 
          else 
              yield buttonLink "logout" (fun _ -> dispatch LogOut) [ str "Logout" ]
        ]