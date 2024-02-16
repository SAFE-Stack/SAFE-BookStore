module ResetStorage

open Azure.Data.Tables
open Azure.Storage.Blobs
open Quartz
open Storage

let resetStorage blobService tableService = task {
    do! saveWishListToDB tableService Defaults.wishList
    do! StateManagement.storeResetTime blobService
}

type ResetStorageJob(blobService: BlobServiceClient, tableService: TableServiceClient) =
    member this.BlobService = blobService
    member this.TableService = tableService

    interface IJob with
        member this.Execute _ =
            printfn $"Resetting wishlist to default"
            resetStorage this.BlobService this.TableService