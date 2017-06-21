module Client.Model 

open System
open ServerCode.Domain

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