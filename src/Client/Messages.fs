module Client.Messages

open System
open ServerCode.Domain

type AppMsg = 
| LoggedIn
| OpenLogIn
| LoginMsg of LoginMsg
| WishListMsg of WishListMsg
| Query of string
| Enter
| FetchFailure of string * exn
| FetchSuccess of string * (string list)
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
| FetchError of exn

type UserData = {
    UserName : string 
    Token : string }

type Page = 
| Home 
| Login
| WishList
| Search of string
let toHash =
    function
    | Home -> "#home"
    | Login -> "#login"
    | WishList -> "#wishlist"
    | Search query -> "#search/" + query