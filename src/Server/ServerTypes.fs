module ServerCode.ServerTypes

(* Type we are using on the server only? *)

open System

/// Represents the rights available for a request
type UserRights = 
   { UserName : string }

    