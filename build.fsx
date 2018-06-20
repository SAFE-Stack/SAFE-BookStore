// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------
#r @"paket: groupref Build //"
#load "./.fake/build.fsx/intellisense.fsx"

#if !FAKE
  #r "netstandard" // Temp fix for https://github.com/fsharp/FAKE/issues/1985
#endif

open Fake.Core
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Tools
open System
open System.IO

let project = "Suave/Fable sample"

let summary = "SAFE sample"

let description = summary

let configuration = "Release"

let clientPath = "./src/Client" |> Path.getFullName

let serverPath = "./src/Server/" |> Path.getFullName

let serverTestsPath = "./test/ServerTests" |> Path.getFullName
let clientTestsPath = "./test/UITests" |> Path.getFullName

let dotnetcliVersion = DotNet.getSDKVersionFromGlobalJson()
let dotnetExePath = "dotnet"

let deployDir = "./deploy"

// Pattern specifying assemblies to be tested using expecto
let clientTestExecutables = "test/UITests/**/bin/**/*Tests*.exe"

let dockerUser = Environment.environVar "DockerUser"
let dockerPassword = Environment.environVar "DockerPassword"
let dockerLoginServer = Environment.environVar "DockerLoginServer"
let dockerImageName = Environment.environVar "DockerImageName"

// --------------------------------------------------------------------------------------
// END TODO: The rest of the file includes standard build steps
// --------------------------------------------------------------------------------------

let run' timeout cmd args dir =
    let result =
        Process.execSimple (fun info ->
            { info with
                FileName = cmd
                Arguments = args
                WorkingDirectory = if String.isNullOrWhiteSpace dir then info.WorkingDirectory else dir }
        ) timeout
    if result <> 0 then failwithf "Error while running '%s' with args: %s" cmd args

let run = run' System.TimeSpan.MaxValue

let runDotnet workingDir args =
    let result =
        Process.execSimple (fun info ->
            { info with
                FileName = dotnetExePath
                WorkingDirectory = workingDir
                Arguments = args }
        ) TimeSpan.MaxValue
    if result <> 0 then failwithf "dotnet %s failed" args

let platformTool tool winTool =
    let tool = if Environment.isUnix then tool else winTool
    tool
    |> Process.tryFindFileOnPath
    |> function Some t -> t | _ -> failwithf "%s not found" tool

let nodeTool = platformTool "node" "node.exe"
let npmTool = platformTool "npm" "npm.cmd"
let yarnTool = platformTool "yarn" "yarn.cmd"

if not <| Environment.isWindows then
    // We have to set the FrameworkPathOverride so that dotnet sdk invocations know
    // where to look for full-framework base class libraries
    let mono = platformTool "mono" "mono"
    let frameworkPath = IO.Path.GetDirectoryName(mono) </> ".." </> "lib" </> "mono" </> "4.5"
    Environment.setEnvironVar "FrameworkPathOverride" frameworkPath


// Read additional information from the release notes document
let releaseNotes = File.ReadAllLines "RELEASE_NOTES.md"

let release = ReleaseNotes.parse releaseNotes

// --------------------------------------------------------------------------------------
// Clean build results
Target.create "Clean" (fun _ ->
    !! "src/**/bin"
    ++ "test/**/bin"
    |> Shell.cleanDirs

    !! "src/**/obj/*.nuspec"
    ++ "test/**/obj/*.nuspec"
    |> Seq.iter Shell.rm

    Shell.cleanDirs ["bin"; "temp"; "docs/output"; deployDir; Path.Combine(clientPath,"public/bundle")]
)

Target.create "InstallDotNetCore" (fun _ ->
    let setParams (options : DotNet.CliInstallOptions) =
        { options with
            Version = DotNet.Version dotnetcliVersion }

    DotNet.install setParams |> ignore
)

// --------------------------------------------------------------------------------------
// Build library & test project
Target.create "BuildServer" (fun _ ->
    runDotnet serverPath "build"
)

Target.create "BuildClientTests" (fun _ ->
    runDotnet clientTestsPath "build"
)

Target.create "BuildServerTests" (fun _ ->
    runDotnet serverTestsPath "build"
)

Target.create "InstallClient" (fun _ ->
    printfn "Node version:"
    run nodeTool "--version" __SOURCE_DIRECTORY__
    printfn "Yarn version:"
    run yarnTool "--version" __SOURCE_DIRECTORY__
    run yarnTool "install --frozen-lockfile" __SOURCE_DIRECTORY__
)

