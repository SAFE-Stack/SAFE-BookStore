#r @"packages/build/FAKE/tools/FakeLib.dll"

open Fake
open Fake.Git
open Fake.ReleaseNotesHelper
open System
open System.IO
open Fake.Testing.Expecto


let clientPath = FullName "./src/Client"
let serverPath = FullName "./src/Server/"
let serverTestsPath = FullName "./test/ServerTests"
let clientTestsPath = FullName "./test/UITests"

let dotnetcliVersion = DotNetCli.GetDotNetSDKVersionFromGlobalJson()
let mutable dotnetExePath = "dotnet"

let deployDir = "./deploy"

let clientTestExecutables = "test/UITests/**/bin/**/*Tests*.exe"


let dockerUser = getBuildParam "DockerUser"
let dockerPassword = getBuildParam "DockerPassword"
let dockerLoginServer = getBuildParam "DockerLoginServer"
let dockerImageName = getBuildParam "DockerImageName"




let run cmd args dir =
    let success =
        execProcess (fun info ->
            info.FileName <- cmd
            if not (String.IsNullOrWhiteSpace dir) then
                info.WorkingDirectory <- dir
            info.Arguments <- args
        ) System.TimeSpan.MaxValue 
    if not success then
        failwithf "Error while running '%s' with args: %s" cmd args


let runDotnet workingDir args =
    let result =
        ExecProcess (fun info ->
            info.FileName <- dotnetExePath
            info.WorkingDirectory <- workingDir
            info.Arguments <- args) TimeSpan.MaxValue
    if result <> 0 then 
        failwithf "dotnet %s failed" args

let platformTool tool winTool =
    let tool = if isUnix then tool else winTool
    tool
    |> ProcessHelper.tryFindFileOnPath
    |> function Some t -> t | _ -> failwithf "%s not found" tool


let runFunc workingDir args =
    let result =
        ExecProcess (fun info ->
            info.FileName <- Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),@"npm\func.cmd")
            info.WorkingDirectory <- workingDir
            info.Arguments <- args) TimeSpan.MaxValue
    if result <> 0 then failwithf "func %s failed" args

let nodeTool = platformTool "node" "node.exe"
let yarnTool = platformTool "yarn" "yarn.cmd"

do if not isWindows then
    // We have to set the FrameworkPathOverride so that dotnet sdk invocations know
    // where to look for full-framework base class libraries
    let mono = platformTool "mono" "mono"
    let frameworkPath = IO.Path.GetDirectoryName(mono) </> ".." </> "lib" </> "mono" </> "4.5"
    setEnvironVar "FrameworkPathOverride" frameworkPath


// Read additional information from the release notes document
let releaseNotes = File.ReadAllLines "RELEASE_NOTES.md"

let releaseNotesData =
    releaseNotes
    |> parseAllReleaseNotes

let release = List.head releaseNotesData

// --------------------------------------------------------------------------------------
// Clean build results

Target "Clean" (fun _ ->
    !!"src/**/bin"
    ++ "test/**/bin"
    |> CleanDirs

    !! "src/**/obj/*.nuspec"
    ++ "test/**/obj/*.nuspec"
    |> DeleteFiles

    CleanDirs ["bin"; "temp"; "docs/output"; deployDir; Path.Combine(clientPath,"public/bundle")]
)

Target "InstallDotNetCore" (fun _ ->
    dotnetExePath <- DotNetCli.InstallDotNetSDK dotnetcliVersion
    let fi = FileInfo dotnetExePath
    let SEPARATOR = if isWindows then ";" else ":"
    Environment.SetEnvironmentVariable(
        "PATH",
        fi.Directory.FullName + SEPARATOR + System.Environment.GetEnvironmentVariable "PATH",
        EnvironmentVariableTarget.Process)
)

// --------------------------------------------------------------------------------------
// Build library & test project


Target "BuildServer" (fun _ ->
    runDotnet serverPath "build"
)

Target "BuildTests" (fun _ ->
    runDotnet clientTestsPath "build"
    runDotnet serverTestsPath "build"
)

Target "NPMInstall" (fun _ ->
    printfn "Node version:"
    run nodeTool "--version" __SOURCE_DIRECTORY__
    printfn "Yarn version:"
    run yarnTool "--version" __SOURCE_DIRECTORY__
    run yarnTool "install --frozen-lockfile" __SOURCE_DIRECTORY__
)

Target "BuildClient" (fun _ ->
    runDotnet clientPath "restore"
    runDotnet clientPath "fable webpack-cli -- --config src/Client/webpack.config.js -p"
)

