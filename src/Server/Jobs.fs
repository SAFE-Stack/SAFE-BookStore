module Jobs

open Microsoft.Extensions.DependencyInjection
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

let addResetStorageJob (services: IServiceCollection) =
    services
        .AddQuartz(fun config ->
            let jobName = JobKey "reset-storage"

            config
                .AddJob<ResetStorageJob>(jobName)
                .AddTrigger(fun trigger -> trigger.ForJob(jobName).WithCronSchedule("30 * * * * ?") |> ignore)
            |> ignore)
        .AddQuartzHostedService(fun options -> options.WaitForJobsToComplete <- true)
    |> ignore

    services