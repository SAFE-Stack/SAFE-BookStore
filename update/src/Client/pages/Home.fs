module Home

open Utils
open Shared
open Elmish
#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

type Model = {
    WishList : WishList option
}

    with
        static member Empty : Model = {
            WishList = None
        }

type Msg =
| LoadWishList
| WishListLoaded of WishList
| Error of exn



let init () = Model.Empty, Cmd.ofMsg LoadWishList

/// Get the wish list from the server, used to populate the model
let getWishList userName =
    promise {
        let url = sprintf "/api/wishlist/%s" userName
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
        { WishList = Some wishList }, Cmd.none

    | Error e ->
        printfn $"Error: %s{e.Message}"
        model, Cmd.none

type BookProps = { Key: string; Book: Book }

let bookComponent = elmishView "Book" (fun (props: BookProps) ->
    let book = props.Book
    tr [ Key props.Key ] [
        td [] [
            if String.IsNullOrWhiteSpace book.Link then
                yield str book.Title
            else
                yield a [ Href book.Link; Target "_blank" ] [str book.Title ] ]
        td [] [ str book.Authors ]
        td [] [ img [ Src book.ImageLink; Title book.Title ]]
    ]
)

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
                        elmishView "Book" bookComponent {
                            Key = book.Title + book.Authors
                            Book = book
                        })
                    |> ofList
            ]
        ]
    | _ ->
        div [] []
)