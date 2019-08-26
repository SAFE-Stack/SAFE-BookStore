module Client.Home

open Fable.React
open Fable.React.Props
open Client.Styles
open Client.Pages
open System
open Client.Utils
open ServerCode.Domain

type Model = {
    Version : string
    WishList : WishList option
}


let empty = {
    Version = ReleaseNotes.Version
    WishList = None
}


type BookProps = { key: string; book: Book }

let bookComponent { book = book } =
    tr [ Key book.Link ] [
        td [] [
            if String.IsNullOrWhiteSpace book.Link then
                yield str book.Title
            else
                yield a [ Href book.Link; Target "_blank" ] [str book.Title ] ]
        td [] [ str book.Authors ]
        td [] [ img [ Src book.ImageLink; Title book.Title ]]
    ]

let BookComponent = elmishView "Book" bookComponent

type BooksProps = {
    WishList: WishList option
}

let booksView = elmishView "Books" (fun (props:BooksProps) ->
    match props.WishList with
    | Some wishList ->
        table [ClassName "table table-striped table-hover"] [
            thead [] [
                tr [] [
                    th [] [str "Title"]
                    th [] [str "Authors"]
                    th [] [str "Image"]
                ]
            ]
            tbody [] [
                wishList.Books
                    |> List.map (fun book ->
                       BookComponent {
                            key = book.Title + book.Authors
                            book = book
                       })
                    |> ofList
            ]
        ]
    | _ -> div [] []
)

let view = elmishView "Home" (fun (model:Model) ->
    div [Key "Home"; centerStyle "column"] [
        viewLink Page.Login "Please login into the SAFE-Stack sample app"
        br []
        br []
        br []
        br []
        words 20 "Made with"
        br []
        a [ Href "https://safe-stack.github.io/" ] [ img [ Src "/Images/safe_logo.png" ] ]
        br []
        br []
        words 15 "An end-to-end, functional-first stack for cloud-ready web development that emphasises type-safe programming."
        br []
        br []

        booksView { WishList = model.WishList }

        br []
        br []
        words 20 ("version " + model.Version)
    ]
)