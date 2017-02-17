module Client.WishList

open Fable.Core
open Fable.Import
open Elmish
open Fable.Helpers.React
open Fable.Helpers.React.Props
open ServerCode.Domain
open Style
open Messages
open System
open Fable.Core.JsInterop
open Fable.PowerPack
open Fable.PowerPack.Fetch.Fetch_types

type Model = { 
    WishList : WishList
    ErrorMsg : string }

let getWishList token =
    promise {        
        let url = "api/wishlist/"
        let props = 
            [ RequestProperties.Headers [
                HttpRequestHeaders.Authorization ("Bearer " + token) ]]

        return! Fable.PowerPack.Fetch.fetchAs<WishList> url props
    }

let loadWishListCmd token = 
    Cmd.ofPromise getWishList token FetchedWishList FetchError

let init (user:UserData) = 
    { WishList = WishList.empty user.UserName
      ErrorMsg = "" }, loadWishListCmd user.Token

let update (msg:WishListMsg) model : Model*Cmd<WishListMsg> = 
    match msg with
    | WishListMsg.LoadForUser user ->
        model, []
    | FetchedWishList wishList -> 
        { model with WishList = wishList }, Cmd.none
    | FetchError _ -> 
        model, Cmd.none

let view (model:Model) (dispatch: AppMsg -> unit) = 
    div [] [
        h4 [] [text (sprintf "Wishlist")]
        table [ClassName "table table-striped table-hover"] [
            thead [] [
                    tr [] [
                        th [] [text "Title"]
                        th [] [text "Authors"]
                ]
            ]                
            tbody[] [
                for book in model.WishList.Books do
                    yield 
                      tr [] [
                        td [] [ a [ Href book.Link; Target "_blank"] [text book.Title ] ]
                        td [] [ text (String.Join(", ",book.Authors)) ]
                        ]
            ]
        ]
    ]