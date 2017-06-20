module Client.Messages

open System
open ServerCode.Domain

/// The different messages processed by the application
type Msg = 
  | MenuMsg of Client.Menu.Msg
  | LoggedIn
  | LoggedOut
  | LogOut
  | StorageFailure of exn
  | OpenLogIn
  | LoginMsg of Client.Login.Msg
  | WishListMsg of Client.WishList.Msg
