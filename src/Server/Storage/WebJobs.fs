namespace ServerCode.Storage

open Microsoft.Azure.WebJobs
open Microsoft.Azure.WebJobs.Host

/// Contains all reactive web jobs as required by the application.
type WishListWebJobs(connectionString) =
    member __.ClearWishListsWebJob([<TimerTrigger "00:10:00">] timer:TimerInfo) =
        AzureTable.clearWishLists connectionString

/// An extremely crude Job Activator, designed to create WishListWebJobs and nothing else.
type WishListWebJobsActivator(connectionString) =
    interface IJobActivator with
        member __.CreateInstance<'T>() =
            WishListWebJobs connectionString |> box :?> 'T

/// Start up background Azure web jobs with the given Azure connection.
module WebJobs =
    open ServerCode.Storage.AzureTable

    let startWebJobs azureConnection =
        let host =
            let config =
                let (AzureConnection connectionString) = azureConnection
                JobHostConfiguration(
                    DashboardConnectionString = connectionString,
                    StorageConnectionString = connectionString)
            config.UseTimers()
            config.JobActivator <- WishListWebJobsActivator azureConnection
            new JobHost(config)
        host.Start()