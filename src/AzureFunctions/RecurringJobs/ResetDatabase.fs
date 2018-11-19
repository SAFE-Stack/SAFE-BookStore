module ResetDatabase

open System
open Microsoft.Azure.WebJobs
open FSharp.Control.Tasks.ContextInsensitive
open ServerCode.Storage
open ServerCode.Storage.AzureTable

let connectionString = System.Environment.GetEnvironmentVariable "STORAGE_CONNECTION"

let connection = AzureConnection connectionString

[<FunctionName("ResetDatabase")>]

let run([<TimerTrigger("0 0 */2 * * *")>] timer:TimerInfo) = 
    let t = task {
        do! AzureTable.clearWishLists connection
    }
    t.Wait()