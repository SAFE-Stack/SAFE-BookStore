module Client.WishList

open Fable.PowerPack
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core.JsInterop

open Elmish
open Fetch.Fetch_types
open ServerCode
open ServerCode.Domain
open Client.Styles
open System
#if FABLE_COMPILER
open Thoth.Json
#else
open Thoth.Json.Net
#endif

type Model =
  { WishList : WishList
    Token : string
    NewBookModel : NewBook.Model
    ResetTime : DateTime option
    ErrorMsg : string option }

/// The different messages processed when interacting with the wish list
type Msg =
    | LoadForUser of string
    | FetchedWishList of WishList
    | FetchedResetTime of DateTime
    | RemoveBook of Book
    | NewBookMsg of NewBook.Msg
    | FetchError of exn

/// Get the wish list from the server, used to populate the model
let getWishList token =
    promise {
        let url = ServerUrls.APIUrls.WishList
        let props =
            [ Fetch.requestHeaders [
                HttpRequestHeaders.Authorization ("Bearer " + token) ]]

        let! res = Fetch.fetch url props
        let! txt = res.text()
        return Decode.Auto.unsafeFromString<WishList> txt
    }

let getResetTime token =
    promise {
        let url = ServerUrls.APIUrls.ResetTime
        let props =
            [ Fetch.requestHeaders [
                HttpRequestHeaders.Authorization ("Bearer " + token) ]]

        let! res = Fetch.fetch url props
        let! txt = res.text()
        let details = Decode.Auto.unsafeFromString<ServerCode.Domain.WishListResetDetails> txt
        return details.Time
    }


let postWishList (token,wishList) =
    promise {
        let url = ServerUrls.APIUrls.WishList
        let body = Encode.Auto.toString(0, wishList)
        let props =
            [ RequestProperties.Method HttpMethod.POST
              Fetch.requestHeaders [
                HttpRequestHeaders.Authorization ("Bearer " + token)
                HttpRequestHeaders.ContentType "application/json" ]
              RequestProperties.Body !^body ]

        let! res = Fetch.fetch url props
        let! txt = res.text()
        return Decode.Auto.unsafeFromString<WishList> txt
    }

let postWishListCmd (token,wishList) =
    Cmd.ofPromise postWishList (token,wishList) FetchedWishList FetchError


let init (user:UserData) =
    let submodel,cmd = NewBook.init()
    { WishList = WishList.New user.UserName
      Token = user.Token
      NewBookModel = submodel
      ResetTime = None
      ErrorMsg = None },
        Cmd.batch [
            Cmd.map NewBookMsg cmd
            Cmd.ofPromise getWishList user.Token FetchedWishList FetchError
            Cmd.ofPromise getResetTime user.Token FetchedResetTime FetchError ]

let update (msg:Msg) model : Model*Cmd<Msg> =
    match msg with
    | LoadForUser user ->
        model, Cmd.none

    | FetchedWishList wishList ->
        let wishList = { wishList with Books = wishList.Books |> List.sortBy (fun b -> b.Title) }
        { model with WishList = wishList }, Cmd.none

    | FetchedResetTime datetime ->
        { model with ResetTime = Some datetime }, Cmd.none

    | RemoveBook book ->
        let wishList = { model.WishList with Books = model.WishList.Books |> List.filter ((<>) book) }
        { model with
            WishList = wishList
            ErrorMsg = Validation.verifyBookisNotADuplicate wishList model.NewBookModel.NewBook },
                postWishListCmd(model.Token,wishList)

    | NewBookMsg msg ->
        match msg with
        | NewBook.Msg.AddBook ->
            if Validation.verifyBook model.NewBookModel.NewBook then
                match Validation.verifyBookisNotADuplicate model.WishList model.NewBookModel.NewBook with
                | Some err ->
                    { model with ErrorMsg = Some err }, Cmd.none
                | None ->
                    let wishList = { model.WishList with Books = (model.NewBookModel.NewBook :: model.WishList.Books) |> List.sortBy (fun b -> b.Title) }
                    let submodel,cmd = NewBook.init()
                    { model with WishList = wishList; NewBookModel = submodel; ErrorMsg = None },
                        Cmd.batch [
                            Cmd.map NewBookMsg cmd
                            postWishListCmd(model.Token,wishList)
                        ]
            else
                { model with
                    ErrorMsg = Validation.verifyBookisNotADuplicate model.WishList model.NewBookModel.NewBook  }, Cmd.none
        | _ ->
            let submodel,cmd = NewBook.update msg model.NewBookModel
            { model with NewBookModel = submodel}, Cmd.map NewBookMsg cmd
    | FetchError e ->
        { model with ErrorMsg = Some e.Message }, Cmd.none


type BookProps = { key: string; book: Book; removeBook: unit -> unit }

let bookComponent { book = book; removeBook = removeBook } =
  tr [] [
    td [] [
        if String.IsNullOrWhiteSpace book.Link then
            yield str book.Title
        else
            yield a [ Href book.Link; Target "_blank"] [str book.Title ] ]
    td [] [ str book.Authors ]
    td [] [ buttonLink "" removeBook [ str "Remove" ] ]
    ]

let inline BookComponent props = (ofFunction bookComponent) props []

let view (model:Model) (dispatch: Msg -> unit) =
    let time = model.ResetTime |> Option.map (fun t -> " - Last database reset at " + t.ToString("yyyy-MM-dd HH:mm") + "UTC") |> Option.defaultValue ""
    div [] [
        h4 [] [ str (sprintf "Wishlist for %s%s" model.WishList.UserName time) ]
        table [ClassName "table table-striped table-hover"] [
            thead [] [
                    tr [] [
                        th [] [str "Title"]
                        th [] [str "Authors"]
                ]
            ]
            tbody [] [
                model.WishList.Books
                    |> List.map (fun book ->
                        BookComponent {
                            key = book.Title + book.Authors
                            book = book
                            removeBook = (fun _ -> dispatch (RemoveBook book))
                    })
                    |> ofList
            ]
        ]
        NewBook.view model.NewBookModel (dispatch << NewBookMsg)
    ]
