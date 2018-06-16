/// Wish list API web parts and data access functions.
module ServerCode.WishList

open System.Threading.Tasks
open ServerCode.Domain
open Freya.Core
open Freya.Machines
open ServerCode.Database
open Freya.Machines.Http
open Freya.Types.Http
open Server
open Freya.Optics.Http
open Server.Represent
open Freya.Optics.Http.Cors
open Freya.Core.Operators
open ServerCode.ServerTypes

/// Handle the GET on /api/wishlist

let getUser = 
    Auth.getUserFromAuthToken
    |> Freya.map (Option.map(fun t -> t.UserName) >> Option.defaultValue "???")
    |> Freya.memo

let getWishlist (db:IDatabaseFunctions) =
    getUser
    |> Freya.bind (fun user ->
        db.LoadWishList user
        |> (Async.AwaitTask >> Freya.fromAsync)
    )
    |> Freya.map Server.Represent.json

let wishLisPostModel =
    readJson<Domain.WishList>
    |> Freya.memo

let whenPost altValue fn =
    Freya.Optic.get Request.method_ 
    |> Freya.bind (function 
        | POST -> fn
        | _ -> Freya.init altValue)
let isAllowedPostWishListRequest = 
    whenPost true 
    <| (freya {
        let! wishList = wishLisPostModel
        let! token = Auth.getUserFromAuthToken
        let sameUser =
            token
            |> Option.map (fun t -> (t.UserName = wishList.UserName))
            |> Option.defaultValue false
        return sameUser
    })

let forbiddenPostList =
    let text (token:UserRights) = sprintf "WishList is not matching user %s" token.UserName

    Auth.getUserFromAuthToken
    |> Freya.map ( Option.map text >> Option.defaultValue "WishList not ok" >> Represent.text)
    
let isPostWishlistValid =
    whenPost false ((Validation.verifyWishList >> not) <!> wishLisPostModel)

let badPostWishlist =
    Represent.text "WishList is not valid"
    |> Freya.init

let postWishlist (saveWishListToDB: WishList -> Task<unit>) =
    freya {
        let! wishList = wishLisPostModel
        let saveTask =
            saveWishListToDB wishList
            |> Async.AwaitTask
            |> Freya.fromAsync
        do! saveTask
    }    

let wishListCreated = 
    wishLisPostModel
    |> Freya.map (Represent.json)

let wishListMachine (db:Database.IDatabaseFunctions) =
    freyaMachine {
        methods [GET;HEAD;OPTIONS;POST]
        including Auth.authMachine
        handleOk (getWishlist db)
        
        allowed isAllowedPostWishListRequest
        handleForbidden forbiddenPostList

        badRequest isPostWishlistValid
        handleBadRequest badPostWishlist

        doPost (postWishlist db.SaveWishList)
        handleCreated wishListCreated
    }

let getResetTime (getLastResetTime: unit -> Task<System.DateTime>) =
    freya {
        let! lastResetTime = 
            getLastResetTime()
            |> Async.AwaitTask
            |> Freya.fromAsync
        return Server.Represent.json lastResetTime
    }

let resetTimeMachine (db:Database.IDatabaseFunctions) = 
    freyaMachine {
        methods [GET;HEAD;OPTIONS]
        handleOk (getResetTime db.GetLastResetTime)
    }