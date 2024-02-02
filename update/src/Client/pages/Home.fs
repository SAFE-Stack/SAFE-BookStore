module Page.Home

open Elmish
open Feliz.DaisyUI
open Shared

type Model = { Wishlist: Book seq }

type Msg = GotWishlist of Book seq

let init (booksApi: IBooksApi) =
    let model = { Wishlist = Seq.empty }
    let cmd = Cmd.OfAsync.perform booksApi.getBooks () GotWishlist
    model, cmd

let update msg model =
    match msg with
    | GotWishlist books -> { Wishlist = books }, Cmd.none

open Feliz

let bookRow book =
    let link =
        Daisy.link [
            link.hover
            link.primary
            prop.target "_blank"
            prop.text book.Title
            prop.href book.Link
        ]

    let image = Html.img [ prop.src book.ImageLink ]

    let tableCell (key: string) (element: ReactElement) =
        Html.td [ prop.key key; prop.children element ]

    Html.tr [
        prop.className "hover:primary"
        prop.children [
            tableCell "title" link
            tableCell "authors" (Html.text book.Authors)
            tableCell "image" image
        ]
    ]

let view model dispatch =
    Html.div [
        prop.className "overflow-y-auto"
        prop.children [
            Daisy.table [
                prop.children [
                    Html.tbody [
                        for book in model.Wishlist do
                            bookRow book
                    ]
                    Html.thead [ Html.tr [ Html.th "Title"; Html.th "Authors"; Html.th "Image" ] ]
                ]
            ]
        ]
    ]