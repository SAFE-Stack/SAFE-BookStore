module Client.Home

open Fable.React
open Fable.React.Props
open Client.Styles
open Client.Pages
open System
open Client.Utils
open ServerCode.Domain
open Elmish
#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

type Model = {
    Version : string
    WishList : WishList option
}

type Msg =
| LoadWishList
| WishListLoaded of WishList
| Error of exn

let private empty = {
    Version = ReleaseNotes.Version
    WishList = None
}

let init () = empty, Cmd.ofMsg LoadWishList

/// Get the wish list from the server, used to populate the model
let getWishList userName =
    promise {
        let url = ServerCode.ServerUrls.APIUrls.WishList userName
        let props = [ ]

        let! res = Fetch.fetch url props
        let! txt = res.text()
        return Decode.Auto.unsafeFromString<WishList> txt
    }

let update (msg:Msg) model : Model*Cmd<Msg> =
    match msg with
    | LoadWishList ->
        model, Cmd.OfPromise.either getWishList "test" WishListLoaded Error

    | WishListLoaded wishList ->
        { model with WishList = Some wishList }, Cmd.none

    | Error e ->
        printfn "Error: %s" e.Message
        model, Cmd.none


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
        a [ Href "https://safe-stack.github.io/" ] [ img [ Src "/Images/safe_logo.png"; Title "SAFE logo" ] ]
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