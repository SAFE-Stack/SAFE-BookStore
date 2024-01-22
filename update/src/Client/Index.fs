module Index

open Elmish
open Fable.Remoting.Client
open Feliz.DaisyUI
open Shared

type PageTab =
    | Home
    | Login
    | Wishlist

type Model = { Page: PageTab }

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
    Html.section [
        theme.winter
        prop.className "grid grid-rows-3 gap-5"
        prop.children [
            logo
            navigation model dispatch
            Page.Home.view model dispatch
        ]
    ]