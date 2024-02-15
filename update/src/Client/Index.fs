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
    | OnSessionChange
    | Logout

let wishListApi token =
    let bearer = $"Bearer {token}"
    Remoting.createApi ()
    |> Remoting.withAuthorizationHeader bearer
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IWishListApi>

let guestApi =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IGuestApi>

let initFromUrl model url =
    match url with
    | [] ->
        let homeModel, homeMsg = Home.init guestApi

        let model = {
            Page = Home homeModel
            User = model.User
        }

        let cmd = homeMsg |> Cmd.map HomePageMsg
        model, cmd
    | [ "login" ] ->
        let loginModel, loginMsg = Login.init ()

        let model = {
            Page = Login loginModel
            User = model.User
        }

        let cmd = loginMsg |> Cmd.map LoginPageMsg
        model, cmd
    | [ "wishlist" ] ->
        match model.User with
        | User user ->
            let wishlistModel, wishlistMsg = Wishlist.init (wishListApi user.Token) user.UserName

            let model = {
                Page = Wishlist wishlistModel
                User = model.User
            }

            let cmd = wishlistMsg |> Cmd.map WishlistMsg
            model, cmd
        | Guest -> model, Cmd.navigate "login"
    | _ -> { Page = NotFound; User = model.User }, Cmd.none

let init () =
    let model, _ = Home.init guestApi
    let user = Session.loadUser () |> Option.map User |> Option.defaultValue Guest

    Router.currentUrl () |> initFromUrl { Page = Home model; User = user }

let update msg model =
    match model.Page, msg with
    | Home homeModel, HomePageMsg homeMsg ->
        let newModel, cmd = Home.update homeMsg homeModel

        {
            Page = Home newModel
            User = model.User
        },
        cmd
    | Login loginModel, LoginPageMsg loginMsg ->
        let user =
            match loginMsg with
            | Login.LoggedIn user -> User user
            | _ -> model.User

        let newModel, cmd = Login.update guestApi loginMsg loginModel
        { Page = Login newModel; User = user }, cmd |> Cmd.map LoginPageMsg
    | Wishlist wishlistModel, WishlistMsg wishlistMsg ->
        let token =
            match model.User with
            | User data -> data.Token
            | Guest -> ""
        let newModel, cmd = Wishlist.update (wishListApi token) wishlistMsg wishlistModel

        {
            Page = Wishlist newModel
            User = model.User
        },
        cmd |> Cmd.map WishlistMsg
    | NotFound, _ -> { Page = NotFound; User = model.User }, Cmd.none
    | _, UrlChanged url -> initFromUrl model url
    | _, Logout ->
        Session.deleteUser ()
        { model with User = Guest }, Cmd.navigate ""
    | _, OnSessionChange ->
        let session = Session.loadUser ()
        let user = session |> Option.map User |> Option.defaultValue Guest
        let cmd = session |> Option.map (fun _ -> Cmd.none) |> Option.defaultValue (Cmd.navigate "login")
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
                            | Home homeModel -> Home.view homeModel (HomePageMsg >> dispatch)
                            | Login loginModel -> Login.view loginModel (LoginPageMsg >> dispatch)
                            | Wishlist wishlistModel -> Wishlist.view wishlistModel (WishlistMsg >> dispatch)
                            | NotFound -> Html.div [ prop.text "Not Found" ]
                        ]
                    ]
                ]
            ]
        ]
    ]

let resetStorage onResetStorageMsg =
    let register dispatch =
        let callback _ =
            dispatch onResetStorageMsg
        window.addEventListener("storage", callback)

        { new IDisposable with
            member _.Dispose() = window.removeEventListener("storage", callback) }
    register

let subscribe _ =
    [ ["resetStorage"], resetStorage OnSessionChange ]