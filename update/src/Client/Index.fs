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

let booksApi =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IBooksApi>

let initFromUrl url =
    match url with
    | [] ->
        let homeModel, homeMsg = Home.init booksApi
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
    Html.div [
        prop.className "grid border-b-[1px] border-slate-200 pb-8"
        prop.children [
            Daisy.tabs [
                prop.className "justify-self-center"
                prop.children [
                    Daisy.tab [ prop.text "Home"; prop.onClick (fun _ -> Router.navigate "") ]
                    Daisy.tab [ prop.text "Login"; prop.onClick (fun _ -> Router.navigate "login")]
                ]
            ]

        ]
    ]

let view model dispatch =
    React.router [
        router.onUrlChanged (UrlChanged >> dispatch)
        router.children [
            Html.section [
                theme.winter
                prop.className "grid grid-rows-[min-content_min-content_auto] gap-5 h-screen"
                prop.children [
                    logo
                    navigation model dispatch
                    Html.div [
                        prop.className "pt-8"
                        prop.children [
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
        ]
    ]