Target.create "BuildClient" (fun _ ->
    runDotnet clientPath "restore"
    runDotnet clientPath "fable webpack --port free -- -p --mode production"
)

Target.create "RunServerTests" (fun _ ->
    runDotnet serverTestsPath "run"
)

Target.create "RunClientTests" (fun _ ->
    Target.activateFinal "KillProcess"

    let serverProcess =
        let info = System.Diagnostics.ProcessStartInfo()
        info.FileName <- dotnetExePath
        info.WorkingDirectory <- serverPath
        info.Arguments <- " run"
        info.UseShellExecute <- false
        System.Diagnostics.Process.Start info

    System.Threading.Thread.Sleep 15000 |> ignore  // give server some time to start

    !! clientTestExecutables
    |> Expecto.run (fun p -> { p with Parallel = false } )
    |> ignore

    serverProcess.Kill()
)

// --------------------------------------------------------------------------------------
// Run the Website
let ipAddress = "localhost"
let port = 8080
let serverPort = 8085

Target.createFinal "KillProcess" (fun _ ->
    Process.killAllByName "dotnet"
    Process.killAllByName "dotnet.exe"
)

Target.create "Run" (fun _ ->
    runDotnet clientPath "restore"
    runDotnet serverTestsPath "restore"

    let unitTestsWatch = async {
        let result =
            Process.execSimple (fun info ->
                { info with
                    FileName = dotnetExePath
                    WorkingDirectory = serverTestsPath
                    Arguments = sprintf "watch msbuild /t:TestAndRun /p:DotNetHost=%s" dotnetExePath }
            ) TimeSpan.MaxValue

        if result <> 0 then failwith "Website shut down." }

    let fablewatch = async { runDotnet clientPath "fable webpack-dev-server --port free -- --mode development" }
    let openBrowser = async {
        System.Threading.Thread.Sleep(5000)
        Diagnostics.Process.Start("http://"+ ipAddress + sprintf ":%d" port) |> ignore }

    Async.Parallel [| unitTestsWatch; fablewatch; openBrowser |]
    |> Async.RunSynchronously
    |> ignore
)

Target.create "RunSSR" (fun _ ->
    runDotnet clientPath "restore"
    runDotnet serverTestsPath "restore"

    let unitTestsWatch = async {
        let result =
            Process.execSimple (fun info ->
                { info with
                    FileName = dotnetExePath
                    WorkingDirectory = serverTestsPath
                    Arguments = sprintf "watch msbuild /t:TestAndRun /p:DotNetHost=%s /p:DebugSSR=true" dotnetExePath }
            ) TimeSpan.MaxValue

        if result <> 0 then failwith "Website shut down." }

    let fablewatch = async { runDotnet clientPath "fable webpack --port free -- -w --mode development" }
    let openBrowser = async {
        System.Threading.Thread.Sleep(10000)
        Diagnostics.Process.Start("http://"+ ipAddress + sprintf ":%d" serverPort) |> ignore }

    Async.Parallel [| unitTestsWatch; fablewatch; openBrowser |]
    |> Async.RunSynchronously
    |> ignore
)

// --------------------------------------------------------------------------------------
// Release Scripts
Target.create "SetReleaseNotes" (fun _ ->
    let lines = [
            "module internal ReleaseNotes"
            ""
            (sprintf "let Version = \"%s\"" release.NugetVersion)
            ""
            (sprintf "let IsPrerelease = %b" (release.SemVer.PreRelease <> None))
            ""
            "let Notes = \"\"\""] @ Seq.toList releaseNotes @ ["\"\"\""]
    File.WriteAllLines("src/Client/ReleaseNotes.fs",lines)
)

Target.create "PrepareRelease" (fun _ ->
    Git.Branches.checkout "" false "master"
    Git.CommandHelper.directRunGitCommand "" "fetch origin" |> ignore
    Git.CommandHelper.directRunGitCommand "" "fetch origin --tags" |> ignore

    Git.Staging.stageAll ""
    Git.Commit.exec "" (sprintf "Bumping version to %O" release.NugetVersion)
    Git.Branches.pushBranch "" "origin" "master"

    let tagName = string release.NugetVersion
    Git.Branches.tag "" tagName
    Git.Branches.pushTag "" "origin" tagName

    let result =
        Process.execSimple (fun info ->
            { info with
                FileName = "docker"
                Arguments = sprintf "tag %s/%s %s/%s:%s" dockerUser dockerImageName dockerUser dockerImageName release.NugetVersion }
        ) TimeSpan.MaxValue
    if result <> 0 then failwith "Docker tag failed"
)

