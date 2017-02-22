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

type Model = 
  { WishList : WishList
    Token : string
    NewBook: Book
    TitleErrorText : string option
    AuthorsErrorText : string option
    LinkErrorText : string option
    ErrorMsg : string }

/// Get the wish list from the server, used to populate the model
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
    { WishList = WishList.New user.UserName
      Token = user.Token
      NewBook = Book.empty
      TitleErrorText = None
      AuthorsErrorText = None
      LinkErrorText = None
      ErrorMsg = "" }, loadWishListCmd user.Token


let update (msg:WishListMsg) model : Model*Cmd<WishListMsg> = 
    match msg with
    | WishListMsg.LoadForUser user ->
        model, []
    | FetchedWishList wishList ->
        let wishList = { wishList with Books = wishList.Books |> List.sortBy (fun b -> b.Title) }
        { model with WishList = wishList }, Cmd.none
    | TitleChanged title -> 
        { model with NewBook = { model.NewBook with Title = title }; TitleErrorText = Validation.verifyBookTitle title }, Cmd.none
    | AuthorsChanged authors -> 
        { model with NewBook = { model.NewBook with Authors = authors }; AuthorsErrorText = Validation.verifyBookAuthors authors }, Cmd.none
    | LinkChanged link -> 
        { model with NewBook = { model.NewBook with Link = link }; LinkErrorText = Validation.verifyBookLink link }, Cmd.none
    | RemoveBook book -> 
        let wishList = { model.WishList with Books = model.WishList.Books |> List.filter ((<>) book) }
        { model with WishList = wishList}, postWishListCmd(model.Token,wishList)
    | AddBook ->
        if Validation.verifyBook model.NewBook then
            let wishList = { model.WishList with Books = (model.NewBook :: model.WishList.Books) |> List.sortBy (fun b -> b.Title) }
            { model with WishList = wishList; NewBook = Book.empty }, postWishListCmd(model.Token,wishList)
        else
            { model with 
                TitleErrorText = Validation.verifyBookTitle model.NewBook.Title
                AuthorsErrorText = Validation.verifyBookAuthors model.NewBook.Authors
                LinkErrorText = Validation.verifyBookLink model.NewBook.Link }, Cmd.none
    | FetchError _ -> 
        model, Cmd.none

let newBookForm (model:Model) dispatch =
    let buttonActive = if String.IsNullOrEmpty model.NewBook.Title || String.IsNullOrEmpty model.NewBook.Authors then "btn-disabled" else "btn-primary"
    
    let titleStatus = if String.IsNullOrEmpty model.NewBook.Title then "" else "has-success"

    let authorStatus = if String.IsNullOrEmpty model.NewBook.Authors then "" else "has-success"

    let linkStatus = if String.IsNullOrEmpty model.NewBook.Link then "" else "has-success"

    div [] [
        h4 [] [text "New Book"]

        div [ClassName "container"] [
            div [ClassName "row"] [
                div [ClassName "col-md-8"] [
                    div [ClassName ("form-group has-feedback" + titleStatus)] [
                        yield div [ClassName "input-group"] [
                             yield span [ClassName "input-group-addon"] [span [ClassName "glyphicon glyphicon-pencil"] [] ]
                             yield input [
                                     HTMLAttr.Type "text"
                                     Name "Title"
                                     Value (U2.Case1 model.NewBook.Title)
                                     ClassName "form-control"
                                     Placeholder "Please insert book title"
                                     Required true
                                     OnChange (fun (ev:React.FormEvent) -> dispatch (WishListMsg (WishListMsg.TitleChanged (unbox ev.target?value)))) ] []
                             match model.TitleErrorText with
                             | Some e -> yield span [ClassName "glyphicon glyphicon-remove form-control-feedback"] []
                             | _ -> ()
                        ]
                        match model.TitleErrorText with
                        | Some e -> yield p [ClassName "text-danger"][text e]
                        | _ -> ()
                    ]
                    div [ClassName ("form-group has-feedback" + authorStatus) ] [
                         yield div [ClassName "input-group"][
                             yield span [ClassName "input-group-addon"] [span [ClassName "glyphicon glyphicon-user"] [] ]
                             yield input [ 
                                     HTMLAttr.Type "text"
                                     Name "Author"
                                     Value (U2.Case1 model.NewBook.Authors)
                                     ClassName "form-control"
                                     Placeholder "Please insert authors"
                                     Required true
                                     OnChange (fun (ev:React.FormEvent) -> dispatch (WishListMsg (WishListMsg.AuthorsChanged (unbox ev.target?value))))] []
                             match model.AuthorsErrorText with
                             | Some e -> yield span [ClassName "glyphicon glyphicon-remove form-control-feedback"] []
                             | _ -> ()
                         ]
                         match model.AuthorsErrorText with
                         | Some e -> yield p [ClassName "text-danger"][text e]
                         | _ -> ()
                    ]
                    div [ClassName ("form-group has-feedback" + linkStatus)] [
                         yield div [ClassName "input-group"] [
                             yield span [ClassName "input-group-addon"] [span [ClassName "glyphicon glyphicon glyphicon-pencil"] [] ]
                             yield input [ 
                                    HTMLAttr.Type "text"
                                    Name "Link"
                                    Value (U2.Case1 model.NewBook.Link)
                                    ClassName "form-control"
                                    Placeholder "Please insert link"
                                    Required true
                                    OnChange (fun (ev:React.FormEvent) -> dispatch (WishListMsg (WishListMsg.LinkChanged (unbox ev.target?value))))] []
                             match model.LinkErrorText with
                             | Some e -> yield span [ClassName "glyphicon glyphicon-remove form-control-feedback"] []
                             | _ -> ()
                         ]
                         match model.LinkErrorText with
                         | Some e -> yield p [ClassName "text-danger"][text e]
                         | _ -> ()
                    ]
                    button [ ClassName ("btn " + buttonActive); OnClick (fun _ -> dispatch (WishListMsg WishListMsg.AddBook))] [
                        i [ClassName "glyphicon glyphicon-plus"; Style [PaddingRight 5]] []
                        text "Add"
                    ]  
                ]                    
            ]        
        ]
    ]

let view (model:Model) (dispatch: AppMsg -> unit) = 
    div [] [
        h4 [] [text (sprintf "Wishlist for %s" model.WishList.UserName) ]
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
                        td [] [ 
                            if String.IsNullOrWhiteSpace book.Link then 
                                yield text book.Title
                            else
                                yield a [ Href book.Link; Target "_blank"] [text book.Title ] ]
                        td [] [ text book.Authors ]
                        td [] [ buttonLink "" (fun _ -> dispatch (WishListMsg (RemoveBook book))) [ text "Remove" ] ]
                        ]
            ]
        ]
        newBookForm (model) dispatch
    ]