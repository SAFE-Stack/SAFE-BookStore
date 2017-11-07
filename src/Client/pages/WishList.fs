module Client.WishList

open Fable.Core
open Fable.Import
open Elmish
open Fable.Helpers.React
open Fable.Helpers.React.Props
open ServerCode.Domain
open Style
open System
open Fable.Core.JsInterop
open Fable.PowerPack
open Fable.PowerPack.Fetch.Fetch_types
open ServerCode

type FormField<'a> =
  { Value: 'a
    Error: string option }

type FormFieldMsg<'a> =
  | FormFieldValueChanged of 'a

type TextField = FormField<string>

module TextField =
    let empty =
      { Value = ""
        Error = None }

type NewBookFormModel =
  { Id: Guid // unique key to reset the vdom-elements, see https://github.com/fable-compiler/fable-suave-scaffold/issues/107#issuecomment-301312224
    Title: TextField
    Authors: TextField
    Link: TextField
    Book: Result<Book, string> option } with
  static member Empty() =
    { Id = Guid.NewGuid()
      Title = TextField.empty
      Authors = TextField.empty
      Link = TextField.empty
      Book = None }

type Model =
  { Token : string
    WishList : WishList
    ResetTime : DateTime option
    FetchError: string option
    NewBookForm: NewBookFormModel }

type NewBookFormMsg =
    | TitleChanged of FormFieldMsg<string>
    | AuthorsChanged of FormFieldMsg<string>
    | LinkChanged of FormFieldMsg<string>
    | Add of Book

/// The different messages processed when interacting with the wish list
type Msg =
    | FetchedWishList of WishList
    | FetchedResetTime of DateTime
    | RemoveBook of Book
    | FetchError of exn
    | NewBookMsg of NewBookFormMsg

/// Get the wish list from the server, used to populate the model
let getWishList token =
    promise {
        let url = ServerUrls.WishList
        let props = 
            [ Fetch.requestHeaders [
                HttpRequestHeaders.Authorization ("Bearer " + token) ]]

        return! Fable.PowerPack.Fetch.fetchAs<WishList> url props
    }

let getResetTime token =
    promise {        
        let url = ServerUrls.ResetTime
        let props = 
            [ Fetch.requestHeaders [
                HttpRequestHeaders.Authorization ("Bearer " + token) ]]

        let! details = Fable.PowerPack.Fetch.fetchAs<ServerCode.Domain.WishListResetDetails> url props
        return details.Time
    }

let loadWishListCmd token = 
    Cmd.ofPromise getWishList token FetchedWishList FetchError

let loadResetTimeCmd token = 
    Cmd.ofPromise getResetTime token FetchedResetTime FetchError


let postWishList (token,wishList) =
    promise {
        let url = ServerUrls.WishList
        let body = toJson wishList
        let props = 
            [ RequestProperties.Method HttpMethod.POST
              Fetch.requestHeaders [
                HttpRequestHeaders.Authorization ("Bearer " + token)
                HttpRequestHeaders.ContentType "application/json" ]
              RequestProperties.Body !^body ]

        return! Fable.PowerPack.Fetch.fetchAs<WishList> url props
    }

let postWishListCmd (token,wishList) = 
    Cmd.ofPromise postWishList (token,wishList) FetchedWishList FetchError

let init (user:Menu.UserData) = 
    { Token = user.Token
      WishList = WishList.New user.UserName
      ResetTime = None
      FetchError = None
      NewBookForm = NewBookFormModel.Empty() }, 
        Cmd.batch [
            loadWishListCmd user.Token
            loadResetTimeCmd user.Token ]

let validatedBook wishList title authors link =
    let book = { Title = title.Value; Authors = authors.Value; Link = link.Value }
    match Validation.verifyBook book, Validation.verifyBookisNotADuplicate wishList book with
    | true, None -> Some (Ok book)
    | _, Some e -> Some (Error e)
    | _, None -> None

let updateNewBookForm wishList (msg:NewBookFormMsg) (model:NewBookFormModel) : NewBookFormModel*Cmd<NewBookFormMsg> =

    match msg with
    | TitleChanged (FormFieldValueChanged value) ->
        let newTitle = { Value = value; Error = Validation.verifyBookTitle value }
        { model with
            Title = newTitle
            Book = validatedBook wishList newTitle model.Authors model.Link }, Cmd.none

    | AuthorsChanged (FormFieldValueChanged value) ->
        let newAuthors = { Value = value; Error = Validation.verifyBookAuthors value }
        { model with
            Authors = newAuthors
            Book = validatedBook wishList model.Title newAuthors model.Link }, Cmd.none

    | LinkChanged (FormFieldValueChanged value) ->
        let newLink = { Value = value; Error = Validation.verifyBookLink value }
        { model with
            Link = newLink
            Book = validatedBook wishList model.Title model.Authors newLink }, Cmd.none

    | Add _ ->
        model, Cmd.none

