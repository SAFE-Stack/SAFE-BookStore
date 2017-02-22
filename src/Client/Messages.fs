module Client.Messages

open System
open ServerCode.Domain

type AppMsg = 
| LoggedIn
| LoggedOut
| StorageFailure of exn
| OpenLogIn
| LoginMsg of LoginMsg
| WishListMsg of WishListMsg
| Logout

and LoginMsg =
| GetTokenSuccess of string
| SetUserName of string
| SetPassword of string
| AuthError of exn
| ClickLogIn

and WishListMsg =
| LoadForUser of string
| FetchedWishList of WishList
| RemoveBook of Book
| AddBook
| TitleChanged of string
| AuthorsChanged of string
| LinkChanged of string
| FetchError of exn

type UserData = {
    UserName : string 
    Token : string }

type Page = 
| Home 
| Login
| WishList

let toHash =
    function
    | Home -> "#home"
    | Login -> "#login"
    | WishList -> "#wishlist"