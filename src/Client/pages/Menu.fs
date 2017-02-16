module Client.Menu

open Fable.Core
open Fable.Import
open Elmish
open Fable.Import.Browser
open Fable.PowerPack
open Elmish.Browser.Navigation
open Elmish.UrlParser
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Client.Style
open Client.Messages
open System
open Fable.Core.JsInterop

type Model = {
    User : UserData option
    query : string
}

let init() = { User = Utils.load "user"; query = "" },Cmd.none

let view (model:Model) dispatch =
    div [ centerStyle "row" ] [ 
          viewLink Home "Home"
          viewLink (Blog 42) "Cat Facts"
          viewLink (Blog 13) "Alligator Jokes"
          (if model.User = None then viewLink (Login) "Login" else buttonLink "" (fun _ -> dispatch Logout) [ text "Logout" ])
          input
            [ Placeholder "Enter a zip code (e.g. 90210)"
              Value (U2.Case1 model.query)
              onEnter Enter dispatch
              OnInput (fun ev -> Query (unbox ev.target?value) |> dispatch)
              Style [ CSSProp.Width "200px"; Margin "0 20px" ]
            ]
            []
        ]