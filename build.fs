open Fake
open Fake.Core
open Fake.Core.Operators
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.Tools
open Fake.Tools.Git
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open System
open System.IO

let clientPath = Path.getFullName "./src/Client"
let serverPath = Path.getFullName "./src/Server/"
let serverProj = serverPath </> "Server.fsproj"
let serverTestsPath = Path.getFullName "./test/ServerTests"
let clientTestsPath = Path.getFullName "./test/UITests"

let mutable dotnetExePath = "dotnet"

let deployDir = "./deploy"

let dockerUser = Environment.environVar "DockerUser"
let dockerPassword = Environment.environVar "DockerPassword"
let dockerLoginServer = Environment.environVar "DockerLoginServer"
let dockerImageName = Environment.environVar "DockerImageName"

// Pattern specifying assemblies to be tested using Expecto
let clientTestExecutables = "test/UITests/**/bin/**/*Tests*.exe"

let run cmd args dir =
    let result =
        CreateProcess.fromRawCommandLine cmd args
        |> (fun proc -> if not (String.IsNullOrWhiteSpace dir) then CreateProcess.withWorkingDirectory dir proc else proc)
        |> Proc.run
    if result.ExitCode <> 0 then
        failwithf "Error while running '%s' with args: %s" cmd args


let runDotnet workingDir cmd args =
    let result = DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd args
    if result.ExitCode <> 0 then
        failwithf "dotnet %s failed" args

let platformTool tool winTool =
    let tool = if Environment.isUnix then tool else winTool
    tool
    |> ProcessUtils.tryFindFileOnPath
    |> function Some t -> t | _ -> failwithf "%s not found" tool


let runFunc workingDir args =
    let result =
        Command.RawCommand (
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),@"npm\func.cmd"),
            Arguments.OfWindowsCommandLine args)
        |> CreateProcess.fromCommand
        |> CreateProcess.withWorkingDirectory workingDir
        |> Proc.run
    if result.ExitCode <> 0 then failwithf "func %s failed" args

let nodeTool = platformTool "node" "node.exe"
let yarnTool = platformTool "yarn" "yarn.cmd"
let dockerTool = platformTool "docker" "docker.exe"

// Read additional information from the release notes document
let releaseNotes = File.ReadAllLines "RELEASE_NOTES.md"

let releaseNotesData =
    releaseNotes
    |> ReleaseNotes.parseAll

let release = List.head releaseNotesData

// --------------------------------------------------------------------------------------
// Clean build results

let Clean _ =
    !!"src/**/bin"
    ++ "test/**/bin"
    |> Shell.cleanDirs

    !! "src/**/obj/*.nuspec"
    ++ "test/**/obj/*.nuspec"
    |> File.deleteAll

    !! "./**/temp/db/*.json"
    |> File.deleteAll

    Shell.cleanDirs ["bin"; "temp"; "docs/output"; deployDir; Path.Combine(clientPath,"public/bundle")]


let InstallDotNetCore _ =
    // let options = DotNet.install (fun o -> { o with Version = DotNet.CliVersion.GlobalJson }) (DotNet.Options.Create ())
    // let fi = FileInfo dotnetExePath
    // let SEPARATOR = if Environment.isWindows then ";" else ":"
    // Environment.SetEnvironmentVariable(
    //     "PATH",
    //     fi.Directory.FullName + SEPARATOR + System.Environment.GetEnvironmentVariable "PATH",
    //     EnvironmentVariableTarget.Process)
    ()

// --------------------------------------------------------------------------------------
// Build library & test project


let BuildServer _ =
    runDotnet serverPath "build" ""

let NPMInstall _ =
    printfn "Node version:"
    run nodeTool "--version" __SOURCE_DIRECTORY__
    printfn "Yarn version:"
    run yarnTool "--version" __SOURCE_DIRECTORY__
    run yarnTool "install --frozen-lockfile" __SOURCE_DIRECTORY__

let BuildClient _ =
    // run yarnTool "webpack --config src/Client/webpack.config.js --mode production" clientPath
    DotNet.exec (DotNet.Options.withWorkingDirectory ("src" </> "Client")) "fable" ".\ --run webpack --mode production" |> ignore
    
let RunServerTests _ =
    runDotnet serverTestsPath "run" ""

// FinalTarget
let KillProcess _ =
    Process.killAllByName "dotnet"
    Process.killAllByName "dotnet.exe"
    Process.killAllByName "docker-proxy"

let RunUITest _ =
    Target.activateFinal "KillProcess"

    let serverProcess =
        let info = System.Diagnostics.ProcessStartInfo()
        info.FileName <- dotnetExePath
        info.WorkingDirectory <- serverPath
        info.Arguments <- " run"
        info.UseShellExecute <- false
        System.Diagnostics.Process.Start info

    System.Threading.Thread.Sleep 15000 |> ignore  // give server some time to start

    runDotnet clientTestsPath "build" ""
    
    !! clientTestExecutables
    |> fun xs -> if Seq.isEmpty xs then failwith "no UI tests found" else xs
    |> Expecto.run (fun p -> { p with Parallel = false } )

    serverProcess.Kill()


