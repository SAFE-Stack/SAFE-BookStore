module Page.Login

open Elmish
open Feliz.DaisyUI

type Page =
    | Home
    | Login
    | Wishlist

type Model = { Page: Page }

type Msg =
    | NoOp

// let todosApi =
//     Remoting.createApi ()
//     |> Remoting.withRouteBuilder Route.builder
//     |> Remoting.buildProxy<ITodosApi>

let init () =
    let model = { Page = Home }
    model, Cmd.none

let update msg model =
    match msg with
    | NoOp -> model, Cmd.none

open Feliz

let view model dispatch =
    Html.div [
        prop.className "grid justify-center"
        prop.children [
            Daisy.card [
                prop.className "grid justify-items-center gap-2 w-[32rem] p-10 border-2"
                prop.children [
                    Html.text "Log in with 'test' / 'test'."
                    Daisy.formControl [
                        prop.className ""
                        prop.children [
                            Html.div [
                                prop.className "relative"
                                prop.children [
                                    Daisy.input [ input.bordered; prop.className ""; prop.placeholder "Username" ]
                                ]
                            ]
                        ]
                    ]
                    Daisy.formControl [
                        Html.div [
                            prop.className "relative"
                            prop.children [
                                Daisy.input [ input.bordered; prop.className ""; prop.placeholder "Password" ]
                            ]
                        ]
                    ]
                    Daisy.formControl [
                        Html.div [
                            prop.className ""
                            prop.children [
                                Daisy.button.button [ prop.text "Log In" ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

