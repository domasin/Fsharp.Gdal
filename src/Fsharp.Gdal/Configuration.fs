/// Configuration of GDAL/OGR library's environment variabiles
module FSharp.Gdal.Configuration

(*** hide ***)
open System
open System.IO
open System.Reflection

open OSGeo.GDAL
open OSGeo.OGR
open OSGeo.OSR

/// Stores if GDAL/OGR library is already configured in the system
let configured = ref false

/// Helper function to execute the copy of a directory and its files in a destination folder
let rec directoryCopy srcPath dstPath copySubDirs =

    if not <| System.IO.Directory.Exists(srcPath) then
        let msg = System.String.Format("Source directory does not exist or could not be found: {0}", srcPath)
        raise (System.IO.DirectoryNotFoundException(msg))

    if not <| System.IO.Directory.Exists(dstPath) then
        System.IO.Directory.CreateDirectory(dstPath) |> ignore

    let srcDir = new System.IO.DirectoryInfo(srcPath)

    for file in srcDir.GetFiles() do
        let temppath = System.IO.Path.Combine(dstPath, file.Name)
        if not <| File.Exists(temppath) then
            file.CopyTo(temppath, true) |> ignore

    if copySubDirs then
        for subdir in srcDir.GetDirectories() do
            let dstSubDir = System.IO.Path.Combine(dstPath, subdir.Name)
            directoryCopy subdir.FullName dstSubDir copySubDirs

(**
Gdal Configuration
========================
*)

(**
To use gdal library in F# we need to have all the dlls in the executing assemly path 
and set the enivronoment variables it needs.

The `configure_gdal.fsx` script checks if all the files are already in the path and if not 
it copies them from the nuget packages folders.
*)

(**
If we work in the intercative shell, the executing assembly is the FSharp.Compiler itself. 

(Documents built with FsLab instead show that the executing assembly is the FsLab 
FSharp.Compiler.Service dll.)
*)

/// Copies all the GDAL/OGR dlls in the executing assemly path and set the enivronoment variables it needs.
/// Then registers all GDAL and OGR drivers.
let Init() = 
    
    if not !configured then

        let executingAssembly = new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase)
        let executingAssemblyFile = executingAssembly.LocalPath
        let executingDirectory = Path.GetDirectoryName(executingAssemblyFile)

        let gdalPath = Path.Combine(executingDirectory, "gdal")

        (**
        We set the `path` system environment variable 
        to point to the native gdal dlls plus the .NET wrappers copied in the executing 
        assembly directory. 

        Gdal provides both the x64 and x86 versions:
        *)

        let platform = if IntPtr.Size = 4 then "x86" else "x64"
        let nativePath = Path.Combine(gdalPath, platform)

        // Prepend native path to environment path, to ensure the
        // right libs are being used.
        let path = Environment.GetEnvironmentVariable("PATH")
        let newPath = nativePath + ";" + Path.Combine(nativePath, "plugins") + ";" + path
        Environment.SetEnvironmentVariable("PATH", newPath)

        (**
        Then we set the additional GDAL environment variables (`GDAL_DATA`, `GDAL_DRIVER_PATH`, 
        `GEOTIFF_CSV`, `PROJ_LIB`) calling the library's specific method `Gdal.SetConfigOption`:
        *)

        // Set the additional GDAL environment variables.
        let gdalData = Path.Combine(gdalPath, "data")
        Environment.SetEnvironmentVariable("GDAL_DATA", gdalData)
        Gdal.SetConfigOption("GDAL_DATA", gdalData)

        let driverPath = Path.Combine(nativePath, "plugins")
        Environment.SetEnvironmentVariable("GDAL_DRIVER_PATH", driverPath)
        Gdal.SetConfigOption("GDAL_DRIVER_PATH", driverPath)

        Environment.SetEnvironmentVariable("GEOTIFF_CSV", gdalData)
        Gdal.SetConfigOption("GEOTIFF_CSV", gdalData)

        let projSharePath = Path.Combine(gdalPath, "share")
        Environment.SetEnvironmentVariable("PROJ_LIB", projSharePath)
        Gdal.SetConfigOption("PROJ_LIB", projSharePath)

        OSGeo.GDAL.Gdal.AllRegister()
        OSGeo.OGR.Ogr.RegisterAll()

        configured := true

/// Print GDAL configured drivers
let printGdalDrivers() = 
    let num = OSGeo.GDAL.Gdal.GetDriverCount()
    for i in 0..(num-1) do 
        printfn "GDAL %i: %s" i (Gdal.GetDriver(i).ShortName) 

/// Print OGR configured drivers
let printOgrDrivers() = 
    let num = OSGeo.OGR.Ogr.GetDriverCount()
    for i in 0..(num-1) do 
        printfn "OGR %i: %s" i (Ogr.GetDriver(i).name) 
