module Page.Wishlist

open System
open Elmish
open Elmish.SweetAlert
open Feliz.DaisyUI
open Shared
open SAFE

type Model = {
    Wishlist: WishList
    NewBook: NewBook.Model option
}

type Msg =
    | GotWishlist of WishList
    | RemoveBook of string
    | RemovedBook of string
    | NewBookMsg of NewBook.Msg
    | AddedBook of Book
    | OpenNewBookModal
    | UnhandledError of exn

let alert message alertType =
    SimpleAlert(message).Type(alertType) |> SweetAlert.Run

let init (booksApi: IBooksApi) (userName: UserName) =
    let model = {
        Wishlist = {
            UserName = userName
            Books = List.empty
        }
        NewBook = None
    }

    let cmd = Cmd.OfAsync.perform booksApi.getWishlist userName GotWishlist

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
        match newBookMsg, model.NewBook with
        | NewBook.AddBook book, _ ->
            match model.Wishlist.VerifyNewBookIsNotADuplicate book with
            | Ok _ ->
                let userName = model.Wishlist.UserName
                model, Cmd.OfAsync.either booksApi.addBook (userName, book) AddedBook UnhandledError
            | Error error -> model, Exception(error) |> UnhandledError |> Cmd.ofMsg
        | NewBook.Cancel, _ ->
            {model with NewBook = None}, Cmd.none
        | _, Some newBook ->
            let newBookModel, cmd = NewBook.update newBookMsg newBook
            let model = { model with NewBook = Some newBookModel }
            model, cmd |> Cmd.map NewBookMsg
        | _, _ ->
            model, Cmd.none
    | AddedBook book ->
        let wishList = {
            model.Wishlist with
                Books = book :: model.Wishlist.Books |> List.sortBy (fun book -> book.Title)
            
        }

        {
            Wishlist = wishList
            NewBook = None
        },
        alert $"{book.Title} added" AlertType.Success

    | OpenNewBookModal ->
        let newBook, newBookmsg = NewBook.init()
        {model with NewBook = Some newBook}, (newBookmsg |> Cmd.map NewBookMsg)
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


let newBookButton dispatch =
    Daisy.button.label [
        button.primary
        prop.text "Add"
        prop.onClick ( fun _ -> OpenNewBookModal |> dispatch)

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
            newBookButton dispatch
            match model.NewBook with
            | Some book ->
                NewBook.view book (NewBookMsg >> dispatch)
            | None -> ()
        ]
    ]