module Client.NewBook

open Fable.React
open Fable.React.Props
open Elmish
open ServerCode.Domain
open System
open Client.Styles


type Model =
  { NewBook : Book
    NewBookId : Guid // unique key to reset the vdom-elements, see https://github.com/SAFE-Stack/SAFE-BookStore/issues/107#issuecomment-301312224
    TitleErrorText : string option
    AuthorsErrorText : string option
    LinkErrorText : string option }

/// The different messages processed when interacting with the wish list
type Msg =
    | ValidateBook
    | AddBook
    | TitleChanged of string
    | AuthorsChanged of string
    | LinkChanged of string


let init () =
    { NewBook = Book.Empty
      NewBookId = Guid.NewGuid()
      TitleErrorText = None
      AuthorsErrorText = None
      LinkErrorText = None },
        Cmd.none

let update (msg:Msg) model : Model*Cmd<Msg> =
    match msg with
    | TitleChanged title ->
        let newBook = { model.NewBook with Title = title }
        { model with
            NewBook = newBook
            TitleErrorText = newBook.ValidateTitle() }, Cmd.none

    | AuthorsChanged authors ->
        let newBook = { model.NewBook with Authors = authors }
        { model with
            NewBook = newBook
            AuthorsErrorText = newBook.ValidateAuthors() }, Cmd.none

    | LinkChanged link ->
        let newBook = { model.NewBook with Link = link }
        { model with
            NewBook = newBook
            LinkErrorText = newBook.ValidateLink() }, Cmd.none

    | ValidateBook ->
        let validated =
            { model with
                TitleErrorText = model.NewBook.ValidateTitle()
                AuthorsErrorText = model.NewBook.ValidateAuthors()
                LinkErrorText = model.NewBook.ValidateLink() }
        validated,
            if model.NewBook.Validate() then
                Cmd.OfFunc.result AddBook
            else
                Cmd.none

    | AddBook ->
        model, Cmd.none

let view (model:Model) dispatch =
    div [] [
        h4 [] [str "New Book"]

        div [ClassName "container"] [
            div [ClassName "row"; Key (model.NewBookId.ToString())] [
                div [ClassName "col-md-8"] [
                    validatedTextBox (dispatch << TitleChanged) "Title" "Please insert title" model.TitleErrorText model.NewBook.Title
                    validatedTextBox (dispatch << AuthorsChanged) "Author" "Please insert authors" model.AuthorsErrorText model.NewBook.Authors
                    validatedTextBox (dispatch << LinkChanged) "Link" "Please insert link" model.LinkErrorText model.NewBook.Link

                    button [
                        ClassName ("btn " + if model.NewBook.Validate() then "btn-primary" else "btn-disabled")
                        OnClick (fun _ -> dispatch ValidateBook) ] [
                        i [ClassName "glyphicon glyphicon-plus"; Style [PaddingRight 5]] []
                        str "Add"
                    ]
                ]
            ]
        ]
    ]
