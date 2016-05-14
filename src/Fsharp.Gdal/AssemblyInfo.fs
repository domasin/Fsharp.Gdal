namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Fsharp.Gdal")>]
[<assembly: AssemblyProductAttribute("Fsharp.Gdal")>]
[<assembly: AssemblyDescriptionAttribute("Geospatial Data in F#")>]
[<assembly: AssemblyVersionAttribute("0.0.1")>]
[<assembly: AssemblyFileVersionAttribute("0.0.1")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.0.1"
    let [<Literal>] InformationalVersion = "0.0.1"
