module NewBook

open System
open Elmish
open Feliz
open Feliz.DaisyUI
open Shared

type Model = {
    NewBook: Book
    NewBookId: Guid // unique key to reset the vdom-elements, see https://github.com/SAFE-Stack/SAFE-BookStore/issues/107#issuecomment-301312224
    TitleErrorText: string option
    AuthorsErrorText: string option
    LinkErrorText: string option
    ImageLinkErrorText: string option
}

/// The different messages processed when interacting with the wish list
type Msg =
    | ValidateBook
    | AddBook of Book
    | SetTitle of string
    | SetAuthors of string
    | SetLink of string
    | SetImageLink of string
    | Cancel


let init () =
    {
        NewBook = Book.Empty
        NewBookId = Guid.NewGuid()
        TitleErrorText = None
        AuthorsErrorText = None
        ImageLinkErrorText = None
        LinkErrorText = None
    },
    Cmd.none

let update msg model =
    match msg with
    | SetTitle title ->
        let newBook = { model.NewBook with Title = title }

        {
            model with
                NewBook = newBook
                TitleErrorText = newBook.ValidateTitle()
        },
        Cmd.none

    | SetAuthors authors ->
        let newBook = { model.NewBook with Authors = authors }

        {
            model with
                NewBook = newBook
                AuthorsErrorText = newBook.ValidateAuthors()
        },
        Cmd.none

    | SetLink link ->
        let newBook = { model.NewBook with Link = link }

        {
            model with
                NewBook = newBook
                LinkErrorText = newBook.ValidateLink()
        },
        Cmd.none

    | SetImageLink link ->
        let newBook = { model.NewBook with ImageLink = link }

        {
            model with
                NewBook = newBook
                ImageLinkErrorText = newBook.ValidateImageLink()
        },
        Cmd.none

    | ValidateBook ->
        let validated = {
            model with
                TitleErrorText = model.NewBook.ValidateTitle()
                AuthorsErrorText = model.NewBook.ValidateAuthors()
                ImageLinkErrorText = model.NewBook.ValidateImageLink()
                LinkErrorText = model.NewBook.ValidateLink()
        }

        validated,
        if model.NewBook.Validate() then
            model.NewBook |> AddBook |> Cmd.ofMsg
        else
            Cmd.none


    | Cancel
    | AddBook _ -> model, Cmd.none

let errorLabel text =
    Daisy.label [ prop.className "text-error"; prop.text (text |> Option.defaultValue "") ]

let inputField (value: string) (key: string) (onChange: string -> unit) error placeholder =
    Daisy.formControl [
        prop.key key
        prop.children [
            Html.div [
                prop.className "relative"
                prop.children [
                    Html.i [
                        prop.className "fa fa-pencil-alt absolute inset-y-0 end-0 grid items-center mr-2 text-primary"
                    ]
                    Daisy.input [
                        input.bordered
                        prop.className "w-full"
                        prop.placeholder placeholder
                        prop.value value
                        prop.onChange onChange
                    ]
                ]
            ]
            errorLabel error
        ]
    ]

let view model dispatch =
    Html.div [
            Daisy.modal.div [
                modal.open'
                prop.children [
                    Daisy.modalBox.div [
                        prop.children [
                            inputField
                                model.NewBook.Title
                                "title"
                                (SetTitle >> dispatch)
                                model.TitleErrorText
                                "Please insert title"
                            inputField
                                model.NewBook.Authors
                                "author"
                                (SetAuthors >> dispatch)
                                model.AuthorsErrorText
                                "Please insert authors"
                            inputField
                                model.NewBook.Link
                                "link"
                                (SetLink >> dispatch)
                                model.LinkErrorText
                                "Please insert link"
                            inputField
                                model.NewBook.ImageLink
                                "image-link"
                                (SetImageLink >> dispatch)
                                model.ImageLinkErrorText
                                "Please insert image link"
                            Daisy.modalAction [
                                Daisy.button.label [
                                    button.primary
                                    prop.type'.submit
                                    prop.text "Add Book"
                                    prop.onClick (fun _ -> dispatch ValidateBook)
                                    if model.NewBook.Validate() then
                                        prop.htmlFor "add-book"
                                ]
                                Daisy.button.label [
                                    prop.htmlFor "add-book"
                                    button.error
                                    prop.text "Cancel"
                                    prop.onClick (fun _ -> dispatch Cancel)
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    