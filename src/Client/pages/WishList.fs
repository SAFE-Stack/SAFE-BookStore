module Client.WishList

open Fable.React
open Fable.React.Props
open Fable.Core.JsInterop

open Elmish
open Fetch.Types
open ServerCode
open ServerCode.Domain
open Client.Styles
open Client.Utils
open System
#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

type Model = {
    // Domain data
    WishList : WishList
    // Subcomponent model
    NewBookModel : NewBook.Model
    // Additional view data
    Token : string
    ResetTime : DateTime option
    ErrorMsg : string option
}

/// The different messages processed when interacting with the wish list
type Msg =
    | FetchedWishList of WishList
    | FetchedResetTime of DateTime
    | RemoveBook of Book
    | NewBookMsg of NewBook.Msg
    | FetchError of exn

/// Get the wish list from the server, used to populate the model
let getWishList userName =
    promise {
        let url = sprintf "/api/wishlist/%s" userName
        let props = [ ]

        let! res = Fetch.fetch url props
        let! txt = res.text()
        return Decode.Auto.unsafeFromString<WishList> txt
    }

let getResetTime () =
    promise {
        let url = "/api/resetTime/"
        let props = [ ]

        let! res = Fetch.fetch url props
        let! txt = res.text()
        let details = Decode.Auto.unsafeFromString<Domain.WishListResetDetails> txt
        return details.Time
    }


let postWishList (token,wishList:WishList) =
    promise {
        let url = "/api/wishlist/"
        let body = Encode.Auto.toString(0, wishList)
        let props =
            [ Method HttpMethod.POST
              Fetch.requestHeaders [
                Authorization ("Bearer " + token)
                ContentType "application/json" ]
              Body !^body ]

        let! res = Fetch.fetch url props
        let! txt = res.text()
        return Decode.Auto.unsafeFromString<WishList> txt
    }


let init (userName:string) (token:string) =
    let submodel,cmd = NewBook.init()
    { WishList = WishList.New userName
      Token = token
      NewBookModel = submodel
      ResetTime = None
      ErrorMsg = None },
        Cmd.batch [
            Cmd.map NewBookMsg cmd
            Cmd.OfPromise.either getWishList userName FetchedWishList FetchError
            Cmd.OfPromise.either getResetTime () FetchedResetTime FetchError ]

let update (msg:Msg) model : Model * Cmd<Msg> =
    match msg with
    | FetchedWishList wishList ->
        let wishList = { wishList with Books = wishList.Books |> List.sortBy (fun b -> b.Title) }
        { model with WishList = wishList }, Cmd.none

    | FetchedResetTime datetime ->
        { model with ResetTime = Some datetime }, Cmd.none

    | RemoveBook book ->
        let wishList = { model.WishList with Books = model.WishList.Books |> List.filter ((<>) book) }
        { model with
            WishList = wishList }, Cmd.OfPromise.either postWishList (model.Token,wishList) FetchedWishList FetchError

    | NewBookMsg msg ->
        match msg with
        | NewBook.Msg.AddBook ->
            match model.WishList.VerifyNewBookIsNotADuplicate model.NewBookModel.NewBook with
            | Some err ->
                { model with ErrorMsg = Some err }, Cmd.none
            | None ->
                let wishList =
                    { model.WishList
                        with
                            Books =
                                (model.NewBookModel.NewBook :: model.WishList.Books)
                                |> List.sortBy (fun b -> b.Title) }

                let submodel,cmd = NewBook.init()
                { model with WishList = wishList; NewBookModel = submodel; ErrorMsg = None },
                    Cmd.batch [
                        Cmd.map NewBookMsg cmd
                        Cmd.OfPromise.either postWishList (model.Token,wishList) FetchedWishList FetchError
                    ]
        | _ ->
            let newSubModel,cmd = NewBook.update msg model.NewBookModel
            { model with NewBookModel = newSubModel}, Cmd.map NewBookMsg cmd
    | FetchError e ->
        { model with ErrorMsg = Some e.Message }, Cmd.none


type BookProps = { key: string; book: Book; removeBook: unit -> unit }

let bookComponent { book = book; removeBook = removeBook } =
    tr [ Key book.Link ] [
        td [] [
            if String.IsNullOrWhiteSpace book.Link then
                yield str book.Title
            else
                yield a [ Href book.Link; Target "_blank" ] [str book.Title ] ]
        td [] [ str book.Authors ]
        td [] [ img [ Src book.ImageLink; Title book.Title ]]
        td [] [ buttonLink "" removeBook [ str "Remove" ] ]
    ]

let BookComponent = elmishView "Book" bookComponent

type BooksProps = {
    WishList: WishList
    Dispatch: Msg -> unit
}

let booksView = elmishView "Books" <| fun { WishList = wishList; Dispatch = dispatch } ->
    table [ClassName "table table-striped table-hover"] [
        thead [] [
            tr [] [
                th [] [str "Title"]
                th [] [str "Authors"]
                th [] [str "Image"]
                th [] []
            ]
        ]
        tbody [] [
            wishList.Books
                |> List.map (fun book ->
                   BookComponent {
                        key = book.Title + book.Authors
                        book = book
                        removeBook = (fun _ -> dispatch (RemoveBook book))
                })
                |> ofList
        ]
    ]

type WishListProps = {
    Model: Model
    Dispatch: Msg -> unit
}

let view = elmishView "WishList" (fun { Model = model; Dispatch = dispatch } ->
    let time = model.ResetTime |> Option.map (fun t -> " - Last database reset at " + t.ToString("yyyy-MM-dd HH:mm") + "UTC") |> Option.defaultValue ""
    div [ Key "WishList" ] [
        h4 [] [ str "Wishlist for " ; str model.WishList.UserName; str time ]
        booksView { WishList = model.WishList; Dispatch = dispatch }
        NewBook.view { Model = model.NewBookModel; Dispatch = (dispatch << NewBookMsg) }
        errorBox model.ErrorMsg
    ]
)