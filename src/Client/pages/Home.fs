module Client.Home

open Fable.React
open Fable.React.Props
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

let view = elmishView "Home" (fun (model:Model) ->
    match model.WishList with
    | Some wishList ->
        table [Key "Books"; ClassName "table table-striped table-hover"] [
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
    | _ ->
        div [] []
)