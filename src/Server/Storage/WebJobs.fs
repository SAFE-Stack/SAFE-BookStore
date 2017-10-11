namespace ServerCode.Storage

open System.Threading.Tasks
open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Host

/// Contains all reactive web jobs as required by the application.
type WishListWebJobs(connectionString) =
    member __.ClearWishListsWebJob([<TimerTrigger "00:10:00">] timer:TimerInfo) =
        AzureTable.clearWishLists connectionString |> Async.StartAsTask :> Task

/// An extremely crude Job Activator, designed to create WishListWebJobs and nothing else.
type WishListWebJobsActivator(connectionString) =
    interface IJobActivator with
        member __.CreateInstance<'T>() =
            WishListWebJobs connectionString |> box :?> 'T