Target "RunServerTests" (fun _ ->
    runDotnet serverTestsPath "run"
)

FinalTarget "KillProcess" (fun _ ->
    killProcess "dotnet"
    killProcess "dotnet.exe"
)

Target "RunUITest" (fun _ ->
    ActivateFinalTarget "KillProcess"

    let serverProcess =
        let info = System.Diagnostics.ProcessStartInfo()
        info.FileName <- dotnetExePath
        info.WorkingDirectory <- serverPath
        info.Arguments <- " run"
        info.UseShellExecute <- false
        System.Diagnostics.Process.Start info

    System.Threading.Thread.Sleep 15000 |> ignore  // give server some time to start

    !! clientTestExecutables
    |> Expecto (fun p -> { p with Parallel = false } )
    |> ignore

    serverProcess.Kill()
)


// --------------------------------------------------------------------------------------
// Development mode

let host = "localhost"
let port = 8080
let serverPort = 8085


Target "Run" (fun _ ->
    runDotnet serverTestsPath "restore"
    runDotnet clientPath "restore"

    let unitTestsWatch = async {
        let result =
            ExecProcess (fun info ->
                info.FileName <- dotnetExePath
                info.WorkingDirectory <- serverTestsPath
                info.Arguments <- sprintf "watch msbuild /t:TestAndRun /p:DotNetHost=%s" dotnetExePath) TimeSpan.MaxValue

        if result <> 0 then failwith "Website shut down."
    }

    let fablewatch = async { 
        runDotnet clientPath "fable webpack-dev-server -- --config src/Client/webpack.config.js"
    }

    let openBrowser = async {
        System.Threading.Thread.Sleep(5000)
        Diagnostics.Process.Start("http://"+ host + sprintf ":%d" port) |> ignore
    }

    Async.Parallel [| unitTestsWatch; fablewatch; openBrowser |]
    |> Async.RunSynchronously
    |> ignore
)


Target "RunSSR" (fun _ ->
    runDotnet serverTestsPath "restore"
    runDotnet clientPath "restore"

    let unitTestsWatch = async {
        let result =
            ExecProcess (fun info ->
                info.FileName <- dotnetExePath
                info.WorkingDirectory <- serverTestsPath
                info.Arguments <- sprintf "watch msbuild /t:TestAndRun /p:DotNetHost=%s /p:DebugSSR=true" dotnetExePath) TimeSpan.MaxValue

        if result <> 0 then failwith "Website shut down."
    }

    let fablewatch = async { 
        runDotnet clientPath "fable webpack-cli -- --config src/Client/webpack.config.js -w"
    }

    let openBrowser = async {
        System.Threading.Thread.Sleep(10000)
        Diagnostics.Process.Start("http://"+ host + sprintf ":%d" serverPort) |> ignore
    }

    Async.Parallel [| unitTestsWatch; fablewatch; openBrowser |]
    |> Async.RunSynchronously
    |> ignore
)


// --------------------------------------------------------------------------------------
// Release Scripts


Target "SetReleaseNotes" (fun _ ->
    let lines = [
            "module internal ReleaseNotes"
            ""
            (sprintf "let Version = \"%s\"" release.NugetVersion)
            ""
            (sprintf "let IsPrerelease = %b" (release.SemVer.PreRelease <> None))
            ""
            "let Notes = \"\"\""] @ Array.toList releaseNotes @ ["\"\"\""]
    File.WriteAllLines("src/Client/ReleaseNotes.fs",lines)
)

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

Target "BundleClient" (fun _ ->
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
    !! "src/Client/Images/**/*.*" |> CopyFiles imageDir

    "src/Client/index.html" |> CopyFile clientDir
)

Target "CreateDockerImage" (fun _ ->
    if String.IsNullOrEmpty dockerUser then
        failwithf "docker username not given."
    if String.IsNullOrEmpty dockerImageName then
        failwithf "docker image Name not given."
    let result =
        ExecProcess (fun info ->
            info.FileName <- "docker"
            info.UseShellExecute <- false
            info.Arguments <- sprintf "build -t %s/%s ." dockerUser dockerImageName) TimeSpan.MaxValue
    if result <> 0 then failwith "Docker build failed"
)

