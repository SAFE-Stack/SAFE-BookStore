module Page.Home

open Elmish
open Feliz.DaisyUI
open Feliz.UseElmish
open Shared

type Model = { Books: Book seq }

type Msg = GotBooks of Book seq

let init (guestApi: IGuestApi) =
    let model = { Books = Seq.empty }
    let cmd = Cmd.OfAsync.perform guestApi.getBooks () GotBooks
    model, cmd

let update msg model =
    match msg with
    | GotBooks books -> { Books = books }, Cmd.none

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

[<ReactComponent>]
let View api =
    let model, dispatch = React.useElmish (init api, update, [||])

    Html.div [
        prop.className "overflow-y-auto"
        prop.children [
            Daisy.table [
                prop.children [
                    Html.tbody [
                        for book in model.Books do
                            bookRow book
                    ]
                    Html.thead [ Html.tr [ Html.th "Title"; Html.th "Authors"; Html.th "Image" ] ]
                ]
            ]
        ]
    ]