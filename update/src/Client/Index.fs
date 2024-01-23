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
    | Wishlist of Wishlist.Model
    | NotFound

type User =
    | Guest
    | User of UserData

type Model = { Page: PageTab; User: User }

type Msg =
    | HomePageMsg of Home.Msg
    | LoginPageMsg of Login.Msg
    | WishlistMsg of Wishlist.Msg
    | UrlChanged of string list
    | Logout

let booksApi =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IBooksApi>

let userApi =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IUserApi>

let initFromUrl model url =
    match url with
    | [] ->
        let homeModel, homeMsg = Home.init booksApi
        let model = { Page = Home homeModel; User = model.User }
        let cmd = homeMsg |> Cmd.map HomePageMsg
        model, cmd
    | [ "login" ] ->
        let loginModel, loginMsg = Login.init ()
        let model = { Page = Login loginModel; User = model.User }
        let cmd = loginMsg |> Cmd.map LoginPageMsg
        model, cmd
    | [ "wishlist" ] ->
        let wishlistModel, wishlistMsg = Wishlist.init booksApi
        let model = { Page = Wishlist wishlistModel; User = model.User }
        let cmd = wishlistMsg |> Cmd.map WishlistMsg
        model, cmd
    | _ -> { Page = NotFound; User = model.User }, Cmd.none

let init () =
    let model, _ = Home.init booksApi
    Router.currentUrl ()
    |> initFromUrl { Page = Home model; User = Guest }

let update msg model =
    match model.Page, msg with
    | Home homeModel, HomePageMsg homeMsg ->
        let newModel, cmd = Home.update homeMsg homeModel
        { Page = Home newModel; User = model.User }, cmd
    | Login loginModel, LoginPageMsg loginMsg ->
        let user =
            match loginMsg with
            | Login.LoggedIn user -> User user
            | _ -> model.User
        let newModel, cmd = Login.update userApi loginMsg loginModel
        { Page = Login newModel; User = user }, cmd |> Cmd.map LoginPageMsg
    | Wishlist wishlistModel, WishlistMsg wishlistMsg ->
        let newModel, cmd = Wishlist.update wishlistMsg wishlistModel
        { Page = Wishlist newModel; User = model.User }, cmd
    | NotFound, _ ->
        { Page = NotFound; User = model.User }, Cmd.none
    | _, UrlChanged url -> initFromUrl model url
    | _, Logout -> { model with User = Guest }, Cmd.ofMsg (UrlChanged [])
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
                    match model.User with
                    | Guest ->
                        Daisy.tab [ prop.text "Login"; prop.onClick (fun _ -> Router.navigate "login")]
                    | User user ->
                        Daisy.tab [ prop.text "Wishlist"; prop.onClick (fun _ -> Router.navigate "wishlist")]
                        Daisy.tab [ prop.text "Logout"; prop.onClick (fun _ -> dispatch Logout)]
                        Daisy.tab [ prop.text $"Logged in as {user.UserName}" ]
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
                                Home.view homeModel (HomePageMsg >> dispatch)
                            | Login loginModel ->
                                Login.view loginModel (LoginPageMsg >> dispatch)
                            | Wishlist wishlistModel ->
                                Wishlist.view wishlistModel (WishlistMsg >> dispatch)
                            | NotFound -> Html.div [ prop.text "Not Found" ]
                        ]
                    ]
                ]
            ]
        ]
    ]
