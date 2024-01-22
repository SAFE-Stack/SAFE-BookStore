module Page.Login

open Elmish

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
    Html.div [ prop.text "Login" ]

