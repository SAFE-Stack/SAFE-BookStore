module Client.Style

open System
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core
open Fable.Import
open Elmish
open Fable.Import.Browser
open Fable.PowerPack
open Elmish.Browser.Navigation
open Elmish.UrlParser
open Messages


let viewLink page description =
  a [ Style [ Padding "0 20px" ]
      Href (toHash page) ]
    [ unbox description]

let internal centerStyle direction =
    Style [ Display "flex"
            FlexDirection direction
            AlignItems "center"
            unbox("justifyContent", "center")
            Padding "20px 0"
    ]

let words size message =
    span [ Style [ unbox("fontSize", size |> sprintf "%dpx") ] ] [ unbox message ]


let text s = text [] [unbox s ]

let buttonLink cssClass onClick elements = 
    a [ ClassName cssClass
        OnClick (fun _ -> onClick())
        OnTouchStart (fun _ -> onClick())
        Style [ unbox("cursor", "pointer") ] ] elements

let internal onEnter msg dispatch =
    function
    | (ev:React.KeyboardEvent) when ev.keyCode = 13. ->
        ev.preventDefault()
        dispatch msg
    | _ -> ()
    |> OnKeyDown