// --------------------------------------------------------------------------------------
// Development mode

let host = "localhost"
let port = 8089
let serverPort = 8085


let Run _ =
    let serverWatch = async {
        let result = DotNet.exec (DotNet.Options.withWorkingDirectory serverPath) "watch" "run"

        if result.ExitCode <> 0 then failwith "Website shut down."
    }

    let unitTestsWatch = async {
        let result = DotNet.exec (DotNet.Options.withWorkingDirectory serverTestsPath) "watch" "run"

        if result.ExitCode <> 0 then failwith "Website shut down."
    }

    let fablewatch = async {
        run yarnTool "webpack-dev-server --config src/Client/webpack.config.js" clientPath
    }

    let openBrowser = async {
        System.Threading.Thread.Sleep(5000)
        Diagnostics.Process.Start("http://"+ host + sprintf ":%d" port) |> ignore
    }

    Async.Parallel [| unitTestsWatch; fablewatch; serverWatch; openBrowser |]
    |> Async.RunSynchronously
    |> ignore

// --------------------------------------------------------------------------------------
// Release Scripts


let SetReleaseNotes _ =
    let lines = [
            "module internal ReleaseNotes"
            ""
            (sprintf "let Version = \"%s\"" release.NugetVersion)
            ""
            (sprintf "let IsPrerelease = %b" (release.SemVer.PreRelease <> None))
            ""
            "let Notes = \"\"\""] @ Array.toList releaseNotes @ ["\"\"\""]
    File.WriteAllLines("src/Client/ReleaseNotes.fs",lines)

let PrepareRelease _ =
    Git.Branches.checkout "" false "master"
    Git.CommandHelper.directRunGitCommand "" "fetch origin" |> ignore
    Git.CommandHelper.directRunGitCommand "" "fetch origin --tags" |> ignore

    Staging.stageAll ""
    Git.Commit.exec "" (sprintf "Bumping version to %O" release.NugetVersion)
    Git.Branches.pushBranch "" "origin" "master"

    let tagName = string release.NugetVersion
    Git.Branches.tag "" tagName
    Git.Branches.pushTag "" "origin" tagName

    run dockerTool (sprintf "tag %s/%s %s/%s:%s" dockerUser dockerImageName dockerUser dockerImageName release.NugetVersion) ""

let BundleClient _ =
    DotNet.publish (fun po ->
        { po.WithCommon(DotNet.Options.withWorkingDirectory serverPath) with
            Configuration = DotNet.BuildConfiguration.Release
            OutputPath = Some (Path.getFullName deployDir) }) serverProj

    let clientDir = deployDir </> "client"
    let publicDir = clientDir </> "public"
    let jsDir = clientDir </> "js"
    let cssDir = clientDir </> "css"
    let imageDir = clientDir </> "Images"

    !! "src/Client/public/**/*.*" |> Shell.copyFiles publicDir
    !! "src/Client/js/**/*.*" |> Shell.copyFiles jsDir
    !! "src/Client/css/**/*.*" |> Shell.copyFiles cssDir
    !! "src/Client/Images/**/*.*" |> Shell.copyFiles imageDir

let CreateDockerImage _ =
    !! "./**/temp/db/*.json"
    |> File.deleteAll

    if String.IsNullOrEmpty dockerUser then
        failwithf "docker username not given."
    if String.IsNullOrEmpty dockerImageName then
        failwithf "docker image Name not given."
    run dockerTool (sprintf "build -t %s/%s ." dockerUser dockerImageName) ""

let TestDockerImage _ =
    Target.activateFinal "KillProcess"
    let testImageName = "test"

    run dockerTool (sprintf "run -d -p 127.0.0.1:8085:8085 --rm --name %s -it %s/%s" testImageName dockerUser dockerImageName) ""

    System.Threading.Thread.Sleep 5000 |> ignore  // give server some time to start

    runDotnet clientTestsPath "build" ""
    
    !! clientTestExecutables
    |> fun xs -> if Seq.isEmpty xs then failwith "no UI tests found" else xs
    |> Expecto.run (fun p -> { p with Parallel = false } )

    run dockerTool (sprintf "stop %s" testImageName) ""

let Deploy _ =
    run dockerTool (sprintf "login %s --username \"%s\" --password \"%s\"" dockerLoginServer dockerUser dockerPassword) deployDir
    run dockerTool (sprintf "push %s/%s/%s:%s" dockerLoginServer dockerUser dockerImageName release.NugetVersion) deployDir
    run dockerTool (sprintf "push %s/%s/%s:latest" dockerLoginServer dockerUser dockerImageName) deployDir

// -------------------------------------------------------------------------------------


