module Page.Home

open Elmish
open Feliz.DaisyUI

type Book = {
  Title : string
  Authors : string
  Link : string
  ImageLink : string
}

let mockBooks = seq {
    {
        Title = "Get Programming with F#"
        Authors = "Isaac Abraham"
        ImageLink = "/images/Isaac.png"
        Link = "https://www.manning.com/books/get-programming-with-f-sharp"
    }
    {
        Title = "Mastering F#"
        Authors = "Alfonso Garcia-Caro Nunez"
        ImageLink = "/images/Alfonso.jpg"
        Link = "https://www.amazon.com/Mastering-F-Alfonso-Garcia-Caro-Nunez-ebook/dp/B01M112LR9"
    }
    {
        Title = "Stylish F#"
        Authors = "	Kit Eason"
        ImageLink = "/images/Kit.jpg"
        Link = "https://www.apress.com/la/book/9781484239995"
    }
}

type Model = { Books: Book seq }

type Msg =
    | GotBooks of Book seq

let init () =
    let model = { Books = mockBooks }

    model, Cmd.none

let update msg model =
    match msg with
    | GotBooks books -> model, Cmd.none

open Feliz

let bookRow book =
    let link = Daisy.link [ link.hover; link.primary; prop.target "_blank"; prop.text book.Title; prop.href book.Link ]
    let image = Html.img [ prop.src book.ImageLink ]
    Html.tr [ prop.className "hover:bg-accent"; prop.children [ Html.td link; Html.td book.Authors; Html.td image ] ]

let view model dispatch =
    Html.div [
        prop.className "overflow-y-auto"
        prop.children [
            Daisy.table [
                prop.children [
                    Html.tbody [
                        for book in model.Books do
                            bookRow book
                    ]
                    Html.thead [ Html.tr [ Html.th "Title"; Html.th "Authors"; Html.th "Image" ] ]
                ]
            ]
        ]
    ]

