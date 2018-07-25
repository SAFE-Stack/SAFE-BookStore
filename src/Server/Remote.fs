module ServerCode.Remote
open Client.Shared
open Elmish
open ServerCode.ServerTypes
open ServerCode.Database
open ServerCode
open Elmish.Bridge
open ServerCode.ServerUrls
module WishList =

    open ServerCode.Domain
    open Client.WishList
    let update clientDispatch (db:#IDatabaseFunctions) msg  (model:UserRights) =
        match msg with
        | FetchWishList ->
            let load = db.LoadWishList >> Async.AwaitTask
            model, Cmd.ofAsync load model.UserName FetchWishListSuccess (fun ex -> FetchFailure ex.Message)
        | FetchWishListSuccess wishList ->
            clientDispatch (FetchedWishList wishList)
            model, Cmd.none
        | FetchResetTime ->
            let load = db.GetLastResetTime >> Async.AwaitTask
            model, Cmd.ofAsync load () FetchResetTimeSuccess (fun ex -> FetchFailure ex.Message)
        | FetchResetTimeSuccess resetTime ->
            clientDispatch (FetchedResetTime resetTime)
            model, Cmd.none
        | FetchFailure error ->
            clientDispatch (FetchError error)
            model, Cmd.none
        | SendWishList wishList when Validation.verifyWishList wishList->
            db.SaveWishList wishList |> Async.AwaitTask |> Async.StartImmediate
            clientDispatch (FetchedWishList wishList)
            model, Cmd.none
        | SendWishList _ ->
            clientDispatch (FetchError "WishList is not valid")
            model, Cmd.none

module Login =
    open Client.Login
    let update clientDispatch msg model =
        match msg with
        | SendLogin login when login.IsValid() ->
            clientDispatch (Auth.createUserData login |> LoginSuccess)
            Some {UserName=login.UserName}, Cmd.none
        | SendLogin login ->
            let error = sprintf "User '%s' can't be logged in." login.UserName
            clientDispatch (AuthError error)
            None, Cmd.none

let init clientDispatch () =
    clientDispatch Connected
    None, Cmd.none

let update (db:#IDatabaseFunctions) clientDispatch msg model =
    match msg with
    | SendToken token ->
        let model = JsonWebToken.isValid token
        model |> Option.iter (fun user -> clientDispatch (LoggedIn {Token = token; UserName = user.UserName}) )
        model, Cmd.none
    | ClearUser ->
        None, Cmd.none
    | WishListServerMsg msg ->
        match model with
        | None -> model, Cmd.none
        | Some user ->
            let _, cmd = WishList.update (WishListMsg >> clientDispatch) db msg user
            model, cmd |> Cmd.map WishListServerMsg
    | LoginServerMsg msg ->
        match model with
        |None ->
            let model, cmd = Login.update (LoginMsg >> clientDispatch) msg model
            model, cmd |> Cmd.map LoginServerMsg
        | Some _ -> model, Cmd.none

let remote db =
    Bridge.mkServer APIUrls.Socket init (update db)
    |> Bridge.withConsoleTrace
    |> Bridge.register WishListServerMsg
    |> Bridge.register LoginServerMsg
    |> Bridge.run Giraffe.server