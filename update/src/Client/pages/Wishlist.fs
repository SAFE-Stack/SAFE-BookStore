module Page.Wishlist

open System
open Elmish
open Elmish.SweetAlert
open Feliz.DaisyUI
open Shared
open SAFE

type Model = {
    Wishlist: WishList
    NewBook: NewBook.Model
}

type Msg =
    | GotWishlist of WishList
    | RemoveBook of string
    | RemovedBook of string
    | NewBookMsg of NewBook.Msg
    | AddedBook of Book
    | UnhandledError of exn

let alert message alertType =
    SimpleAlert(message).Type(alertType) |> SweetAlert.Run

let init (booksApi: IBooksApi) (userName: UserName) =
    let newBookModel, newBookCmd = NewBook.init ()

    let model = {
        Wishlist = {
            UserName = userName
            Books = List.empty
        }
        NewBook = newBookModel
    }

    let cmd =
        Cmd.batch [
            Cmd.OfAsync.perform booksApi.getWishlist userName GotWishlist
            newBookCmd |> Cmd.map NewBookMsg
        ]

    model, cmd

let update booksApi msg model =
    match msg with
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
        match newBookMsg with
        | NewBook.AddBook book ->
            match model.Wishlist.VerifyNewBookIsNotADuplicate book with
            | Ok _ ->
                let userName = model.Wishlist.UserName
                model, Cmd.OfAsync.either booksApi.addBook (userName, book) AddedBook UnhandledError
            | Error error -> model, Exception(error) |> UnhandledError |> Cmd.ofMsg
        | _ ->
            let newBookModel, cmd = NewBook.update newBookMsg model.NewBook
            let model = { model with NewBook = newBookModel }
            model, cmd |> Cmd.map NewBookMsg
    | AddedBook book ->
        let wishList = {
            model.Wishlist with
                Books = book :: model.Wishlist.Books |> List.sortBy (fun book -> book.Title)
        }

        let newBookModel, _ = NewBook.init ()

        {
            Wishlist = wishList
            NewBook = newBookModel
        },
        alert $"{book.Title} added" AlertType.Success
    | UnhandledError exn -> model, exn.AsAlert()

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

    Html.tr [
        prop.key book.Title
        prop.className "hover:bg-primary"
        prop.children [ Html.td titleLink; Html.td book.Authors; Html.td image; Html.td remove ]
    ]

let view model dispatch =
    Html.div [
        prop.className ""
        prop.children [
            Daisy.table [
                prop.children [
                    Html.tbody [
                        for book in model.Wishlist.Books do
                            bookRow book dispatch
                    ]
                    Html.thead [ Html.tr [ Html.th "Title"; Html.th "Authors"; Html.th "Image" ] ]
                ]
            ]
            Daisy.divider ""
            NewBook.view model.NewBook (NewBookMsg >> dispatch)
        ]
    ]