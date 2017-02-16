module Client.Messages

open System

type AppMsg = 
| LoggedIn
| OpenLogIn
| LoginMsg of LoginMsg
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

type UserData = {
    UserName : string 
    Token : string }

type Page = 
| Home 
| Login
| Blog of int 
| Search of string

let toHash =
    function
    | Home -> "#home"
    | Login -> "#login"
    | Blog id -> "#blog/" + (string id)
    | Search query -> "#search/" + query