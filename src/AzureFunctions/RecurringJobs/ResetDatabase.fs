module ResetDatabase

open Microsoft.Azure.WebJobs
open FSharp.Control.Tasks.ContextInsensitive
open ServerCode.Storage
open ServerCode.Storage.AzureTable
open ServerCode.Storage.AzureTable.StateManagement

let connectionString = System.Environment.GetEnvironmentVariable "STORAGE_CONNECTION"

let connection = AzureConnection connectionString

[<FunctionName("ResetDatabase")>]

let run([<TimerTrigger("0 0 */2 * * *")>] timer:TimerInfo) =
    let t = task {
        let defaults = Defaults.defaultWishList "test"
        do! saveWishListToDB connection defaults
        do! storeResetTime connection
    }
    t.Wait()