Target "TestDockerImage" (fun _ ->
    ActivateFinalTarget "KillProcess"
    let testImageName = "test"

    let result =
        ExecProcess (fun info ->
            info.FileName <- "docker"
            info.Arguments <- sprintf "run -d -p 127.0.0.1:8086:8085 --rm --name %s -it %s/%s" testImageName dockerUser dockerImageName) TimeSpan.MaxValue
    if result <> 0 then failwith "Docker run failed"

    System.Threading.Thread.Sleep 5000 |> ignore  // give server some time to start

    !! clientTestExecutables
    |> Expecto (fun p -> { p with Parallel = false } )
    |> ignore

    let result =
        ExecProcess (fun info ->
            info.FileName <- "docker"
            info.Arguments <- sprintf "stop %s" testImageName) TimeSpan.MaxValue
    if result <> 0 then failwith "Docker stop failed"
)

Target "Deploy" (fun _ ->
    let result =
        ExecProcess (fun info ->
            info.FileName <- "docker"
            info.WorkingDirectory <- deployDir
            info.Arguments <- sprintf "login %s --username \"%s\" --password \"%s\"" dockerLoginServer dockerUser dockerPassword) TimeSpan.MaxValue
    if result <> 0 then failwith "Docker login failed"

    let result =
        ExecProcess (fun info ->
            info.FileName <- "docker"
            info.WorkingDirectory <- deployDir
            info.Arguments <- sprintf "push %s/%s" dockerUser dockerImageName) TimeSpan.MaxValue
    if result <> 0 then failwith "Docker push failed"
)

// -------------------------------------------------------------------------------------


let getFunctionApp projectName =
    match projectName with 
    | "RecurringJobs.fsproj" ->
        "bookstoretasks"
    | _ ->
        "bookstoretasks"

let functionsPath = "./src/AzureFunctions/" |> FullName

let azureFunctionsfilter = getBuildParamOrDefault "FunctionApp" ""

let functionApps = 
    [ "bookstoretasks" ]

Target "PublishAzureFunctions" (fun _ ->
    let deployDir = deployDir + "/functions"
    CleanDir deployDir

    for functionApp in functionApps do
        if azureFunctionsfilter <> "" && functionApp <> azureFunctionsfilter then () else

        let deployDir = deployDir + "/" + functionApp
        CleanDir deployDir
        
        !! (functionsPath + "/*.json")
        |> CopyFiles deployDir

        let functionsToDeploy = 
            !! (functionsPath + "/**/*.fsproj")
            |> Seq.filter (fun proj ->
                let fi = FileInfo proj
                getFunctionApp fi.Name = functionApp)
            |> Seq.toList
        
        let targetBinDir = deployDir + "/bin"
        CleanDir targetBinDir

        functionsToDeploy
        |> Seq.iter (fun proj -> 
            let fi = FileInfo proj
            runDotnet fi.Directory.FullName (sprintf "publish -c Release %s" fi.Name)
            let targetPath = deployDir + "/" + fi.Name.Replace(fi.Extension,"") + "/"
            CleanDir targetPath
            tracefn "  Target: %s" targetPath
            
            let mutable found = false
            let allFiles x = found <- true; allFiles x

            let publishDir = Path.Combine(fi.Directory.FullName,"bin/Release/netstandard2.0/publish")
            let binDir = Path.Combine(publishDir,"bin")
            if Directory.Exists binDir then
                CopyDir targetBinDir binDir allFiles
                !! (publishDir + "/*.deps.json")
                |> CopyFiles targetBinDir
            else
                CopyDir targetBinDir publishDir allFiles

            let functionJson = publishDir + "/**/function.json"
            !! functionJson
            |> Seq.iter (fun fileName ->
                let fi = FileInfo fileName
                let target = Path.Combine(targetPath,"..",fi.Directory.Name) 
                CleanDir target
                fileName |> CopyFile target)


            if not found then failwithf "No files found for function %s" fi.Name

            !! (fi.Directory.FullName + "/function.json")
            |> CopyFiles targetPath
        )

        runFunc deployDir ("azure functionapp publish " + functionApp)
)

// -------------------------------------------------------------------------------------


Target "Build" DoNothing
Target "All" DoNothing

"Clean"
  ==> "InstallDotNetCore"
  ==> "NPMInstall"
  ==> "SetReleaseNotes"
  ==> "BuildServer"
  ==> "BuildClient"
  ==> "BuildTests"
  ==> "RunServerTests"
  ==> "RunUITest"
  ==> "BundleClient"
  ==> "All"
  ==> "CreateDockerImage"
  ==> "TestDockerImage"
  ==> "PrepareRelease"
  ==> "Deploy"

"BuildClient"
  ==> "Build"

"NPMInstall"
  ==> "Run"

"NPMInstall"
  ==> "RunSSR"

"InstallDotNetCore"
  ==> "PublishAzureFunctions"

RunTargetOrDefault "All"
