module ResetStorage

open Azure.Data.Tables
open Azure.Storage.Blobs
open Quartz
open Storage

type ResetStorageJob(blobService: BlobServiceClient, tableService: TableServiceClient) =
    interface IJob with
        member this.Execute _ = task {
            printfn "Resetting wishlist to default"
            do! saveWishListToDB tableService Defaults.wishList
            do! StateManagement.storeResetTime blobService
        }