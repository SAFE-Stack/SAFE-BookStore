namespace Shared

open System

type JWT = string

// Login credentials.
type Login = {
    UserName: string
    Password: string
} with

    member this.IsValid() =
        not (
            (this.UserName <> "test" || this.Password <> "test")
            && (this.UserName <> "test2" || this.Password <> "test2")
        )

type UserName =
    | UserName of string

    member this.Value =
        match this with
        | UserName v -> v

type UserData = { UserName: UserName; Token: JWT }

type Book = {
    Title: string
    Authors: string
    Link: string
    ImageLink: string
} with

    static member Empty = {
        Title = ""
        Authors = ""
        Link = ""
        ImageLink = ""
    }

    member this.ValidateTitle() =
        if String.IsNullOrWhiteSpace this.Title then
            Some "No title was entered"
        else
            None

    member this.ValidateAuthors() =
        if String.IsNullOrWhiteSpace this.Authors then
            Some "No author was entered"
        else
            None

    member this.ValidateLink() =
        if String.IsNullOrWhiteSpace this.Link then
            Some "No link was entered"
        else
            None

    member this.ValidateImageLink() =
        if String.IsNullOrWhiteSpace this.ImageLink then
            Some "No image link was entered"
        else
            None

    member this.Validate() =
        this.ValidateTitle() = None
        && this.ValidateAuthors() = None
        && this.ValidateImageLink() = None
        && this.ValidateLink() = None

type WishList = {
    UserName: UserName
    Books: Book list
} with

    member this.VerifyNewBookIsNotADuplicate book =
        // only compare author and title; ignore url because it is not directly user-visible
        if
            this.Books
            |> List.exists (fun b -> (b.Authors, b.Title) = (book.Authors, book.Title))
        then
            Error "Your wishlist contains this book already."
        else
            Ok book

module Route =
    let builder typeName methodName = $"/api/%s{typeName}/%s{methodName}"

type IBooksApi = {
    getBooks: unit -> Async<Book seq>
    getWishlist: UserName -> Async<WishList>
    addBook: UserName * Book -> Async<Book>
    removeBook: UserName * string -> Async<string>
    getLastResetTime: unit -> Async<DateTime>
}

type IUserApi = { login: Login -> Async<UserData> }