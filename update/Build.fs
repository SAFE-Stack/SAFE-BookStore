open Fake.Core
open Fake.IO
open Farmer
open Farmer.Builders
open Farmer.WebApp

open Helpers

initializeContext ()

let sharedPath = Path.getFullName "src/Shared"
let serverPath = Path.getFullName "src/Server"
let clientPath = Path.getFullName "src/Client"
let deployPath = Path.getFullName "deploy"
let sharedTestsPath = Path.getFullName "tests/Shared"
let serverTestsPath = Path.getFullName "tests/Server"
let clientTestsPath = Path.getFullName "tests/Client"

let appName = "safebookstoret"
let storageAccountName = $"{appName}storage"
let logAnalyticsName = $"{appName}-la"
let appInsightsName = $"{appName}-ai"

Target.create "Clean" (fun _ ->
    Shell.cleanDir deployPath
    run dotnet [ "fable"; "clean"; "--yes" ] clientPath // Delete *.fs.js files created by Fable
)

Target.create "InstallClient" (fun _ -> run npm [ "install" ] ".")

Target.create "StartServices" (fun _ ->
    async {
        runParallel [
            "Docker Services", docker [ "compose"; "--project-name"; "bookstore"; "up" ] "."
        ]
    }
    |> Async.Start)

Target.create "Bundle" (fun _ ->
    [
        "server", dotnet [ "publish"; "-c"; "Release"; "-o"; deployPath ] serverPath
        "client", dotnet [ "fable"; "-o"; "output"; "-s"; "--run"; "npx"; "vite"; "build" ] clientPath
    ]
    |> runParallel)

Target.create "Azure" (fun _ ->
    let analytics = logAnalytics {
        name logAnalyticsName
    }
    let insights = appInsights {
        name appInsightsName
        log_analytics_workspace analytics
    }

    let web = webApp {
        name appName
        system_identity
        setting "StorageAccountName" storageAccountName
        operating_system OS.Linux
        runtime_stack Runtime.DotNet80
        sku (Basic "B1")
        zip_deploy "deploy"
        always_on
        link_to_app_insights insights
    }

    let storage = storageAccount {
        name storageAccountName
        grant_access web.SystemIdentity Roles.StorageTableDataContributor
        grant_access web.SystemIdentity Roles.StorageBlobDataContributor
    }

    let deployment = arm {
        location Location.WestEurope
        add_resources [ web; storage; analytics; insights ]
    }

    deployment |> Deploy.execute appName Deploy.NoParameters |> ignore)

Target.create "Run" (fun _ ->
    run dotnet [ "build" ] sharedPath

    [
        "server", dotnet [ "watch"; "run" ] serverPath
        "client", dotnet [ "fable"; "watch"; "-o"; "output"; "-s"; "--run"; "npx"; "vite" ] clientPath
    ]
    |> runParallel)

Target.create "Tests" (fun _ ->
    run dotnet [ "build" ] sharedTestsPath

    [
        "server", dotnet [ "watch"; "run" ] serverTestsPath
        "client", dotnet [ "fable"; "watch"; "-o"; "output"; "-s"; "--run"; "npx"; "vite" ] clientTestsPath
    ]
    |> runParallel)

Target.create "Format" (fun _ -> run dotnet [ "fantomas"; "." ] ".")

open Fake.Core.TargetOperators

let dependencies = [
    "Clean" ==> "InstallClient" ==> "Bundle" ==> "Azure"

    "Clean" ==> "InstallClient" ==> "StartServices" ==> "Run"

    "InstallClient" ==> "Tests"
]

[<EntryPoint>]
let main args = runOrDefault args