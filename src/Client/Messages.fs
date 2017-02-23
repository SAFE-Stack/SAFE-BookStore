module Client.Messages

open System
open ServerCode.Domain

/// The messages processed during login 
type LoginMsg =
  | GetTokenSuccess of string
  | SetUserName of string
  | SetPassword of string
  | AuthError of exn
  | ClickLogIn

/// The different messages processed when interacting with the wish list
type WishListMsg =
  | LoadForUser of string
  | FetchedWishList of WishList
  | RemoveBook of Book
  | AddBook
  | TitleChanged of string
  | AuthorsChanged of string
  | LinkChanged of string
  | FetchError of exn

/// The different messages processed by the application
type AppMsg = 
  | LoggedIn
  | LoggedOut
  | StorageFailure of exn
  | OpenLogIn
  | LoginMsg of LoginMsg
  | WishListMsg of WishListMsg
  | Logout

/// The user data sent with every message.
type UserData = 
  { UserName : string 
    Token : JWT }

/// The different pages of the application. If you add a new page, then add an entry here.
type Page = 
  | Home 
  | Login
  | WishList

let toHash =
  function
  | Home -> "#home"
  | Login -> "#login"
  | WishList -> "#wishlist"
