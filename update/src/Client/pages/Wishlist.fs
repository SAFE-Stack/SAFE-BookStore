module Page.Wishlist

open Elmish
open Fable.Core
open Feliz.DaisyUI
open Shared
open SAFE

type Model = { Wishlist: WishList }

type Msg =
    | GotWishlist of WishList
    | RemoveBook of string
    | RemovedBook of string
    | UnhandledError of exn

let init (booksApi: IBooksApi) (userName: UserName) =
    let model = { Wishlist = { UserName = userName; Books = List.empty } }
    let cmd = Cmd.OfAsync.perform booksApi.getWishlist userName GotWishlist
    model, cmd

let update booksApi msg model =
    match msg with
    | GotWishlist wishlist ->
        { Wishlist = wishlist }, Cmd.none
    | RemoveBook title ->
        let userName = model.Wishlist.UserName
        let cmd = Cmd.OfAsync.either booksApi.removeBook (userName, title) RemovedBook UnhandledError
        model, cmd
    | RemovedBook title ->
        let model = { Wishlist = { UserName = model.Wishlist.UserName; Books = model.Wishlist.Books |> List.filter (fun book -> book.Title <> title) } }
        model, Cmd.none
    | UnhandledError exn ->
        model, exn.AsAlert()

open Feliz

let bookRow book dispatch =
    let link = Daisy.link [ link.hover; link.primary; prop.target "_blank"; prop.text book.Title; prop.href book.Link ]
    let image = Html.img [ prop.src book.ImageLink ]
    let remove = Daisy.link [ prop.text "Remove"; prop.onClick (fun _ -> book.Title |> RemoveBook |> dispatch) ]
    Html.tr [ prop.key book.Title; prop.className "hover:bg-teal-300"; prop.children [ Html.td link; Html.td book.Authors; Html.td image; Html.td remove ] ]

let view model dispatch =
    Html.div [
        prop.className "overflow-y-auto"
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
        ]
    ]