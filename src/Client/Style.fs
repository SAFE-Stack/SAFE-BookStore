module Client.Style

open Fable.Helpers.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
module R = Fable.Helpers.React


let viewLink page description =
  R.a [ Style [ Padding "0 20px" ]
        Href (Pages.toHash page) ]
      [ R.str description]

let centerStyle direction =
    Style [ Display "flex"
            FlexDirection direction
            AlignItems "center"
            !!("justifyContent", "center")
            Padding "20px 0"
    ]

let words size message =
    R.span [ Style [ !!("fontSize", size |> sprintf "%dpx") ] ] [ R.str message ]

let buttonLink cssClass onClick elements = 
    R.a [ ClassName cssClass
          OnClick (fun _ -> onClick())
          OnTouchStart (fun _ -> onClick())
          Style [ !!("cursor", "pointer") ] ] elements

let [<Literal>] ENTER_KEY = 13.

let onEnter msg dispatch =
    function 
    | (ev:React.KeyboardEvent) when ev.keyCode = ENTER_KEY ->
        ev.preventDefault()
        dispatch msg
    | _ -> ()
    |> OnKeyDown
   