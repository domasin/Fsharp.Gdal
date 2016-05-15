(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#load "packages/FsLab/FsLab.fsx"
open FSharp.Charting
#load "plot-geometries.fsx"
open ``Plot-geometries``

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
This is the equivalent of the Python GDAL/OGR Cookbook's Geomtry page for Fsharp.Gdal. 
To show the shape of geometries we use the `plot` function which is described in the 
[Plot Geometries](plot-geometries.html) appendix.
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

(*** define-output:plotPoint ***)
point |> plot
(*** include-it:plotPoint ***)

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

(*** define-output:plotLine ***)
line |> plot
(*** include-it:plotLine ***)

(**
Create a Polygon
------------------------
*)

(*** define-output:createAPolygon ***)
// Create ring
let ring = new OGR.Geometry(OGR.wkbGeometryType.wkbLinearRing)
ring.AddPoint(1179091.1646903288, 712782.8838459781, 0.)
ring.AddPoint(1161053.0218226474, 667456.2684348812, 0.)
ring.AddPoint(1214704.933941905, 641092.8288590391, 0.)
ring.AddPoint(1228580.428455506, 682719.3123998424, 0.)
ring.AddPoint(1218405.0658121984, 721108.1805541387, 0.)
ring.AddPoint(1179091.1646903288, 712782.8838459781, 0.)

// Create Polygon
let poly = new OGR.Geometry(OGR.wkbGeometryType.wkbPolygon)
poly.AddGeometry(ring)

let _,polyStringWkt = line.ExportToWkt()

printfn "Well Known Text = %s" polyStringWkt
(*** include-output:createAPolygon ***)

(*** define-output:plotPoly ***)
poly |> plot
(*** include-it:plotPoly ***)

(** 
Create Polygon with holes
*)

// Create outer ring
let outRing = new OGR.Geometry(OGR.wkbGeometryType.wkbLinearRing)
outRing.AddPoint(1154115.274565847, 686419.4442701361, 0.)
outRing.AddPoint(1154115.274565847, 653118.2574374934, 0.)
outRing.AddPoint(1165678.1866605144, 653118.2574374934, 0.)
outRing.AddPoint(1165678.1866605144, 686419.4442701361, 0.)
outRing.AddPoint(1154115.274565847, 686419.4442701361, 0.)

// Create inner ring
let innerRing = new OGR.Geometry(OGR.wkbGeometryType.wkbLinearRing)
innerRing.AddPoint(1149490.1097279799, 691044.6091080031, 0.)
innerRing.AddPoint(1149490.1097279799, 648030.5761158396, 0.)
innerRing.AddPoint(1191579.1097525698, 648030.5761158396, 0.)
innerRing.AddPoint(1191579.1097525698, 691044.6091080031, 0.)
innerRing.AddPoint(1149490.1097279799, 691044.6091080031, 0.)

// Create polygon
let polyWithHoles = new OGR.Geometry(OGR.wkbGeometryType.wkbPolygon)
polyWithHoles.AddGeometry(outRing)
polyWithHoles.AddGeometry(innerRing)

(*** define-output:plotPolyWithHoles ***)
polyWithHoles |> plot
(*** include-it:plotPolyWithHoles ***)

//(**
//Get the bounding box
//*)
//
//let env = 
//    let res = new OGR.Envelope()
//    line.GetEnvelope(res)
//    res



