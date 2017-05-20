// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r @"packages/build/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Git
open Fake.AssemblyInfoFile
open Fake.ReleaseNotesHelper
open System
open System.IO
open Fake.Testing.Expecto


let project = "Suave/Fable sample"

let summary = "Suave and Fable sample"

let description = summary

let configuration = "Release"

let clientPath = "./src/Client" |> FullName

let serverPath = "./src/Server/" |> FullName

let dotnetcliVersion = "1.0.4"

let mutable dotnetExePath = "dotnet"

let deployDir = "./deploy"


// Pattern specifying assemblies to be tested using expecto
let testExecutables = "test/**/bin/Release/*Tests*.exe"

let dockerUser = "forki"
let dockerImageName = "fable-suave"

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

let runDotnet workingDir args =
    let result =
        ExecProcess (fun info ->
            info.FileName <- dotnetExePath
            info.WorkingDirectory <- workingDir
            info.Arguments <- args) TimeSpan.MaxValue
    if result <> 0 then failwithf "dotnet %s failed" args

let platformTool tool winTool =
    let tool = if isUnix then tool else winTool
    tool
    |> ProcessHelper.tryFindFileOnPath
    |> function Some t -> t | _ -> failwithf "%s not found" tool

let nodeTool = platformTool "node" "node.exe"
let npmTool = platformTool "npm" "npm.cmd"
let yarnTool = platformTool "yarn" "yarn.cmd"

// Read additional information from the release notes document
let release = LoadReleaseNotes "RELEASE_NOTES.md"
let packageVersion = SemVerHelper.parse release.NugetVersion


// --------------------------------------------------------------------------------------
// Clean build results

Target "Clean" (fun _ ->
    CleanDirs ["bin"; "temp"; "docs/output"; deployDir; Path.Combine(clientPath,"public/bundle")]
)

Target "InstallDotNetCore" (fun _ ->
    dotnetExePath <- DotNetCli.InstallDotNetSDK dotnetcliVersion
)

// --------------------------------------------------------------------------------------
// Build library & test project

Target "InstallServer" (fun _ ->
    runDotnet serverPath "restore"
)

Target "BuildServer" (fun _ ->
    runDotnet serverPath "build"
)

Target "InstallClient" (fun _ ->
    printfn "Node version:"
    run nodeTool "--version" __SOURCE_DIRECTORY__
    printfn "Yarn version:"
    run yarnTool "--version" __SOURCE_DIRECTORY__
    run yarnTool "install" __SOURCE_DIRECTORY__
    runDotnet clientPath "restore"
)

Target "BuildClient" (fun _ ->
    runDotnet clientPath "fable webpack -- -p"
)


Target "BuildTests" (fun _ ->
    !! "./Tests.sln"
    |> MSBuildReleaseExt "" [ ("Configuration", configuration); ("Platform", "Any CPU") ] "Rebuild"
    |> ignore
)

// --------------------------------------------------------------------------------------
// Rename driver for macOS or Linux

Target "RenameDrivers" (fun _ ->
    if not isWindows then
        run npmTool "install phantomjs" ""
    try
        if isMacOS && not <| File.Exists "test/UITests/bin/Release/chromedriver" then
            Fake.FileHelper.Rename "test/UITests/bin/Release/chromedriver" "test/UITests/bin/Release/chromedriver_macOS"
        elif isLinux then
            Fake.FileHelper.Rename "test/UITests/bin/Release/chromedriver" "test/UITests/bin/Release/chromedriver_linux64"
    with
    | exn -> failwithf "Could not rename chromedriver at test/UITests/bin/Release/chromedriver. Message: %s" exn.Message
)

Target "RunTests" (fun _ ->
    ActivateFinalTarget "KillProcess"

    let serverProcess =
        let info = System.Diagnostics.ProcessStartInfo()
        info.FileName <- dotnetExePath
        info.WorkingDirectory <- serverPath
        info.Arguments <- " run"
        info.UseShellExecute <- false
        System.Diagnostics.Process.Start info

    System.Threading.Thread.Sleep 5000 |> ignore  // give server some time to start

    !! testExecutables
    |> Expecto (fun p -> { p with Parallel = false } )
    |> ignore

    serverProcess.Kill()
)

// --------------------------------------------------------------------------------------
// Run the Website

let ipAddress = "localhost"
let port = 8080