Target.create "BundleClient" (fun _ ->
    let result =
        Process.execSimple (fun info ->
            { info with
                FileName = dotnetExePath
                WorkingDirectory = serverPath
                Arguments = sprintf "publish -c Release -o \"%s\"" (Path.getFullName deployDir) }
            ) TimeSpan.MaxValue
    if result <> 0 then failwith "Publish failed"

    let clientDir = deployDir </> "client"
    let publicDir = clientDir </> "public"
    let jsDir = clientDir </> "js"
    let cssDir = clientDir </> "css"
    let imageDir = clientDir </> "Images"

    !! "src/Client/public/**/*.*" |> Shell.copyFiles publicDir
    !! "src/Client/js/**/*.*" |> Shell.copyFiles jsDir
    !! "src/Client/css/**/*.*" |> Shell.copyFiles cssDir
    !! "src/Client/Images/**/*.*" |> Shell.copyFiles imageDir

    "src/Client/index.html" |> Shell.copyFile clientDir
)

Target.create "CreateDockerImage" (fun _ ->
    if String.IsNullOrEmpty dockerUser then
        failwithf "docker username not given."
    if String.IsNullOrEmpty dockerImageName then
        failwithf "docker image Name not given."
    let result =
        Process.execSimple (fun info ->
            { info with
                FileName = "docker"
                UseShellExecute = false
                Arguments = sprintf "build -t %s/%s ." dockerUser dockerImageName }
        ) TimeSpan.MaxValue
    if result <> 0 then failwith "Docker build failed"
)

Target.create "TestDockerImage" (fun _ ->
    Target.activateFinal "KillProcess"
    let testImageName = "test"

    let result =
        Process.execSimple (fun info ->
            { info with
                FileName = "docker"
                Arguments = sprintf "run -d -p 127.0.0.1:8086:8085 --rm --name %s -it %s/%s" testImageName dockerUser dockerImageName }
        ) TimeSpan.MaxValue
    if result <> 0 then failwith "Docker run failed"

    System.Threading.Thread.Sleep 5000 |> ignore  // give server some time to start

    !! clientTestExecutables
    |> Expecto.run (fun p -> { p with Parallel = false } )
    |> ignore

    let result =
        Process.execSimple (fun info ->
            { info with
                FileName = "docker"
                Arguments = sprintf "stop %s" testImageName }
        ) TimeSpan.MaxValue
    if result <> 0 then failwith "Docker stop failed"
)

Target.create "Deploy" (fun _ ->
    let result =
        Process.execSimple (fun info ->
            { info with
                FileName = "docker"
                WorkingDirectory = deployDir
                Arguments = sprintf "login %s --username \"%s\" --password \"%s\"" dockerLoginServer dockerUser dockerPassword }
        ) TimeSpan.MaxValue
    if result <> 0 then failwith "Docker login failed"

    let result =
        Process.execSimple (fun info ->
            { info with
                FileName = "docker"
                WorkingDirectory = deployDir
                Arguments = sprintf "push %s/%s" dockerUser dockerImageName }
        ) TimeSpan.MaxValue
    if result <> 0 then failwith "Docker push failed"
)

// -------------------------------------------------------------------------------------
open Fake.Core.TargetOperators
Target.create "Build" ignore
Target.create "All" ignore

"Clean"
  ==> "InstallDotNetCore"
  ==> "InstallClient"
  ==> "SetReleaseNotes"
  ==> "BuildServer"
  ==> "BuildClient"
  ==> "BuildServerTests"
  ==> "RunServerTests"
  ==> "BuildClientTests"
  ==> "RunClientTests"
  ==> "BundleClient"
  ==> "All"
  ==> "CreateDockerImage"
  ==> "TestDockerImage"
  ==> "PrepareRelease"
  ==> "Deploy"

"BuildClient"
  ==> "Build"

"InstallClient"
  ==> "Run"

"InstallClient"
  ==> "RunSSR"

Target.runOrDefault "All"