let update (msg:Msg) model : Model*Cmd<Msg> = 
    match msg with
    | FetchedWishList wishList ->
        let wishList = { wishList with Books = wishList.Books |> List.sortBy (fun b -> b.Title) }
        let book = validatedBook wishList model.NewBookForm.Title model.NewBookForm.Authors model.NewBookForm.Link
        { model with
            WishList = wishList
            NewBookForm = { model.NewBookForm with Book = book } }, Cmd.none

    | FetchedResetTime datetime ->
        { model with
            ResetTime = Some datetime }, Cmd.none

    | RemoveBook book -> 
        let wishList = { model.WishList with Books = model.WishList.Books |> List.filter ((<>) book) }
        let book = validatedBook wishList model.NewBookForm.Title model.NewBookForm.Authors model.NewBookForm.Link
        { model with
            WishList = wishList
            NewBookForm = { model.NewBookForm with Book = book }
            FetchError = None }, postWishListCmd(model.Token,wishList)

    | FetchError e ->
        { model with FetchError = Some e.Message }, Cmd.none

    | NewBookMsg (Add book) ->
        let wishList = { model.WishList with Books = (book :: model.WishList.Books) |> List.sortBy (fun b -> b.Title) }
        { model with
            WishList = wishList
            NewBookForm = NewBookFormModel.Empty()
            FetchError = None }, postWishListCmd(model.Token,wishList)
        
    | NewBookMsg msg ->
        let m,cmd = updateNewBookForm model.WishList msg model.NewBookForm
        { model with
            NewBookForm = m }, Cmd.map NewBookMsg cmd

let viewTextField name placeholder id (field: TextField) dispatch =
    div [ClassName ("form-group has-feedback" + if String.IsNullOrEmpty field.Value then "" else "has-success")] [
        yield div [ClassName "input-group"] [
             yield span [ClassName "input-group-addon"] [span [ClassName "glyphicon glyphicon-pencil"] [] ]
             yield input [
                     Key (name + "_" + id.ToString())
                     HTMLAttr.Type "text"
                     Name name
                     DefaultValue field.Value
                     ClassName "form-control"
                     Placeholder placeholder
                     Required true
                     OnChange (fun (ev:React.FormEvent) -> dispatch (FormFieldValueChanged !!ev.target?value)) ]
             match field.Error with
             | Some e -> yield span [ClassName "glyphicon glyphicon-remove form-control-feedback"] []
             | _ -> ()
        ]
        match field.Error with
        | Some e -> yield p [ClassName "text-danger"][str e]
        | _ -> ()
    ]

let newBookForm (model:NewBookFormModel) dispatch =
    let buttonClasses: IHTMLProp list =
        match model.Book with
        | Some (Ok book) ->
            [ ClassName ("btn btn-primary"); OnClick (fun _ -> dispatch (Add book))]
        | _ -> [ ClassName ("btn btn-disabled") ]

    div [] [
        h4 [] [str "New Book"]

        div [ClassName "container"] [
            div [ClassName "row"] [
                div [ClassName "col-md-8"] [
                    viewTextField "Title" "Please insert book title" model.Id model.Title (TitleChanged >> dispatch)
                    viewTextField "Author" "Please insert authors" model.Id model.Authors (AuthorsChanged >> dispatch)
                    viewTextField "Link" "Please insert link" model.Id model.Link (LinkChanged >> dispatch)
                    div [] [
                        yield button buttonClasses [
                                  i [ClassName "glyphicon glyphicon-plus"; Style [PaddingRight 5]] []
                                  str "Add"
                        ]
                        match model.Book with
                        | Some (Error e) -> yield p [ClassName "text-danger"][str e]
                        | _ -> ()
                    ]
                ]                    
            ]        
        ]
    ]

let view (model:Model) (dispatch: Msg -> unit) = 
    div [] [
        h4 [] [
            let time = model.ResetTime |> Option.map (fun t -> " - Last database reset at " + t.ToString("yyyy-MM-dd HH:mm") + "UTC") |> Option.defaultValue ""
            yield str (sprintf "Wishlist for %s%s" model.WishList.UserName time) ]
        table [ClassName "table table-striped table-hover"] [
            thead [] [
                    tr [] [
                        th [] [str "Title"]
                        th [] [str "Authors"]
                ]
            ]
            tbody[] [
                for book in model.WishList.Books do
                    yield 
                      tr [] [
                        td [] [
                            if String.IsNullOrWhiteSpace book.Link then 
                                yield str book.Title
                            else
                                yield a [ Href book.Link; Target "_blank"] [str book.Title ] ]
                        td [] [ str book.Authors ]
                        td [] [ buttonLink "" (fun _ -> dispatch (RemoveBook book)) [ str "Remove" ] ]
                        ]
            ]
        ]
        newBookForm model.NewBookForm (NewBookMsg >> dispatch)
    ]