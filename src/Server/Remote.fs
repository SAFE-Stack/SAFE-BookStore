module ServerCode.Remote
open Client.Shared
open Elmish
open ServerCode.ServerTypes
open Elmish.Remoting
open ServerCode.Database
open ServerCode
open Elmish.Remoting
open ServerCode.ServerUrls
module WishList =

    open ServerCode.Domain
    open Client.WishList
    let update (db:#IDatabaseFunctions) msg  (model:UserRights) =
        match msg with
        | FetchWishList ->
            let load = db.LoadWishList >> Async.AwaitTask
            model, Cmd.ofAsync load model.UserName (FetchedWishList >> C) (fun ex -> C (FetchError ex.Message))
        | FetchResetTime ->
            let load = db.GetLastResetTime >> Async.AwaitTask
            model, Cmd.ofAsync load () (FetchedResetTime >> C) (fun ex -> C (FetchError ex.Message))
        | SendWishList wishList when Validation.verifyWishList wishList->
            db.SaveWishList wishList |> Async.AwaitTask |> Async.StartImmediate
            model, Cmd.ofMsg (C (FetchedWishList wishList))
        | SendWishList _ ->
            model, Cmd.ofMsg (C (FetchError "WishList is not valid"))

module Login =
    open Client.Login
    let update msg model =
        match msg with
        | SendLogin login when login.IsValid() ->
            Some {UserName=login.UserName}, Cmd.ofMsg (C (Auth.createUserData login |> LoginSuccess))
        | SendLogin login ->
            let error = sprintf "User '%s' can't be logged in." login.UserName
            None, Cmd.ofMsg (C (AuthError error) )

let init () =
    None, Cmd.ofMsg (C Connected)

let update (db:#IDatabaseFunctions) msg model =
    match msg with
    | SendToken token ->
        let model = JsonWebToken.isValid token
        let cmd =
            match model with
            | Some user -> Cmd.ofMsg (C (LoggedIn {Token = token; UserName = user.UserName}))
            | None -> Cmd.none
        model, cmd
    | ClearUser ->
        None, Cmd.none
    | WishListServerMsg msg ->
        match model with
        | None -> model, Cmd.none
        | Some user ->
            let _, cmd = WishList.update db msg user
            model, cmd |> Cmd.remoteMap WishListServerMsg WishListMsg
    | LoginServerMsg msg ->
        match model with
        |None ->
            let model, cmd = Login.update msg model
            model, cmd |> Cmd.remoteMap LoginServerMsg LoginMsg
        | Some _ -> model, Cmd.none

let remote db =
    ServerProgram.mkProgram init (update db)
    |> ServerProgram.runServerAt Giraffe.server APIUrls.Socket