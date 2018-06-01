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

/// Handle the GET on /api/wishlist

let getUser = 
    freya {
        let! token = Auth.getUserFromAuthToken
        let name =
            token
            |> Option.map(fun t -> t.UserName)
            |> Option.defaultValue "???"
        return name
    } |> Freya.memo

let getWishlist (db:IDatabaseFunctions) =
    freya {
        let! user = getUser
        let! wishlist = 
            db.LoadWishList user
            |> Async.AwaitTask
            |> Freya.fromAsync
        return Server.Represent.json wishlist
    }

let wishLisPostModel =
    freya {
        return! readJson<Domain.WishList>
    } |> Freya.memo

let isAllowedPostWishListRequest = 
    freya {
        let! wishList = wishLisPostModel
        let! token = Auth.getUserFromAuthToken
        let sameUser =
            token
            |> Option.map (fun t -> (t.UserName = wishList.UserName))
            |> Option.defaultValue false
        return sameUser
    }

let forbiddenPostList =
    freya {
        let! token = Auth.getUserFromAuthToken
        let content =
            token
            |> Option.map (fun t ->
                sprintf "WishList is not matching user %s" t.UserName
            )
            |> Option.defaultValue "WishList not ok"

        return Represent.text content
    }  

let whenPost altValue fn =
    freya {
        let! verb = Freya.Optic.get Request.method_
        let! res =
            match verb = POST with
            | true -> fn
            | false -> Freya.init altValue
        return res
    }

let isPostWishlistValid =
    freya {
        let! model = wishLisPostModel
        return not(Validation.verifyWishList model)
    }

let badPostWishlist =
    freya {
        return Represent.text "WishList is not valid"
    }

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
    freya {
        let! wishList = wishLisPostModel
        return Represent.json wishList
    }

let wishListMachine (db:Database.IDatabaseFunctions) =
    freyaMachine {
        methods [GET;HEAD;OPTIONS;POST]
        including Auth.authMachine
        handleOk (getWishlist db)
        
        allowed (whenPost true isAllowedPostWishListRequest)
        handleForbidden forbiddenPostList

        badRequest (whenPost false isPostWishlistValid)
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