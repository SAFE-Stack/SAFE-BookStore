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
    Token : string
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

let postWishList (token,wishList) =
    promise {        
        let url = "api/wishlist/"
        let body = toJson wishList
        let props = 
            [ RequestProperties.Method HttpMethod.POST
              RequestProperties.Headers [
                HttpRequestHeaders.Authorization ("Bearer " + token)
                HttpRequestHeaders.ContentType "application/json" ]
              RequestProperties.Body (unbox body) ]

        return! Fable.PowerPack.Fetch.fetchAs<WishList> url props
    }

let postWishListCmd (token,wishList) = 
    Cmd.ofPromise postWishList (token,wishList) FetchedWishList FetchError

let init (user:UserData) = 
    { WishList = WishList.empty user.UserName
      Token = user.Token
      ErrorMsg = "" }, loadWishListCmd user.Token

let update (msg:WishListMsg) model : Model*Cmd<WishListMsg> = 
    match msg with
    | WishListMsg.LoadForUser user ->
        model, []
    | FetchedWishList wishList ->
        { model with WishList = wishList }, Cmd.none
    | UpdateWishListOnServer -> 
        model, postWishListCmd(model.Token,model.WishList)
    | RemoveBook book -> 
        let books = model.WishList.Books |> List.filter ((<>) book)
        { model with WishList = { model.WishList with Books = books } }, Cmd.ofMsg UpdateWishListOnServer
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
                        td [] [ buttonLink "" (fun _ -> dispatch (WishListMsg (RemoveBook book))) [ text "Remove" ] ]
                        ]
            ]
        ]
    ]