module Index

open System
open Browser
open Elmish
open Fable.Remoting.Client
open Feliz.DaisyUI
open Feliz.Router
open Page
open Shared

type PageTab =
    | Home
    | Login
    | Wishlist of UserData
    | NotFound

type User =
    | Guest
    | User of UserData

type Model = { Page: PageTab; User: User }

type Msg =
    | WishlistMsg of WishList.Msg
    | UrlChanged of string list
    | OnSessionChange
    | Logout


let guestApi =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IGuestApi>

let initFromUrl model url =
    match url with
    | [] ->
        let model = { Page = Home; User = model.User }
        model, Cmd.none
    | [ "login" ] ->
        let model = { Page = Login; User = model.User }
        model, Cmd.none
    | [ "wishlist" ] ->
        match model.User with
        | User user ->
            let model = {
                Page = Wishlist user
                User = model.User
            }

            model, Cmd.none
        | Guest -> model, Cmd.navigate "login"
    | _ -> { Page = NotFound; User = model.User }, Cmd.none

let init () =
    let user = Session.loadUser () |> Option.map User |> Option.defaultValue Guest

    Router.currentUrl () |> initFromUrl { Page = Home; User = user }

let update msg model =
    match model.Page, msg with
    | NotFound, _ -> { Page = NotFound; User = model.User }, Cmd.none
    | _, UrlChanged url -> initFromUrl model url
    | _, Logout ->
        Session.deleteUser ()
        { model with User = Guest }, Cmd.navigate ""
    | _, OnSessionChange ->
        let session = Session.loadUser ()
        let user = session |> Option.map User |> Option.defaultValue Guest

        let cmd =
            session
            |> Option.map (fun _ -> Cmd.none)
            |> Option.defaultValue (Cmd.navigate "login")

        { model with User = user }, cmd
    | _, _ -> model, Cmd.none

open Feliz

let logo =
    Html.a [
        prop.href "https://safe-stack.github.io/"
        prop.className "ml-12 h-12 w-12 bg-primary hover:cursor-pointer hover:bg-teal-400"
        prop.children [ Html.img [ prop.src "/favicon.png"; prop.alt "Logo" ] ]
    ]

let navigation model dispatch =
    Html.div [
        prop.className "grid"
        prop.children [
            Daisy.tabs [
                prop.className "justify-self-center"
                prop.children [
                    Daisy.tab [ prop.text "Home"; prop.onClick (fun _ -> Router.navigate "") ]
                    match model.User with
                    | Guest -> Daisy.tab [ prop.text "Login"; prop.onClick (fun _ -> Router.navigate "login") ]
                    | User user ->
                        Daisy.tab [ prop.text "Wishlist"; prop.onClick (fun _ -> Router.navigate "wishlist") ]
                        Daisy.tab [ prop.text "Logout"; prop.onClick (fun _ -> dispatch Logout) ]
                        Daisy.tab [ prop.text $"Logged in as {user.UserName.Value}" ]
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
                prop.className "grid grid-rows-index gap-5 h-screen"
                prop.children [
                    logo
                    navigation model dispatch
                    Daisy.divider ""
                    Html.div [
                        prop.className "overflow-y-auto"
                        prop.children [
                            match model.Page with
                            | Home -> Home.View guestApi
                            | Login -> Login.View guestApi
                            | Wishlist user -> WishList.View user
                            | NotFound -> Html.div [ prop.text "Not Found" ]
                        ]
                    ]
                ]
            ]
        ]
    ]

let resetStorage onResetStorageMsg =
    let register dispatch =
        let callback _ = dispatch onResetStorageMsg
        window.addEventListener ("storage", callback)

        { new IDisposable with
            member _.Dispose() =
                window.removeEventListener ("storage", callback)
        }

    register

let subscribe _ = [ [ "resetStorage" ], resetStorage OnSessionChange ]