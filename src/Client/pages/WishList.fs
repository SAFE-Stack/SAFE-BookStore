module Page.WishList

open System
open Elmish
open Elmish.SweetAlert
open Feliz.DaisyUI
open Feliz.UseElmish
open Shared
open SAFE
open Fable.Remoting.Client

let wishListApi token =
    let bearer = $"Bearer {token}"

    Remoting.createApi ()
    |> Remoting.withAuthorizationHeader bearer
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IWishListApi>

type Model = {
    Wishlist: WishList
    LastResetTime: DateTime
    NewBook: NewBook.Model option
}

type Msg =
    | GotLastRestTime of DateTime
    | GotWishlist of WishList
    | RemoveBook of string
    | RemovedBook of string
    | NewBookMsg of NewBook.Msg
    | AddedBook of Book
    | OpenNewBookModal
    | UnhandledError of exn

let alert message alertType =
    SimpleAlert(message).Type(alertType) |> SweetAlert.Run

let init api (user: UserData) =
    let model = {
        Wishlist = {
            UserName = user.UserName
            Books = List.empty
        }
        LastResetTime = DateTime.MinValue
        NewBook = None
    }

    let cmd =
        Cmd.batch [
            Cmd.OfAsync.either api.getWishlist user.UserName GotWishlist UnhandledError
            Cmd.OfAsync.either api.getLastResetTime () GotLastRestTime UnhandledError
        ]

    model, cmd

let update booksApi msg model =
    match msg with
    | GotLastRestTime time -> { model with LastResetTime = time }, Cmd.none
    | GotWishlist wishlist -> { model with Wishlist = wishlist }, Cmd.none
    | RemoveBook title ->
        let userName = model.Wishlist.UserName

        let cmd =
            Cmd.OfAsync.either booksApi.removeBook (userName, title) RemovedBook UnhandledError

        model, cmd
    | RemovedBook title ->
        let model = {
            model with
                Wishlist = {
                    UserName = model.Wishlist.UserName
                    Books = model.Wishlist.Books |> List.filter (fun book -> book.Title <> title)
                }
        }

        model, alert $"{title} removed" AlertType.Info
    | NewBookMsg newBookMsg ->
        match newBookMsg, model.NewBook with
        | NewBook.AddBook book, _ ->
            match model.Wishlist.VerifyNewBookIsNotADuplicate book with
            | Ok _ ->
                let userName = model.Wishlist.UserName
                model, Cmd.OfAsync.either booksApi.addBook (userName, book) AddedBook UnhandledError
            | Error error -> model, Exception(error) |> UnhandledError |> Cmd.ofMsg
        | NewBook.Cancel, _ -> { model with NewBook = None }, Cmd.none
        | _, Some newBook ->
            let newBookModel, cmd = NewBook.update newBookMsg newBook

            let model = {
                model with
                    NewBook = Some newBookModel
            }

            model, cmd |> Cmd.map NewBookMsg
        | _, _ -> model, Cmd.none
    | AddedBook book ->
        let wishList = {
            model.Wishlist with
                Books = book :: model.Wishlist.Books |> List.sortBy _.Title
        }

        {
            model with
                Wishlist = wishList
                NewBook = None
        },
        alert $"{book.Title} added" AlertType.Success

    | OpenNewBookModal ->
        let newBook, newBookMsg = NewBook.init ()
        { model with NewBook = Some newBook }, (newBookMsg |> Cmd.map NewBookMsg)
    | UnhandledError exn ->
        exn.OnStatusRun 401 Session.deleteUser
        model, Cmd.none

open Feliz

let bookRow book dispatch =
    let titleLink =
        Daisy.link [
            link.hover
            link.secondary
            prop.target "_blank"
            prop.text book.Title
            prop.href book.Link
        ]

    let image = Html.img [ prop.src book.ImageLink ]

    let remove =
        Daisy.link [
            link.hover
            link.secondary
            prop.text "Remove"
            prop.onClick (fun _ -> book.Title |> RemoveBook |> dispatch)
        ]

    let tableCell (key: string) (element: ReactElement) =
        Html.td [ prop.key key; prop.children element ]

    Html.tr [
        prop.key book.Title
        prop.className "hover:bg-primary"
        prop.children [
            tableCell "title" titleLink
            tableCell "author" (Html.text book.Authors)
            tableCell "image" image
            tableCell "remove" remove
        ]
    ]

let newBookButton dispatch =
    Daisy.button.label [
        button.primary
        prop.text "Add"
        prop.onClick (fun _ -> OpenNewBookModal |> dispatch)

    ]

let table model dispatch =
    Daisy.table [
        prop.children [
            Html.tbody [
                for book in model.Wishlist.Books do
                    bookRow book dispatch
            ]
            Html.thead [ Html.tr [ Html.th "Title"; Html.th "Authors"; Html.th "Image" ] ]
        ]
    ]

[<ReactComponent>]
let View user =
    let api: IWishListApi = wishListApi user.Token

    let model, dispatch = React.useElmish (init api user, update api, [||])
    let user = model.Wishlist.UserName.Value
    let lastReset = model.LastResetTime.ToString("yyyy-MM-dd HH:mm")

    Html.div [
        prop.className "grid h-full gap-4 content-start"
        prop.children [
            Html.div [
                prop.className "row-min flex justify-end gap-4 mx-4"
                prop.children [ newBookButton dispatch ]
            ]
            Html.div [
                prop.className "overflow-y-auto"
                prop.children [
                    Html.text $"Wishlist for {user} - Last database reset at {lastReset}UTC"
                    table model dispatch
                ]
            ]

            match model.NewBook with
            | Some book -> NewBook.view book (NewBookMsg >> dispatch)
            | None -> ()
        ]
    ]