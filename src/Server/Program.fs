module ServerCode.Program

open System.IO

[<EntryPoint>]
let main args =
    try
        let clientPath =
            match args |> Array.toList with
            | clientPath:: _  when Directory.Exists clientPath -> clientPath
            | _ -> 
                let devPath = Path.Combine("..","Client")
                if Directory.Exists devPath then devPath else
                @"./client"

        Server.startServer (Path.GetFullPath clientPath)
        0
    with
    | exn ->
        let color = System.Console.ForegroundColor
        System.Console.ForegroundColor <- System.ConsoleColor.Red
        System.Console.WriteLine(exn.Message)
        System.Console.ForegroundColor <- color
        1