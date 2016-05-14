(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#load "packages/FsLab/FsLab.fsx"
open FSharp.Charting

//#I "../bin/Fsharp.Gdal"
#I "../src/Fsharp.Gdal/bin/Debug"

#r "Fsharp.Gdal.dll"
#r "gdal_csharp.dll"
#r "gdalconst_csharp.dll"
#r "ogr_csharp.dll"
#r "osr_csharp.dll"

open FSharp.Gdal

(**
Geometry
========================
This is the equivalent of the Python GDAL/OGR Cookbook's Geomtry page for Fsharp.Gdal
*)

(**
Configure gdal
------------------------
Call `Configuration.Init()` to configure the library
*)

Configuration.Init() |> ignore

(**
Create a Point
------------------------
*)

open OSGeo

(*** define-output:createAPoint ***)
let point = new OGR.Geometry(OGR.wkbGeometryType.wkbPoint)
point.AddPoint(1198054.34, 648493.09,0.)

let result, wkt = point.ExportToWkt()

printfn "Result = %i" result
printfn "Well Known Text = %s" wkt
(*** include-output:createAPoint ***)

(**
Create a LineString
------------------------
*)

(*** define-output:createALineString ***)
let line = new OGR.Geometry(OGR.wkbGeometryType.wkbLineString)
line.AddPoint(1116651.439379124, 637392.6969887456, 0.)
line.AddPoint(1188804.0108498496, 652655.7409537067, 0.)
line.AddPoint(1226730.3625203592, 634155.0816022386, 0.)
line.AddPoint(1281307.30760719, 636467.6640211721, 0.)

let _,lineStringWkt = line.ExportToWkt()

printfn "Well Known Text = %s" lineStringWkt
(*** include-output:createALineString ***)

(**
... and just to visualize it:
*)

(*** define-output:chartLineString ***)
let coordinates = 
    let last = line.GetPointCount() - 1
    [
        for i in 0..last ->
            let p = [|0.;0.|]
            line.GetPoint(i,p)
            (p.[0], p.[1])
    ]
Chart.Line(coordinates)
(*** include-it:chartLineString ***)

(**
Get the bounding box
*)

let env = 
    let res = new OGR.Envelope()
    line.GetEnvelope(res)
    res



