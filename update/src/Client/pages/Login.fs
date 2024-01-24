module Page.Login

open System
open Elmish
open Feliz.DaisyUI
open Feliz.Router
open FsToolkit.ErrorHandling
open Shared
open SAFE

type Model = {
    Username: string
    Password: string
    FormErrors: string list
}

type Msg =
    | SetUsername of string
    | SetPassword of string
    | Login
    | LoggedIn of UserData
    | StorageSuccess of unit
    | UnhandledError of exn

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
    return { UserName = name; Password = password }
}

let init () =
    let model = {
        Username = ""
        Password = ""
        FormErrors = []
    }
    model, Cmd.none

let update (userApi: IUserApi) msg model =
    match msg with
    | SetUsername input -> { model with Username = input }, Cmd.none
    | SetPassword input -> { model with Password = input }, Cmd.none
    | Login ->
        let form = validateForm model.Username model.Password
        let model, cmd =
            match form with
            | Ok form -> { model with FormErrors = [] }, Cmd.OfAsync.either userApi.login form LoggedIn UnhandledError
            | Error errors -> { model with FormErrors = errors }, Cmd.none
        model, cmd
    | LoggedIn user ->
        model, Cmd.OfFunc.either Session.saveUser user StorageSuccess UnhandledError
    | StorageSuccess _ ->
        model, Cmd.navigate "wishlist"
    | UnhandledError exn ->
        model, exn.AsAlert()

open Feliz

let view model dispatch =
    Html.div [
        prop.className "grid justify-center"
        prop.children [
            Daisy.card [
                prop.className "shadow-lg grid justify-items-center gap-2 w-[32rem] p-10"
                prop.children [
                    Html.h2 [ prop.text "Log in with 'test' / 'test'." ]
                    model.FormErrors
                    |> List.tryHead
                    |> Option.map (fun error -> Html.div [ color.textError; prop.text error ])
                    |> Option.defaultValue (React.fragment [])
                    Daisy.formControl [
                        prop.className ""
                        prop.children [
                            Html.div [
                                prop.className "relative"
                                prop.children [
                                    Html.i [ prop.className "fa fa-search absolute inset-y-0 end-0 grid items-center mr-2 text-teal-300" ]
                                    Daisy.input [
                                        input.bordered
                                        prop.className ""
                                        prop.placeholder "Username"
                                        prop.value model.Username
                                        prop.onChange (SetUsername >> dispatch)
                                    ]
                                ]
                            ]
                        ]
                    ]
                    Daisy.formControl [
                        Html.div [
                            prop.className "relative"
                            prop.children [
                                Html.i [ prop.className "fa fa-lock absolute inset-y-0 end-0 grid items-center mr-2 text-yellow-400" ]
                                Daisy.input [
                                    input.bordered
                                    prop.className ""
                                    prop.placeholder "Password"
                                    prop.value model.Password
                                    prop.onChange (SetPassword >> dispatch)
                                ]
                            ]
                        ]
                    ]
                    Daisy.formControl [
                        Html.div [
                            prop.className ""
                            prop.children [
                                Daisy.button.button [ prop.className "bg-teal-300"; prop.text "Log In"; prop.onClick (fun _ -> dispatch Login)]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