let getFunctionApp projectName =
    match projectName with
    | "RecurringJobs.fsproj" ->
        "bookstoretasks"
    | _ ->
        "bookstoretasks"

let functionsPath = "./src/AzureFunctions/" |> Path.getFullName

let azureFunctionsfilter = Environment.environVarOrDefault "FunctionApp" ""

let functionApps =
    [ "bookstoretasks" ]

let PublishAzureFunctions _ =
    let deployDir = deployDir + "/functions"
    Shell.cleanDir deployDir

    for functionApp in functionApps do
        if azureFunctionsfilter <> "" && functionApp <> azureFunctionsfilter then () else

        let deployDir = deployDir + "/" + functionApp
        Shell.cleanDir deployDir

        !! (functionsPath + "/*.json")
        |> Shell.copyFiles deployDir

        let functionsToDeploy =
            !! (functionsPath + "/**/*.fsproj")
            |> Seq.filter (fun proj ->
                let fi = FileInfo proj
                getFunctionApp fi.Name = functionApp)
            |> Seq.toList

        let targetBinDir = deployDir + "/bin"
        Shell.cleanDir targetBinDir

        functionsToDeploy
        |> Seq.iter (fun proj ->
            let fi = FileInfo proj
            runDotnet fi.Directory.FullName (sprintf "publish -c Release %s" fi.Name) ""
            let targetPath = deployDir + "/" + fi.Name.Replace(fi.Extension,"") + "/"
            Shell.cleanDir targetPath
            Trace.logf "  Target: %s" targetPath

            let mutable found = false
            let allFiles x = found <- true; true

            let publishDir = Path.Combine(fi.Directory.FullName,"bin/Release/netstandard2.0/publish")
            let binDir = Path.Combine(publishDir,"bin")
            if Directory.Exists binDir then
                Shell.copyDir targetBinDir binDir allFiles
                !! (publishDir + "/*.deps.json")
                |> Shell.copyFiles targetBinDir
            else
                Shell.copyDir targetBinDir publishDir allFiles

            let functionJson = publishDir + "/**/function.json"
            !! functionJson
            |> Seq.iter (fun fileName ->
                let fi = FileInfo fileName
                let target = Path.Combine(targetPath,"..",fi.Directory.Name)
                Shell.cleanDir target
                fileName |> Shell.copyFile target)


            if not found then failwithf "No files found for function %s" fi.Name

            !! (fi.Directory.FullName + "/function.json")
            |> Shell.copyFiles targetPath
        )

        runFunc deployDir ("azure functionapp publish " + functionApp)

// -------------------------------------------------------------------------------------


// Target "Build" DoNothing
// Target "All" DoNothing

let Build _ = ()
let All _ = ()

// FS0020: The result of this expression has type 'string' and is explicitly ignored. ...
#nowarn "0020"

let initTargets () =
    Target.create (nameof(Clean)) Clean
    Target.create (nameof(InstallDotNetCore)) InstallDotNetCore
    Target.create (nameof(BuildServer)) BuildServer
    Target.create (nameof(NPMInstall)) NPMInstall
    Target.create (nameof(BuildClient)) BuildClient
    Target.create (nameof(RunServerTests)) RunServerTests
    Target.createFinal (nameof(KillProcess)) KillProcess
    Target.create (nameof(RunUITest)) RunUITest
    Target.create (nameof(Run)) Run
    Target.create (nameof(SetReleaseNotes)) SetReleaseNotes
    Target.create (nameof(PrepareRelease)) PrepareRelease
    Target.create (nameof(BundleClient)) BundleClient
    Target.create (nameof(CreateDockerImage)) CreateDockerImage
    Target.create (nameof(TestDockerImage)) TestDockerImage
    Target.create (nameof(Deploy)) Deploy
    Target.create (nameof(PublishAzureFunctions)) PublishAzureFunctions
    Target.create (nameof(Build)) Build
    Target.create (nameof(All)) All
    
    nameof(Clean)
      ==> nameof(InstallDotNetCore)
      ==> nameof(NPMInstall)
      ==> nameof(SetReleaseNotes)
      ==> nameof(BuildServer)
      ==> nameof(BuildClient)
      ==> nameof(RunServerTests)
      ==> nameof(RunUITest)
      ==> nameof(BundleClient)
      ==> nameof(All)
      ==> nameof(CreateDockerImage)
      ==> nameof(TestDockerImage)
      ==> nameof(PrepareRelease)
      ==> nameof(Deploy)

    nameof(BuildClient)
      ==> nameof(Build)

    nameof(NPMInstall)
      ==> nameof(Run)

    nameof(InstallDotNetCore)
      ==> nameof(PublishAzureFunctions)

[<EntryPoint>]
let main argv =
    argv
    |> Array.toList
    |> Context.FakeExecutionContext.Create false "build.fs"
    |> Context.RuntimeContext.Fake
    |> Context.setExecutionContext
    initTargets ()
    Target.runOrDefaultWithArguments "All"

    0
