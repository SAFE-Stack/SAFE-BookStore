module Page.Login

open System
open Elmish
open Feliz.DaisyUI
open FsToolkit.ErrorHandling

type Model = {
    Username: string
    Password: string
    FormErrors: string list
}

type Msg =
    | SetUsername of string
    | SetPassword of string
    | Login

let validateUsername name =
    if String.IsNullOrWhiteSpace name |> not then
        Ok name
    else
        Error "You need to fill in a username."

let validatePassword password =
    if String.IsNullOrWhiteSpace password |> not then
        Ok password
    else
        Error "You need to fill in a password."

let validateForm name password = validation {
    let! name = validateUsername name
    and! password = validatePassword password
    return {| Username = name; Password = password |}
}

let init () =
    let model = {
        Username = ""
        Password = ""
        FormErrors = []
    }
    model, Cmd.none

let update msg model =
    match msg with
    | SetUsername input -> { model with Username = input }, Cmd.none
    | SetPassword input -> { model with Username = input }, Cmd.none
    | Login ->
        let form = validateForm model.Username model.Password

        model, Cmd.none

open Feliz

let view model dispatch =
    Html.div [
        prop.className "grid justify-center"
        prop.children [
            Daisy.card [
                prop.className "shadow-lg grid justify-items-center gap-2 w-[32rem] p-10"
                prop.children [
                    Html.h2 [ prop.text "Log in with 'test' / 'test'." ]
                    for error in model.FormErrors do
                        Html.text error
                    Daisy.formControl [
                        prop.className ""
                        prop.children [
                            Html.div [
                                prop.className ""
                                prop.children [
                                    Daisy.input [
                                        input.bordered
                                        prop.className ""
                                        prop.placeholder "Username"
                                        prop.onChange (SetUsername >> dispatch)
                                    ]
                                ]
                            ]
                        ]
                    ]
                    Daisy.formControl [
                        Html.div [
                            prop.className ""
                            prop.children [
                                Daisy.input [
                                    input.bordered
                                    prop.className ""
                                    prop.placeholder "Password"
                                    prop.onChange (SetPassword >> dispatch)
                                ]
                            ]
                        ]
                    ]
                    Daisy.formControl [
                        Html.div [
                            prop.className ""
                            prop.children [
                                Daisy.button.button [ prop.text "Log In"; prop.onClick (fun _ -> dispatch Login)]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

