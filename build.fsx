// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r @"packages/build/FAKE/tools/FakeLib.dll"
#r "System.IO.Compression.FileSystem"

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


let dotnetcliVersion = "1.0.0-preview4-004233"

let dotnetSDKPath = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) </> "dotnetcore" |> FullName

let dotnetExePath =
    dotnetSDKPath </> (if isWindows then "dotnet.exe" else "dotnet")
    |> FullName

let deployDir = "./deploy"

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
    CleanDirs ["bin"; "temp"; "docs/output"; deployDir; Path.Combine(clientPath,"public/bundle")]
)


Target "InstallDotNetCore" (fun _ ->
    let correctVersionInstalled = 
        try
            if FileInfo(dotnetExePath |> Path.GetFullPath).Exists then
                let processResult = 
                    ExecProcessAndReturnMessages (fun info ->  
                    info.FileName <- dotnetExePath
                    info.WorkingDirectory <- Environment.CurrentDirectory
                    info.Arguments <- "--version") (TimeSpan.FromMinutes 30.)

                processResult.Messages |> separated "" = dotnetcliVersion
                
            else
                false
        with 
        | _ -> false

    if correctVersionInstalled then
        tracefn "dotnetcli %s already installed" dotnetcliVersion
    else
        CleanDir dotnetSDKPath
        let archiveFileName = 
            if isLinux then
                sprintf "dotnet-dev-ubuntu-x64.%s.tar.gz" dotnetcliVersion
            else
                sprintf "dotnet-dev-win-x64.%s.zip" dotnetcliVersion
        let downloadPath = 
                sprintf "https://dotnetcli.azureedge.net/dotnet/Sdk/%s/%s" dotnetcliVersion archiveFileName
        let localPath = Path.Combine(dotnetSDKPath, archiveFileName)

        tracefn "Installing '%s' to '%s'" downloadPath localPath
        
        use webclient = new Net.WebClient()
        webclient.DownloadFile(downloadPath, localPath)

        if isLinux then
            let assertExitCodeZero x =
                if x = 0 then () else
                failwithf "Command failed with exit code %i" x

            Shell.Exec("tar", sprintf """-xvf "%s" -C "%s" """ localPath dotnetSDKPath)
            |> assertExitCodeZero
        else  
            System.IO.Compression.ZipFile.ExtractToDirectory(localPath, dotnetSDKPath)
        
        tracefn "dotnet cli path - %s" dotnetSDKPath
        System.IO.Directory.EnumerateFiles dotnetSDKPath
        |> Seq.iter (fun path -> tracefn " - %s" path)
        System.IO.Directory.EnumerateDirectories dotnetSDKPath
        |> Seq.iter (fun path -> tracefn " - %s%c" path System.IO.Path.DirectorySeparatorChar)

    let oldPath = System.Environment.GetEnvironmentVariable("PATH")
    System.Environment.SetEnvironmentVariable("PATH", sprintf "%s%s%s" dotnetSDKPath (System.IO.Path.PathSeparator.ToString()) oldPath)
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target "Build" (fun _ ->
    let result =
        ExecProcess (fun info ->
            info.FileName <- dotnetExePath
            info.WorkingDirectory <- serverPath
            info.Arguments <- "restore") TimeSpan.MaxValue
    if result <> 0 then failwith "Restore failed"

    let result =
        ExecProcess (fun info ->
            info.FileName <- dotnetExePath
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
        let result =
            ExecProcess (fun info ->
                info.FileName <- dotnetExePath
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
        System.Threading.Thread.Sleep(5000)
        Diagnostics.Process.Start("http://"+ ipAddress + sprintf ":%d" port) |> ignore }

    Async.Parallel [| dotnetwatch; fableWatch; openBrowser |]
    |> Async.RunSynchronously
    |> ignore
)

// --------------------------------------------------------------------------------------
// Release Scripts

Target "Release" (fun _ ->
    let result =
        ExecProcess (fun info ->
            info.FileName <- dotnetExePath
            info.WorkingDirectory <- serverPath
            info.Arguments <- "publish -o \"" + FullName deployDir + "\"") TimeSpan.MaxValue
    if result <> 0 then failwith "Publish failed"
  
    // TODO: 
    let clientDir = deployDir </> "client"
    let publicDir = clientDir </> "public"

    !! "src/msu.SmartMeterHome.Client/public/**/*.*" |> CopyFiles publicDir
    "src/msu.SmartMeterHome.Client/index.html" |> CopyFile clientDir
)

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "All" DoNothing


"Clean"
  ==> "InstallDotNetCore"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "BuildClient"
  ==> "Run"
  ==> "All"
  ==> "Release"

RunTargetOrDefault "All"