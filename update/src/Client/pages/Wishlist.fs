module Page.Wishlist

open Elmish
open Fable.Core
open Feliz.DaisyUI
open Shared

type Model = { Wishlist: Book seq }

type Msg =
    | GotWishlist of Book seq

let init (booksApi: IBooksApi) =
    let model = { Wishlist = Seq.empty }
    let cmd = Cmd.OfAsync.perform booksApi.getWishlist () GotWishlist
    model, cmd

let update msg model =
    match msg with
    | GotWishlist books ->
        { Wishlist = books }, Cmd.none

open Feliz

let bookRow book =
    let link = Daisy.link [ link.hover; link.primary; prop.target "_blank"; prop.text book.Title; prop.href book.Link ]
    let image = Html.img [ prop.src book.ImageLink ]
    Html.tr [ prop.className "hover:bg-accent"; prop.children [ Html.td link; Html.td book.Authors; Html.td image ] ]

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