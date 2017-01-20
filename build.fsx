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

// --------------------------------------------------------------------------------------
// START TODO: Provide project-specific details below
// --------------------------------------------------------------------------------------

// Information about the project are used
//  - for version and project name in generated AssemblyInfo file
//  - to run tests and to publish documentation on GitHub gh-pages
//  - for documentation, you also need to edit info in "docs/tools/generate.fsx"

// The name of the project
let project = "msu.SmartMeterHome"

// Short summary of the project
// (used as description in AssemblyInfo and as a short summary for NuGet package)
let summary = "Ein Portal zur Abfrage der SmartMeter-Daten."

// Longer description of the project
let description = "Ein Portal zur Abfrage der SmartMeter-Daten."

let authors = [ "msu" ]

// Tags for your project (for NuGet package)
let tags = "SmartMeter"

// Default target configuration
let configuration = "Release"

// Pattern specifying assemblies to be tested using NUnit
let testAssemblies = "tests/**/bin" </> configuration </> "*Tests*.dll"

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted
let gitOwner = "forki"
let gitHome = sprintf "%s/%s" "https://github.com" gitOwner

// The name of the project on GitHub
let gitName = "msu.SmartMeterHome"

// The url for the raw files hosted
let gitRaw = environVarOrDefault "gitRaw" "https://raw.githubusercontent.com/forki"

let clientPath = "./src/msu.SmartMeterHome.Client" |> FullName

let serverPath = "./src/msu.SmartMeterHome/" |> FullName

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

// Copies binaries from default VS location to expected bin folder
// But keeps a subdirectory structure for each project in the
// src folder to support multiple project outputs
Target "CopyBinaries" (fun _ ->
    !! "src/**/*.??proj"
    -- "src/**/*.shproj"
    |>  Seq.map (fun f -> ((System.IO.Path.GetDirectoryName f) </> "bin" </> configuration, "bin" </> (System.IO.Path.GetFileNameWithoutExtension f)))
    |>  Seq.iter (fun (fromDir, toDir) -> CopyDir toDir fromDir (fun _ -> true))
)

// --------------------------------------------------------------------------------------
// Clean build results

let vsProjProps = 
#if MONO
    [ ("DefineConstants","MONO"); ("Configuration", configuration) ]
#else
    [ ("Configuration", configuration); ("Platform", "Any CPU") ]
#endif

