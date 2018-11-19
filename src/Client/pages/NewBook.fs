module Client.NewBook

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Elmish
open ServerCode.Domain
open System
open Client.Styles


type Model =
  { NewBook : Book
    NewBookId : Guid // unique key to reset the vdom-elements, see https://github.com/SAFE-Stack/SAFE-BookStore/issues/107#issuecomment-301312224    
    TitleErrorText : string option
    AuthorsErrorText : string option
    LinkErrorText : string option
    ErrorMsg : string option }

/// The different messages processed when interacting with the wish list
type Msg =
    | ValidateBook
    | AddBook
    | TitleChanged of string
    | AuthorsChanged of string
    | LinkChanged of string


let init () =
    { NewBook = Book.empty
      NewBookId = Guid.NewGuid()
      TitleErrorText = None
      AuthorsErrorText = None
      LinkErrorText = None
      ErrorMsg = None },
        Cmd.none

let update (msg:Msg) model : Model*Cmd<Msg> =
    match msg with
    | TitleChanged title ->
        let newBook = { model.NewBook with Title = title }
        { model with
            NewBook = newBook
            TitleErrorText = Validation.verifyBookTitle title }, Cmd.none

    | AuthorsChanged authors ->
        let newBook = { model.NewBook with Authors = authors }
        { model with
            NewBook = newBook
            AuthorsErrorText = Validation.verifyBookAuthors authors }, Cmd.none

    | LinkChanged link ->
        let newBook = { model.NewBook with Link = link }
        { model with
            NewBook = newBook
            LinkErrorText = Validation.verifyBookLink link }, Cmd.none

    | ValidateBook ->
        let validated =
            { model with
                TitleErrorText = Validation.verifyBookTitle model.NewBook.Title
                AuthorsErrorText = Validation.verifyBookAuthors model.NewBook.Authors
                LinkErrorText = Validation.verifyBookLink model.NewBook.Link}
        validated, 
            if validated.TitleErrorText = None && validated.AuthorsErrorText = None && validated.LinkErrorText = None then
                Cmd.ofMsg AddBook
            else
                Cmd.none

    | AddBook ->
        model, Cmd.none

let view (model:Model) dispatch =
    let buttonInactive =
        String.IsNullOrEmpty model.NewBook.Title ||
        String.IsNullOrEmpty model.NewBook.Authors ||
        String.IsNullOrEmpty model.NewBook.Link ||
        model.ErrorMsg <> None

    let buttonTag = if buttonInactive then  "btn-disabled" else "btn-primary"

    div [] [
        h4 [] [str "New Book"]

        div [ClassName "container"] [
            div [ClassName "row"] [
                div [ClassName "col-md-8"] [
                    validatedTextBox (dispatch << TitleChanged) "Title" ("Title_" + model.NewBookId.ToString()) "Please insert book title" model.TitleErrorText model.NewBook.Title
                    validatedTextBox (dispatch << AuthorsChanged) "Author" ("Author_" + model.NewBookId.ToString()) "Please insert authors"  model.AuthorsErrorText model.NewBook.Authors
                    validatedTextBox (dispatch << LinkChanged) "Link" ("Link_" + model.NewBookId.ToString()) "Please insert link"  model.LinkErrorText model.NewBook.Link
                    div [] [
                        yield button [ ClassName ("btn " + buttonTag); OnClick (fun _ -> dispatch ValidateBook)] [
                                  i [ClassName "glyphicon glyphicon-plus"; Style [PaddingRight 5]] []
                                  str "Add"
                        ]
                        match model.ErrorMsg with
                        | None -> ()
                        | Some e -> yield p [ClassName "text-danger"][str e]
                    ]
                ]
            ]
        ]
    ]
