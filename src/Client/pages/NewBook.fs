module Client.NewBook

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Elmish
open ServerCode.Domain
open System


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

    let titleStatus = if String.IsNullOrEmpty model.NewBook.Title then "" else "has-success"

    let authorStatus = if String.IsNullOrEmpty model.NewBook.Authors then "" else "has-success"

    let linkStatus = if String.IsNullOrEmpty model.NewBook.Link then "" else "has-success"

    div [] [
        h4 [] [str "New Book"]

        div [ClassName "container"] [
            div [ClassName "row"] [
                div [ClassName "col-md-8"] [
                    div [ClassName ("form-group has-feedback " + titleStatus)] [
                        yield div [ClassName "input-group"] [
                             yield span [ClassName "input-group-addon"] [span [ClassName "glyphicon glyphicon-pencil"] [] ]
                             yield input [
                                     Key ("Title_" + model.NewBookId.ToString())
                                     HTMLAttr.Type "text"
                                     Name "Title"
                                     DefaultValue model.NewBook.Title
                                     ClassName "form-control"
                                     Placeholder "Please insert book title"
                                     Required true
                                     OnChange (fun ev -> dispatch (TitleChanged ev.Value)) ]
                             match model.TitleErrorText with
                             | Some e -> yield span [ClassName "glyphicon glyphicon-remove form-control-feedback"] []
                             | _ -> ()
                        ]
                        match model.TitleErrorText with
                        | Some e -> yield p [ClassName "text-danger"][str e]
                        | _ -> ()
                    ]
                    div [ClassName ("form-group has-feedback " + authorStatus) ] [
                         yield div [ClassName "input-group"][
                             yield span [ClassName "input-group-addon"] [span [ClassName "glyphicon glyphicon-user"] [] ]
                             yield input [
                                     Key ("Author_" + model.NewBookId.ToString())
                                     HTMLAttr.Type "text"
                                     Name "Author"
                                     DefaultValue model.NewBook.Authors
                                     ClassName "form-control"
                                     Placeholder "Please insert authors"
                                     Required true
                                     OnChange (fun ev -> dispatch (AuthorsChanged ev.Value))]
                             match model.AuthorsErrorText with
                             | Some e -> yield span [ClassName "glyphicon glyphicon-remove form-control-feedback"] []
                             | _ -> ()
                         ]
                         match model.AuthorsErrorText with
                         | Some e -> yield p [ClassName "text-danger"][str e]
                         | _ -> ()
                    ]
                    div [ClassName ("form-group has-feedback " + linkStatus)] [
                         yield div [ClassName "input-group"] [
                             yield span [ClassName "input-group-addon"] [span [ClassName "glyphicon glyphicon glyphicon-pencil"] [] ]
                             yield input [
                                    Key ("Link_" + model.NewBookId.ToString())
                                    HTMLAttr.Type "text"
                                    Name "Link"
                                    DefaultValue model.NewBook.Link
                                    ClassName "form-control"
                                    Placeholder "Please insert link"
                                    Required true
                                    OnChange (fun ev -> dispatch (LinkChanged ev.Value))]
                             match model.LinkErrorText with
                             | Some e -> yield span [ClassName "glyphicon glyphicon-remove form-control-feedback"] []
                             | _ -> ()
                         ]
                         match model.LinkErrorText with
                         | Some e -> yield p [ClassName "text-danger"][str e]
                         | _ -> ()
                    ]
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