FinalTarget "KillProcess" (fun _ ->
    killProcess "dotnet"
    killProcess "dotnet.exe"
)


Target "Run" (fun _ ->
    let dotnetwatch = async {
        let result =
            ExecProcess (fun info ->
                info.FileName <- dotnetExePath
                info.WorkingDirectory <- serverPath
                info.Arguments <- "watch run") TimeSpan.MaxValue
        if result <> 0 then failwith "Website shut down." }

    let fablewatch = async { runDotnet clientPath "fable webpack-dev-server" }
    let openBrowser = async {
        System.Threading.Thread.Sleep(5000)
        Diagnostics.Process.Start("http://"+ ipAddress + sprintf ":%d" port) |> ignore }

    Async.Parallel [| dotnetwatch; fablewatch; openBrowser |]
    |> Async.RunSynchronously
    |> ignore
)


// --------------------------------------------------------------------------------------
// Release Scripts


Target "PrepareRelease" (fun _ ->
    Git.Branches.checkout "" false "master"
    Git.CommandHelper.directRunGitCommand "" "fetch origin" |> ignore
    Git.CommandHelper.directRunGitCommand "" "fetch origin --tags" |> ignore

    StageAll ""
    Git.Commit.Commit "" (sprintf "Bumping version to %O" release.NugetVersion)
    Git.Branches.pushBranch "" "origin" "master"

    let tagName = string release.NugetVersion
    Git.Branches.tag "" tagName
    Git.Branches.pushTag "" "origin" tagName

    let result =
        ExecProcess (fun info ->
            info.FileName <- "docker"
            info.Arguments <- sprintf "tag %s/%s %s/%s:%s" dockerUser dockerImageName dockerUser dockerImageName release.NugetVersion) TimeSpan.MaxValue
    if result <> 0 then failwith "Docker tag failed"
)

Target "CreateDockerImage" (fun _ ->
    let result =
        ExecProcess (fun info ->
            info.FileName <- dotnetExePath
            info.WorkingDirectory <- serverPath
            info.Arguments <- "publish -c Release -o \"" + FullName deployDir + "\"") TimeSpan.MaxValue
    if result <> 0 then failwith "Publish failed"

    let clientDir = deployDir </> "client"
    let publicDir = clientDir </> "public"
    let jsDir = clientDir </> "js"
    let cssDir = clientDir </> "css"
    let imageDir = clientDir </> "Images"

    !! "src/Client/public/**/*.*" |> CopyFiles publicDir
    !! "src/Client/js/**/*.*" |> CopyFiles jsDir
    !! "src/Client/css/**/*.*" |> CopyFiles cssDir
    !! "src/Images/**/*.*" |> CopyFiles imageDir

    "src/Client/index.html" |> CopyFile clientDir

    let result =
        ExecProcess (fun info ->
            info.FileName <- "docker"
            info.Arguments <- sprintf "build -t %s/%s ." dockerUser dockerImageName) TimeSpan.MaxValue
    if result <> 0 then failwith "Docker build failed"
)

Target "Deploy" (fun _ ->
    let result =
        ExecProcess (fun info ->
            info.FileName <- "docker"
            info.WorkingDirectory <- deployDir
            info.Arguments <- sprintf "login --username \"%s\" --password \"%s\"" dockerUser (getBuildParam "DockerPassword")) TimeSpan.MaxValue
    if result <> 0 then failwith "Docker login failed"

    let result =
        ExecProcess (fun info ->
            info.FileName <- "docker"
            info.WorkingDirectory <- deployDir
            info.Arguments <- sprintf "push %s/%s:%s" dockerUser dockerImageName release.NugetVersion) TimeSpan.MaxValue
    if result <> 0 then failwith "Docker push failed"
)

// -------------------------------------------------------------------------------------
Target "Build" DoNothing
Target "All" DoNothing

"Clean"
  ==> "InstallDotNetCore"
  ==> "InstallServer"
  ==> "InstallClient"
  ==> "BuildServer"
  ==> "BuildClient"
  ==> "BuildTests"
  ==> "RenameDrivers"
  ==> "RunTests"
  ==> "All"
  ==> "CreateDockerImage"
  ==> "PrepareRelease"
  ==> "Deploy"

"BuildClient"
  ==> "Build"

"InstallClient"
  ==> "Run"

RunTargetOrDefault "All"
