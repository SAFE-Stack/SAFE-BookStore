// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r @"packages/build/FAKE/tools/FakeLib.dll"
open Fake
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open Fake.UserInputHelper
open System
open System.IO


let project = "Suave/Fable sample"

let summary = "Suave and Fable sample"

let description = summary

let configuration = "Release"

let clientPath = "./src/Client" |> FullName

let serverPath = "./src/Server/" |> FullName

// --------------------------------------------------------------------------------------
// END TODO: The rest of the file includes standard build steps
// --------------------------------------------------------------------------------------

let run' timeout cmd args dir =
    if execProcess (fun info ->
        info.FileName <- cmd
        if not (String.IsNullOrWhiteSpace dir) then
            info.WorkingDirectory <- dir
        info.Arguments <- args
    ) timeout |> not then
        failwithf "Error while running '%s' with args: %s" cmd args

let run = run' System.TimeSpan.MaxValue

let runWithLog log cmd args dir =
    let timeout = System.TimeSpan.MaxValue
    (true, traceError, log)
    |||> ExecProcessWithLambdas (fun info ->
        info.FileName <- cmd
        if not (String.IsNullOrWhiteSpace dir) then
            info.WorkingDirectory <- dir
        info.Arguments <- args
    ) timeout
    |> function
        | 0 -> ()
        | _ -> failwithf "Error while running '%s' with args: %s" cmd args

let platformTool tool path =
    isUnix |> function | true -> tool | _ -> path

let nodePath = platformTool "node" (@"C:\Program Files\nodejs\node.exe" |> FullName)

let npmTool = platformTool "npm" (@"C:\Program Files\nodejs\npm.cmd" |> FullName)

let runFableWithLog log projDir args =
    runWithLog log nodePath ("node_modules/fable-compiler " + projDir + " " + args) "."

let runFable = runFableWithLog trace

// Read additional information from the release notes document
let release = LoadReleaseNotes "RELEASE_NOTES.md"

// Helper active pattern for project types
let (|Fsproj|Csproj|Vbproj|Shproj|) (projFileName:string) =
    match projFileName with
    | f when f.EndsWith("fsproj") -> Fsproj
    | f when f.EndsWith("csproj") -> Csproj
    | f when f.EndsWith("vbproj") -> Vbproj
    | f when f.EndsWith("shproj") -> Shproj
    | _                           -> failwith (sprintf "Project file %s not supported. Unknown project type." projFileName)

// Generate assembly info files with the right version & up-to-date information
Target "AssemblyInfo" (fun _ ->
    let getAssemblyInfoAttributes projectName =
        [ Attribute.Title (projectName)
          Attribute.Product project
          Attribute.Description summary
          Attribute.Version release.AssemblyVersion
          Attribute.FileVersion release.AssemblyVersion
          Attribute.Configuration configuration ]

    let getProjectDetails projectPath =
        let projectName = System.IO.Path.GetFileNameWithoutExtension(projectPath)
        ( projectPath,
          projectName,
          System.IO.Path.GetDirectoryName(projectPath),
          (getAssemblyInfoAttributes projectName)
        )

    !! "src/**/*.??proj"
    |> Seq.map getProjectDetails
    |> Seq.iter (fun (projFileName, projectName, folderName, attributes) ->
        match projFileName with
        | Fsproj -> CreateFSharpAssemblyInfo (folderName </> "AssemblyInfo.fs") attributes
        | Csproj -> CreateCSharpAssemblyInfo ((folderName </> "Properties") </> "AssemblyInfo.cs") attributes
        | Vbproj -> CreateVisualBasicAssemblyInfo ((folderName </> "My Project") </> "AssemblyInfo.vb") attributes
        | Shproj -> ()
        )
)

// --------------------------------------------------------------------------------------
// Clean build results

Target "Clean" (fun _ ->
    CleanDirs ["bin"; "temp"; "docs/output"; Path.Combine(clientPath,"public/bundle")]
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target "Build" (fun _ ->
    let command = "dotnet"

    let result =
        ExecProcess (fun info ->
            info.FileName <- command
            info.WorkingDirectory <- serverPath
            info.Arguments <- "restore") TimeSpan.MaxValue
    if result <> 0 then failwith "Restore failed"

    let result =
        ExecProcess (fun info ->
            info.FileName <- command
            info.WorkingDirectory <- serverPath
            info.Arguments <- "build") TimeSpan.MaxValue
    if result <> 0 then failwith "Build failed"    
)

Target "BuildClient" (fun _ ->
    run npmTool "install" ""
)


// --------------------------------------------------------------------------------------
// Run the Website


let ipAddress = "localhost"
let port = 8085


Target "Run" (fun _ ->
    let dotnetwatch = async {
        
        let command = "dotnet"
    
        let result =
            ExecProcess (fun info ->
                info.FileName <- command
                info.WorkingDirectory <- serverPath
                info.Arguments <- "watch run") TimeSpan.MaxValue
        if result <> 0 then failwith "Website shut down." }

    let fableWatch = async {
        (clientPath, "-d")
        ||> runFableWithLog (fun msg ->
            if msg.StartsWith "Bundled" then
                sprintf "http://%s:%d/api/refresh" ipAddress port
                |> REST.ExecuteGetCommand "" ""
                |> printfn "%s")
    }

    let openBrowser = async {
        System.Threading.Thread.Sleep(3000)
        Diagnostics.Process.Start("http://"+ ipAddress + sprintf ":%d" port) |> ignore }

    Async.Parallel [| dotnetwatch; fableWatch; openBrowser |]
    |> Async.RunSynchronously
    |> ignore
)

// --------------------------------------------------------------------------------------
// Release Scripts

Target "Release" (fun _ ->
  ()
)

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "All" DoNothing


"Clean"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "BuildClient"
  ==> "Run"
  ==> "All"

RunTargetOrDefault "All"