Target "Clean" (fun _ ->
    killProcess "msu.SmartMeterHome"
    
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
// Generate the documentation


let fakePath = "packages" </> "build" </> "FAKE" </> "tools" </> "FAKE.exe"
let fakeStartInfo script workingDirectory args fsiargs environmentVars =
    (fun (info: System.Diagnostics.ProcessStartInfo) ->
        info.FileName <- System.IO.Path.GetFullPath fakePath
        info.Arguments <- sprintf "%s --fsiargs -d:FAKE %s \"%s\"" args fsiargs script
        info.WorkingDirectory <- workingDirectory
        let setVar k v =
            info.EnvironmentVariables.[k] <- v
        for (k, v) in environmentVars do
            setVar k v
        setVar "MSBuild" msBuildExe
        setVar "GIT" Git.CommandHelper.gitPath
        setVar "FSI" fsiPath)

/// Run the given buildscript with FAKE.exe
let executeFAKEWithOutput workingDirectory script fsiargs envArgs =
    let exitCode =
        ExecProcessWithLambdas
            (fakeStartInfo script workingDirectory "" fsiargs envArgs)
            TimeSpan.MaxValue false ignore ignore
    System.Threading.Thread.Sleep 1000
    exitCode

// Documentation
let buildDocumentationTarget fsiargs target =
    trace (sprintf "Building documentation (%s), this could take some time, please wait..." target)
    let exit = executeFAKEWithOutput "docs/tools" "generate.fsx" fsiargs ["target", target]
    if exit <> 0 then
        failwith "generating reference documentation failed"
    ()

Target "GenerateReferenceDocs" (fun _ ->
    buildDocumentationTarget "-d:RELEASE -d:REFERENCE" "Default"
)

let generateHelp' fail debug =
    let args =
        if debug then "--define:HELP"
        else "--define:RELEASE --define:HELP"
    try
        buildDocumentationTarget args "Default"
        traceImportant "Help generated"
    with
    | e when not fail ->
        traceImportant "generating help documentation failed"

let generateHelp fail =
    generateHelp' fail false

Target "GenerateHelp" (fun _ ->
    DeleteFile "docs/content/release-notes.md"
    CopyFile "docs/content/" "RELEASE_NOTES.md"
    Rename "docs/content/release-notes.md" "docs/content/RELEASE_NOTES.md"

    DeleteFile "docs/content/license.md"
    CopyFile "docs/content/" "LICENSE.txt"
    Rename "docs/content/license.md" "docs/content/LICENSE.txt"

    generateHelp true
)

Target "GenerateHelpDebug" (fun _ ->
    DeleteFile "docs/content/release-notes.md"
    CopyFile "docs/content/" "RELEASE_NOTES.md"
    Rename "docs/content/release-notes.md" "docs/content/RELEASE_NOTES.md"

    DeleteFile "docs/content/license.md"
    CopyFile "docs/content/" "LICENSE.txt"
    Rename "docs/content/license.md" "docs/content/LICENSE.txt"

    generateHelp' true true
)

Target "KeepRunning" (fun _ ->
    use watcher = !! "docs/content/**/*.*" |> WatchChanges (fun changes ->
         generateHelp' true true
    )

    traceImportant "Waiting for help edits. Press any key to stop."

    System.Console.ReadKey() |> ignore

    watcher.Dispose()
)

Target "GenerateDocs" DoNothing

let createIndexFsx lang =
    let content = """(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use
// it to define helpers that you do not want to show in the documentation.
#I "../../../bin"

(**
F# Project Scaffold ({0})
=========================
*)
"""
    let targetDir = "docs/content" </> lang
    let targetFile = targetDir </> "index.fsx"
    ensureDirectory targetDir
    System.IO.File.WriteAllText(targetFile, System.String.Format(content, lang))

Target "AddLangDocs" (fun _ ->
    let args = System.Environment.GetCommandLineArgs()
    if args.Length < 4 then
        failwith "Language not specified."

    args.[3..]
    |> Seq.iter (fun lang ->
        if lang.Length <> 2 && lang.Length <> 3 then
            failwithf "Language must be 2 or 3 characters (ex. 'de', 'fr', 'ja', 'gsw', etc.): %s" lang

        let templateFileName = "template.cshtml"
        let templateDir = "docs/tools/templates"
        let langTemplateDir = templateDir </> lang
        let langTemplateFileName = langTemplateDir </> templateFileName

        if System.IO.File.Exists(langTemplateFileName) then
            failwithf "Documents for specified language '%s' have already been added." lang

        ensureDirectory langTemplateDir
        Copy langTemplateDir [ templateDir </> templateFileName ]

        createIndexFsx lang)
)

// --------------------------------------------------------------------------------------
// Release Scripts

Target "ReleaseDocs" (fun _ ->
    let tempDocsDir = "temp/gh-pages"
    CleanDir tempDocsDir
    Repository.cloneSingleBranch "" (gitHome + "/" + gitName + ".git") "gh-pages" tempDocsDir

    CopyRecursive "docs/output" tempDocsDir true |> tracefn "%A"
    StageAll tempDocsDir
    Git.Commit.Commit tempDocsDir (sprintf "Update generated documentation for version %s" release.NugetVersion)
    Branches.push tempDocsDir
)

#load "paket-files/build/fsharp/FAKE/modules/Octokit/Octokit.fsx"
open Octokit

Target "Release" (fun _ ->
    let user =
        match getBuildParam "github-user" with
        | s when not (String.IsNullOrWhiteSpace s) -> s
        | _ -> getUserInput "Username: "
    let pw =
        match getBuildParam "github-pw" with
        | s when not (String.IsNullOrWhiteSpace s) -> s
        | _ -> getUserPassword "Password: "
    let remote =
        Git.CommandHelper.getGitResult "" "remote -v"
        |> Seq.filter (fun (s: string) -> s.EndsWith("(push)"))
        |> Seq.tryFind (fun (s: string) -> s.Contains(gitOwner + "/" + gitName))
        |> function None -> gitHome + "/" + gitName | Some (s: string) -> s.Split().[0]

    StageAll ""
    Git.Commit.Commit "" (sprintf "Bump version to %s" release.NugetVersion)
    Branches.pushBranch "" remote (Information.getBranchName "")

    Branches.tag "" release.NugetVersion
    Branches.pushTag "" remote release.NugetVersion

    // release on github
    createClient user pw
    |> createDraft gitOwner gitName release.NugetVersion (release.SemVer.PreRelease <> None) release.Notes
    // TODO: |> uploadFile "PATH_TO_FILE"
    |> releaseDraft
    |> Async.RunSynchronously
)

Target "BuildPackage" DoNothing

// --------------------------------------------------------------------------------------
// Run all targets by default. Invoke 'build <Target>' to override

Target "All" DoNothing


"Clean"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "BuildClient"
  ==> "Run"
  ==> "CopyBinaries"
  ==> "GenerateReferenceDocs"
  ==> "GenerateDocs"
  ==> "All"
  =?> ("ReleaseDocs",isLocalBuild)

"GenerateHelp"
  ==> "GenerateReferenceDocs"
  ==> "GenerateDocs"

"GenerateHelpDebug"
  ==> "KeepRunning"

"ReleaseDocs"
  ==> "Release"

RunTargetOrDefault "All"
