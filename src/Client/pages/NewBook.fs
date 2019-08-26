module Client.NewBook

open Fable.React
open Fable.React.Props
open Elmish
open ServerCode.Domain
open System
open Client.Styles
open Client.Utils

type Model =
  { NewBook : Book
    NewBookId : Guid // unique key to reset the vdom-elements, see https://github.com/SAFE-Stack/SAFE-BookStore/issues/107#issuecomment-301312224
    TitleErrorText : string option
    AuthorsErrorText : string option
    LinkErrorText : string option
    ImageLinkErrorText : string option }

/// The different messages processed when interacting with the wish list
type Msg =
    | ValidateBook
    | AddBook
    | TitleChanged of string
    | AuthorsChanged of string
    | LinkChanged of string
    | ImageLinkChanged of string


let init () =
    { NewBook = Book.Empty
      NewBookId = Guid.NewGuid()
      TitleErrorText = None
      AuthorsErrorText = None
      ImageLinkErrorText = None
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

    | ImageLinkChanged link ->
        let newBook = { model.NewBook with ImageLink = link }
        { model with
            NewBook = newBook
            ImageLinkErrorText = newBook.ValidateImageLink() }, Cmd.none

    | ValidateBook ->
        let validated =
            { model with
                TitleErrorText = model.NewBook.ValidateTitle()
                AuthorsErrorText = model.NewBook.ValidateAuthors()
                ImageLinkErrorText = model.NewBook.ValidateImageLink()
                LinkErrorText = model.NewBook.ValidateLink() }
        validated,
            if model.NewBook.Validate() then
                Cmd.OfFunc.result AddBook
            else
                Cmd.none

    | AddBook ->
        model, Cmd.none

type Props = {
    Model: Model
    Dispatch: Msg -> unit
}

let view = elmishView "NewBook" (fun { Model = model; Dispatch = dispatch } ->
    div [] [
        h4 [] [str "New Book"]

        div [ClassName "container"] [
            div [ClassName "row"; Key (model.NewBookId.ToString())] [
                div [ClassName "col-md-8"] [
                    validatedTextBox (dispatch << TitleChanged) "Title" "Please insert title" model.TitleErrorText model.NewBook.Title
                    validatedTextBox (dispatch << AuthorsChanged) "Author" "Please insert authors" model.AuthorsErrorText model.NewBook.Authors
                    validatedTextBox (dispatch << LinkChanged) "Link" "Please insert link" model.LinkErrorText model.NewBook.Link
                    validatedTextBox (dispatch << ImageLinkChanged) "ImageLink" "Please insert image link" model.ImageLinkErrorText model.NewBook.ImageLink

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
)