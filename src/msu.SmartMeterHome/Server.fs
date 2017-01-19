module msu.SmartMeterHome.Server

open System.IO
open Suave
open Suave.Logging
open System.Net
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors
open Suave.WebSocket
open Suave.Sockets.Control
open Suave.Utils
open Suave.Successful

module private Util =
    let refreshEvent = new Event<unit>()

    let triggerRefreshEvent webPart =
        refreshEvent.Trigger()
        OK "Refreshing..." webPart

    let socketHandler (webSocket : WebSocket) ctx = socket {
        while true do
            let! refreshed =
                Control.Async.AwaitEvent(refreshEvent.Publish)
                |> Suave.Sockets.SocketOp.ofAsync
            do! webSocket.send Text (ASCII.bytes "refreshed" |> Sockets.ByteSegment) true
        }

let startServer clientPath = 
    printfn "Serving files from %s" clientPath
    
    // if not (Directory.Exists clientPath) then
    //     failwithf "Client-HomePath '%s' doesn't exist." clientPath

    // let outPath = Path.Combine(clientPath,"public")
    // if not (Directory.Exists outPath) then
    //     failwithf "Out-HomePath '%s' doesn't exist." outPath

    // if Directory.EnumerateFiles outPath |> Seq.isEmpty then
    //     failwithf "Out-HomePath '%s' is empty." outPath

    let serverConfig =
        { defaultConfig with
            logger = Targets.create LogLevel.Debug [||]
            homeFolder = Some clientPath
            bindings = [ HttpBinding.create HTTP (IPAddress.Parse "0.0.0.0") 8085us] }

    let app =
        choose [
            Filters.GET >=>
                choose [
                    path "/api/websocket" >=> handShake Util.socketHandler
                    path "/api/refresh" >=> Util.triggerRefreshEvent
                    path "/" >=> Files.file (Path.Combine(clientPath, "index.html"))
                    Files.browseHome
                ]
                RequestErrors.NOT_FOUND "Page not found."
        ]

    startWebServer serverConfig app