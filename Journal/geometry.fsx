(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#load "packages/FsLab/FsLab.fsx"
#load "plot-geometry.fsx"
open ``Plot-geometry``

#I "../bin/Fsharp.Gdal"
//#I "../src/Fsharp.Gdal/bin/Debug"

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
[Plot Geometry](plot-geometry.html) appendix.
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

let result, wktPoint = point.ExportToWkt()

printfn "Result = %i" result
printfn "Well Known Text = %s" wktPoint
(*** include-output:createAPoint ***)

point |> plot "point"

(**
![point](./img/point.png "Point")
*)

(**
Create a LineString
------------------------
*)

let line = new OGR.Geometry(OGR.wkbGeometryType.wkbLineString)
line.AddPoint(1116651.439379124, 637392.6969887456, 0.)
line.AddPoint(1188804.0108498496, 652655.7409537067, 0.)
line.AddPoint(1226730.3625203592, 634155.0816022386, 0.)
line.AddPoint(1281307.30760719, 636467.6640211721, 0.)

let _,lineStringWkt = line.ExportToWkt()
(*** include-value:lineStringWkt ***)

line |> plot "line"

(**
![line](./img/line.png "Point")
*)

(**
Create a Polygon
------------------------
*)

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

let _,polyStringWkt = poly.ExportToWkt()
(*** include-value:polyStringWkt ***)

poly |> plot "poly"

(**
![poly](./img/poly.png "Point")
*)

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

(** 
Inner rings must be added before to create a hole
*)
polyWithHoles.AddGeometry(innerRing)
polyWithHoles.AddGeometry(outRing)

let _,polyWithHolesWkt = polyWithHoles.ExportToWkt()
(*** include-value:polyWithHolesWkt ***)

polyWithHoles |> plot "polyWithHoles"

(**
![polyWithHoles](./img/polyWithHoles.png "Point")
*)

(**
Create a MultiPoint
------------------------
*)

let multipoint = new OGR.Geometry(OGR.wkbGeometryType.wkbMultiPoint)

let point1 = new OGR.Geometry(OGR.wkbGeometryType.wkbPoint)
point1.AddPoint(1251243.7361610543, 598078.7958668759, 0.)
multipoint.AddGeometry(point1)

let point2 = new OGR.Geometry(OGR.wkbGeometryType.wkbPoint)
point2.AddPoint(1240605.8570339603, 601778.9277371694, 0.)
multipoint.AddGeometry(point2)

let point3 = new OGR.Geometry(OGR.wkbGeometryType.wkbPoint)
point3.AddPoint(1250318.7031934808, 606404.0925750365, 0.)
multipoint.AddGeometry(point3)

let _,multipointWkt = multipoint.ExportToWkt()
(*** include-value:multipointWkt ***)

multipoint |> plot "multipoint"

(**
![multipoint](./img/multipoint.png "Point")
*)

(**
Create a MultiLineString
------------------------
*)

let multiline = new OGR.Geometry(OGR.wkbGeometryType.wkbMultiLineString)

let line1 = new OGR.Geometry(OGR.wkbGeometryType.wkbLineString)
line1.AddPoint(1214242.4174581182, 617041.9717021306, 0.)
line1.AddPoint(1234593.142744733, 629529.9167643716, 0.)
multiline.AddGeometry(line1)

let line2 = new OGR.Geometry(OGR.wkbGeometryType.wkbLineString)
line2.AddPoint(1184641.3624957693, 626754.8178616514, 0.)
line2.AddPoint(1219792.6152635587, 606866.6090588232, 0.)
multiline.AddGeometry(line2)

let _,multilineWkt = multiline.ExportToWkt()
(*** include-value:multilineWkt ***)

multiline |> plot "multiline"

(**
![multiline](./img/multiline.png "Point")
*)

(**
Create a MultiPloygon
------------------------
*)

let multipolygon = new OGR.Geometry(OGR.wkbGeometryType.wkbMultiPolygon)

// Create ring #1
let ring1 = new OGR.Geometry(OGR.wkbGeometryType.wkbLinearRing)
ring1.AddPoint(1204067.0548148106, 634617.5980860253, 0.)
ring1.AddPoint(1204067.0548148106, 620742.1035724243, 0.)
ring1.AddPoint(1215167.4504256917, 620742.1035724243, 0.)
ring1.AddPoint(1215167.4504256917, 634617.5980860253, 0.)
ring1.AddPoint(1204067.0548148106, 634617.5980860253, 0.)

// Create polygon #1
let poly1 = new OGR.Geometry(OGR.wkbGeometryType.wkbPolygon)
poly1.AddGeometry(ring1)
multipolygon.AddGeometry(poly1)

// Create ring #2
let ring2 = new OGR.Geometry(OGR.wkbGeometryType.wkbLinearRing)
ring2.AddPoint(1179553.6811741155, 647105.5431482664, 0.)
ring2.AddPoint(1179553.6811741155, 626292.3013778647, 0.)
ring2.AddPoint(1194354.20865529, 626292.3013778647, 0.)
ring2.AddPoint(1194354.20865529, 647105.5431482664, 0.)
ring2.AddPoint(1179553.6811741155, 647105.5431482664, 0.)

// Create polygon #1
let poly2 = new OGR.Geometry(OGR.wkbGeometryType.wkbPolygon)
poly2.AddGeometry(ring2)
multipolygon.AddGeometry(poly2)

let _,multipolygonWkt = multipolygon.ExportToWkt()
(*** include-value:multipolygonWkt ***)

multipolygon |> plot "multipolygon"

(**
![multipolygon](./img/multipolygon.png "Point")
*)

(**
Create a GeometryCollection
------------------------
*)

let geomcol = new OGR.Geometry(OGR.wkbGeometryType.wkbGeometryCollection)

// Add a point
let point4 = new OGR.Geometry(OGR.wkbGeometryType.wkbPoint)
point.AddPoint(-122.23, 47.09, 0.)
geomcol.AddGeometry(point)

// Add a line
let line3 = new OGR.Geometry(OGR.wkbGeometryType.wkbLineString)
line3.AddPoint(-122.60, 47.14, 0.)
line3.AddPoint(-122.48, 47.23, 0.)
geomcol.AddGeometry(line3)

let _,geomcolWkt = geomcol.ExportToWkt()
(*** include-value:geomcolWkt ***)

geomcol |> plot "geomcol"

(**
![geomcol](./img/geomcol.png "Point")
*)

(**
Create Geometry from WKT
------------------------
*)

(*** define-output:createFromWKT ***)
let wkt = ref "POINT (1120351.5712494177 741921.4223245403)"
let point5 = OGR.Ogr.CreateGeometryFromWkt(wkt, null)

printfn "%f,%f" (point5.GetX(0)) (point5.GetY(0))
(*** include-output:createFromWKT ***)

point5 |> plot "point5"

(**
![point5](./img/point5.png "Point")
*)

(**
Create Geometry from GeoJSON
------------------------
*)

(*** define-output:createFromGeoJSON ***)
let geojson = """{"type":"Point","coordinates":[108420.33,753808.59]}"""
let point6 = OGR.Ogr.CreateGeometryFromJson(geojson)

printfn "%f,%f" (point6.GetX(0)) (point6.GetY(0))
(*** include-output:createFromGeoJSON ***)

point6 |> plot "point6"

(**
![point6](./img/point6.png "Point")
*)

(**
Create Geometry from GML
------------------------
*)

(*** define-output:createFromGML ***)
let gml = """<gml:Point xmlns:gml="http://www.opengis.net/gml"><gml:coordinates>108420.33,753808.59</gml:coordinates></gml:Point>"""
let point7 = OGR.Ogr.CreateGeometryFromGML(gml)

printfn "%f,%f" (point7.GetX(0)) (point7.GetY(0))
(*** include-output:createFromGML ***)

point7 |> plot "point7"

(**
![point7](./img/point7.png "Point")
*)

(**
Create Geometry from WKB
------------------------
*)

// TODO

(**
Count Points in a Geometry
------------------------
*)

(*** define-output:countPoints ***)
let wkt2 = ref "LINESTRING (1181866.263593049 615654.4222507705, 1205917.1207499576 623979.7189589312, 1227192.8790041457 643405.4112779726, 1224880.2965852122 665143.6860159477)"
let line4 = OGR.Ogr.CreateGeometryFromWkt(wkt2, null)

printfn "Geometry has %i points" (line4.GetPointCount())
(*** include-output:countPoints ***)

(**
Count Geometries in a Geometry
------------------------
*)

(*** define-output:countGeometries ***)
let wkt3 = ref "MULTIPOINT (1181866.263593049 615654.4222507705, 1205917.1207499576 623979.7189589312, 1227192.8790041457 643405.4112779726, 1224880.2965852122 665143.6860159477)"
let geom = OGR.Ogr.CreateGeometryFromWkt(wkt3, null)

printfn "Geometry has %i geometries" (geom.GetGeometryCount())
(*** include-output:countGeometries ***)

(**
Iterate over Geometries in a Geometry
------------------------
*)

(*** define-output:iterateGeometries ***)
for i in 0..(geom.GetGeometryCount() - 1) do
    let g = geom.GetGeometryRef(i)
    let _,wkt = g.ExportToWkt()
    printfn "%i) %s" i wkt
(*** include-output:iterateGeometries ***)

(**
Iterate over Points in a Geometry
------------------------
*)

(*** define-output:iteratePoints ***)
for i in 0..(line4.GetPointCount() - 1) do
    let pt = [|0.; 0.|]
    line4.GetPoint(i, pt)
    printfn "%i) POINT (%f %f)" i pt.[0] pt.[1]
(*** include-output:iteratePoints ***)

(**
Buffer a Geometry
------------------------

`Buffer` creates a polygon around a geometry at a speicified distance:
*)

let bufferDistance = 1000.
let lineBuffer = line4.Buffer(bufferDistance, 1)

let lineAndBuffer = new OGR.Geometry(OGR.wkbGeometryType.wkbGeometryCollection)
lineAndBuffer.AddGeometry(lineBuffer)
lineAndBuffer.AddGeometry(line4)

lineAndBuffer |> plot "lineAndBuffer"

(**
![lineAndBuffer](./img/lineAndBuffer.png "Point")
*)

(**
Calculate Envelope of a Geometry
------------------------

The `Envelope` is the minimal rectangular area enclosing the geometry
*)

(*** define-output:envelope ***)
let env = new OGR.Envelope()
line4.GetEnvelope(env)

printfn "MinX: %f, MinY: %f, MaxX: %f, MaxY: %f" env.MinX env.MinY env.MaxX env.MaxY
(*** include-output:envelope ***)


