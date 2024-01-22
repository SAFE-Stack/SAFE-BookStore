module Index

open Elmish
open Fable.Core
open Fable.Remoting.Client
open Feliz.DaisyUI
open Feliz.Router
open Page
open Shared

type PageTab =
    | Home of Home.Model
    | Login of Login.Model
    | NotFound

type Model = { Page: PageTab }

type Msg =
    | HomePageMsg of Home.Msg
    | LoginPageMsg of Login.Msg
    | UrlChanged of string list

// let todosApi =
//     Remoting.createApi ()
//     |> Remoting.withRouteBuilder Route.builder
//     |> Remoting.buildProxy<ITodosApi>

let initFromUrl url =
    match url with
    | [] ->
        let homeModel, homeMsg = Home.init ()
        let model = { Page = Home homeModel }
        let cmd = homeMsg |> Cmd.map HomePageMsg
        model, cmd
    | [ "login" ] ->
        let loginModel, loginMsg = Login.init ()
        let model = { Page = Login loginModel }
        let cmd = loginMsg |> Cmd.map LoginPageMsg
        model, cmd
    | _ -> { Page = NotFound }, Cmd.none

let init () =
    Router.currentUrl ()
    |> initFromUrl

let update msg model =
    match model.Page, msg with
    | Home homeModel, HomePageMsg homeMsg ->
        let newModel, cmd = Home.update homeMsg homeModel
        { Page = Home newModel }, cmd
    | Login loginModel, LoginPageMsg loginMsg ->
        let newModel, cmd = Login.update loginMsg loginModel
        { Page = Login newModel }, cmd
    | NotFound, _ ->
        { Page = NotFound }, Cmd.none
    | _, UrlChanged url -> initFromUrl url
    | _, _ ->
        model, Cmd.none

open Feliz

let logo =
    Html.a [
        prop.href "https://safe-stack.github.io/"
        prop.className "ml-12 h-12 w-12 bg-teal-300 hover:cursor-pointer hover:bg-teal-400"
        prop.children [ Html.img [ prop.src "/favicon.png"; prop.alt "Logo" ] ]
    ]

let navigation model dispatch =
    Daisy.tabs [
        prop.className "justify-self-center"
        prop.children [
            Daisy.tab "Home"
            Daisy.tab "Login"
        ]
    ]

let view model dispatch =
    React.router [
        router.onUrlChanged (UrlChanged >> dispatch)
        router.children [
            Html.section [
                theme.winter
                prop.className "grid grid-rows-3 gap-5"
                prop.children [
                    logo
                    navigation model dispatch
                    match model.Page with
                    | Home homeModel ->
                        Home.view homeModel dispatch
                    | Login loginModel ->
                        Login.view loginModel dispatch
                    | NotFound -> Html.div [ prop.text "Not Found" ]
                ]
            ]
        ]
    ]
