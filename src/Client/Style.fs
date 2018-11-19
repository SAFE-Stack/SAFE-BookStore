module Client.Style

open Fable.Helpers.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.PowerPack
open Elmish.Browser.Navigation
module R = Fable.Helpers.React


let goToUrl (e: React.MouseEvent) =
    e.preventDefault()
    let href = !!e.target?href
    Navigation.newUrl href |> List.map (fun f -> f ignore) |> ignore

let viewLink page description =
    R.a [ Style [ Padding "0 20px" ]
          Href (Pages.toPath page)
          OnClick goToUrl]
        [ R.str description]

let centerStyle direction =
    Style [ Display "flex"
            FlexDirection direction
            AlignItems "center"
            JustifyContent "center"
            Padding "20px 0"
    ]

let words size message =
    R.span [ Style [ FontSize (size |> sprintf "%dpx") ] ] [ R.str message ]

let buttonLink cssClass onClick elements =
    R.a [ ClassName cssClass
          OnClick (fun _ -> onClick())
          OnTouchStart (fun _ -> onClick())
          Style [ Cursor "pointer" ] ] elements

// let validatedTextBox errorText key =
//     R.div [ClassName ("form-group has-feedback " + linkStatus)] [
//          yield R.div [ClassName "input-group"] [
//              yield R.span [ClassName "input-group-addon"] [R.span [ClassName "glyphicon glyphicon glyphicon-pencil"] [] ]
//              yield R.input [
//                     Key ("Link_" + model.NewBookId.ToString())
//                     HTMLAttr.Type "text"
//                     Name "Link"
//                     DefaultValue model.NewBook.Link
//                     ClassName "form-control"
//                     Placeholder "Please insert link"
//                     Required true
//                     OnChange (fun ev -> dispatch (LinkChanged ev.Value))]
//              match errorText with
//              | Some e -> yield R.span [ClassName "glyphicon glyphicon-remove form-control-feedback"] []
//              | _ -> ()
//          ]
//          match errorText with
//          | Some e -> yield R.p [ClassName "text-danger"][str e]
//          | _ -> ()
//     ]


let onEnter msg dispatch =
    function
    | (ev:React.KeyboardEvent) when ev.keyCode = Keyboard.Codes.enter ->
        ev.preventDefault()
        dispatch msg
    | _ -> ()
    |> OnKeyDown
