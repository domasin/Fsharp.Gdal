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

let pointPlot = point |> Plot

(*** hide ***)
pointPlot.SaveAsBitmap("pointPlot")

(**
![pointPlot](./img/pointPlot.png "pointPlot")
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

let linePlot = line |> Plot

(*** hide ***)
linePlot.SaveAsBitmap("linePlot")

(**
![linePlot](./img/linePlot.png "linePlot")
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

let polyPlot = poly |> Plot

(*** hide ***)
polyPlot.SaveAsBitmap("polyPlot")

(**
![polyPlot](./img/polyPlot.png "polyPlot")
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

polyWithHoles.AddGeometry(innerRing)
polyWithHoles.AddGeometry(outRing)

let _,polyWithHolesWkt = polyWithHoles.ExportToWkt()
(*** include-value:polyWithHolesWkt ***)

let polyWithHolesPlot = polyWithHoles |> Plot

(*** hide ***)
polyWithHolesPlot.SaveAsBitmap("polyWithHolesPlot")

(**
![polyWithHolesPlot](./img/polyWithHolesPlot.png "polyWithHolesPlot")
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

let multipointPlot = multipoint |> Plot

(*** hide ***)
multipointPlot.SaveAsBitmap("multipointPlot")

(**
![multipointPlot](./img/multipointPlot.png "multipointPlot")
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

let multilinePlot = multiline |> Plot

(*** hide ***)
multilinePlot.SaveAsBitmap("multilinePlot")

(**
![multilinePlot](./img/multilinePlot.png "multilinePlot")
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

let multipolygonPlot = multipolygon |> Plot

(*** hide ***)
multipolygonPlot.SaveAsBitmap("multipolygonPlot")

(**
![multipolygonPlot](./img/multipolygonPlot.png "multipolygonPlot")
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

let geomcolPlot = geomcol |> Plot

(*** hide ***)
geomcolPlot.SaveAsBitmap("geomcolPlot")

(**
![geomcolPlot](./img/geomcolPlot.png "geomcolPlot")
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

let point5Plot = point5 |> Plot

(*** hide ***)
point5Plot.SaveAsBitmap("point5Plot")

(**
![point5Plot](./img/point5Plot.png "point5Plot")
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

let point6Plot = point6 |> Plot

(*** hide ***)
point6Plot.SaveAsBitmap("point6Plot")

(**
![point6Plot](./img/point6Plot.png "point6Plot")
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

let point7Plot = point7 |> Plot

(*** hide ***)
point7Plot.SaveAsBitmap("point7Plot")

(**
![point7Plot](./img/point7Plot.png "point7Plot")
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

let lineAndBufferPlot = lineAndBuffer |> Plot

(*** hide ***)
lineAndBufferPlot.SaveAsBitmap("lineAndBufferPlot")

(**
![lineAndBufferPlot](./img/lineAndBufferPlot.png "lineAndBufferPlot")
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

(**
(In the plot-geometry script I've already defined an utility function `toEnv` to obtain the 
envelope of a geometry more easily.)
*)

(**
Calculate the Area of a Geometry
------------------------
*)

(*** define-output:area ***)
let wkt4 = ref "POLYGON ((1162440.5712740074 672081.4332727483, 1162440.5712740074 647105.5431482664, 1195279.2416228633 647105.5431482664, 1195279.2416228633 672081.4332727483, 1162440.5712740074 672081.4332727483))"
let poly3 = OGR.Ogr.CreateGeometryFromWkt(wkt4, null)

printfn "Area = %f" (poly3.GetArea())
(*** include-output:area ***)

(**
Calculate the Length of a Geometry
------------------------
*)

(*** define-output:length ***)
let wkt5 = ref "LINESTRING (1181866.263593049 615654.4222507705, 1205917.1207499576 623979.7189589312, 1227192.8790041457 643405.4112779726, 1224880.2965852122 665143.6860159477)"
let geom2 = OGR.Ogr.CreateGeometryFromWkt(wkt5, null)

printfn "Length = %f" (geom2.Length())
(*** include-output:length ***)

(**
Get the geometry type (as a string) from a Geometry
------------------------
*)

(*** define-output:geometryName ***)
let wkts = 
    [
        ref "POINT (1198054.34 648493.09)"
        ref "LINESTRING (1181866.263593049 615654.4222507705, 1205917.1207499576 623979.7189589312, 1227192.8790041457 643405.4112779726, 1224880.2965852122 665143.6860159477)"
        ref "POLYGON ((1162440.5712740074 672081.4332727483, 1162440.5712740074 647105.5431482664, 1195279.2416228633 647105.5431482664, 1195279.2416228633 672081.4332727483, 1162440.5712740074 672081.4332727483))"
    ]

for wkt in wkts do
    let geom = OGR.Ogr.CreateGeometryFromWkt(wkt, null)
    printfn "%s" (geom.GetGeometryName())
(*** include-output:geometryName ***)

(**
Calculate intersection between two Geometries
------------------------
*)

let wkt7 = ref "POLYGON ((1208064.271243039 624154.6783778917, 1208064.271243039 601260.9785661874, 1231345.9998651114 601260.9785661874, 1231345.9998651114 624154.6783778917, 1208064.271243039 624154.6783778917))"
let wkt6 = ref "POLYGON ((1199915.6662253144 633079.3410163528, 1199915.6662253144 614453.958118695, 1219317.1067437078 614453.958118695, 1219317.1067437078 633079.3410163528, 1199915.6662253144 633079.3410163528)))"

let poly4 = OGR.Ogr.CreateGeometryFromWkt(wkt7, null)
let poly5 = OGR.Ogr.CreateGeometryFromWkt(wkt6, null)

let intersection = poly4.Intersection(poly5)

let _, intersectionStr = intersection.ExportToWkt()

(*** define-output:intersections ***)
printfn "%s" intersectionStr
(*** include-output:intersections ***)

(**
To graphically visualize the intersection we can add all the three geometries 
in a geometry collection and then plot it:
*)

let geomcol2 = new OGR.Geometry(OGR.wkbGeometryType.wkbGeometryCollection)
geomcol2.AddGeometry(poly4)
geomcol2.AddGeometry(poly5)
geomcol2.AddGeometry(intersection)

let geomcol2Plot = geomcol2 |> Plot

(*** hide ***)
geomcol2Plot.SaveAsBitmap("geomcol2Plot")

(**
![geomcol2Plot](./img/geomcol2Plot.png "geomcol2Plot")
*)

(**
Calculate union between two Geometries
------------------------
*)

let wkt8 = ref "POLYGON ((1208064.271243039 624154.6783778917, 1208064.271243039 601260.9785661874, 1231345.9998651114 601260.9785661874, 1231345.9998651114 624154.6783778917, 1208064.271243039 624154.6783778917))"
let wkt9 = ref "POLYGON ((1199915.6662253144 633079.3410163528, 1199915.6662253144 614453.958118695, 1219317.1067437078 614453.958118695, 1219317.1067437078 633079.3410163528, 1199915.6662253144 633079.3410163528)))"

let poly6 = OGR.Ogr.CreateGeometryFromWkt(wkt8, null)
let poly7 = OGR.Ogr.CreateGeometryFromWkt(wkt9, null)

let geomcol3 = new OGR.Geometry(OGR.wkbGeometryType.wkbGeometryCollection)
geomcol3.AddGeometry(poly6)
geomcol3.AddGeometry(poly7)

let geomcol3Plot = geomcol3 |> Plot

(*** hide ***)
geomcol3Plot.SaveAsBitmap("geomcol3Plot")

(**
![geomcol3Plot](./img/geomcol3Plot.png "geomcol3Plot")
*)

let union = poly6.Union(poly7)

let unionPlot = union |> Plot

(*** hide ***)
unionPlot.SaveAsBitmap("unionPlot")

(**
![unionPlot](./img/unionPlot.png "unionPlot")
*)

(**
Write Geometry to GeoJSON
------------------------
Following the Python GDAL/OGR Cookbook we will inspect two options 
to create a GeoJSON from a geometry.

First option: create a new GeoJSON file:
*)

(**
we start from a test polygon:
*)
let ring3 = new OGR.Geometry(OGR.wkbGeometryType.wkbLinearRing)
ring3.AddPoint(1000., 1000., 0.)
ring3.AddPoint(3000., 1000., 0.)
ring3.AddPoint(2000., 2000., 0.)
ring3.AddPoint(1000., 1000., 0.)
let poly8 = new OGR.Geometry(OGR.wkbGeometryType.wkbPolygon)
poly8.AddGeometry(ring3)

(**
intialize the righ output Driver
*)
let outDriver = OGR.Ogr.GetDriverByName("GeoJSON")

(**
with it we create the new json file, but first we need to be sure that 
the file is not yet there because the driver can't override it
*)
let jsonFileName = __SOURCE_DIRECTORY__ + "\\test.geojson"

if System.IO.File.Exists(jsonFileName) then 
    System.IO.File.Delete(jsonFileName)

let outDataSource = outDriver.CreateDataSource(jsonFileName, [||])
let outLayer = outDataSource.CreateLayer(jsonFileName, null, OGR.wkbGeometryType.wkbPolygon, [||])

// Get the output Layer's Feature Definition
let featureDefn = outLayer.GetLayerDefn()

// create a new feature
let outFeature = new OGR.Feature(featureDefn)

// Set new geometry
outFeature.SetGeometry(poly8)

// Add new feature to output Layer
outLayer.CreateFeature(outFeature)

// destroy the feature
outFeature.Dispose()

// Close DataSources
outDataSource.Dispose()

(**
Taking advantage of the [OgrTypeProvider](gdal-type-provider.html) we can inspect what is inside our 
newly created json file:
*)

let newJson = new OgrTypeProvider<"G:/GitHub/Fsharp.Gdal/Journal/test.geojson">()

(*** define-output:jsonFeatures ***)
for feat in newJson.Features do
    printfn "Geometry type: %A" (feat.Geometry.GetGeometryType())
(*** include-output:jsonFeatures ***)

(**
... ok a wkbPolygon25D so let's plot it:
*)

let mutable jsonPoly = new OGR.Geometry(OGR.wkbGeometryType.wkbPolygon25D)

for feat in newJson.Features do
    jsonPoly <- feat.Geometry

let jsonPolyPlot = jsonPoly |> Plot

(*** hide ***)
jsonPolyPlot.SaveAsBitmap("jsonPolyPlot")

(**
![jsonPolyPlot](./img/jsonPolyPlot.png "jsonPolyPlot")
*)

(**
Second option: simply export the geometry to Json and print it
*)

(*** define-output:exportToJson ***)
let geojson2 = poly8.ExportToJson([||])
printfn "%s" geojson2
(*** include-output:exportToJson ***)

(**
Write Geometry to WKT
------------------------
TODO: not really interesting
*)

(**
Write Geometry to KML
------------------------
TODO: not really interesting
*)

(**
Write Geometry to WKB
------------------------
TODO: not really interesting
*)

(**
Force polygon to multipolygon
------------------------
TODO: not really interesting
*)

(**
Quarter polygon and create centroids
------------------------
Given a test polygon
*)

let polyWkt = ref "POLYGON((-107.42631019589980212 40.11971708125970082,-107.42455436683293613 40.12061219666851741,-107.42020981542387403 40.12004414402532859,-107.41789122063043749 40.12149008687303819,-107.41419947746419439 40.11811617239460048,-107.41915181585792993 40.11761695654455906,-107.41998470913324581 40.11894245264452508,-107.42203317637793702 40.1184088144647788,-107.42430674991324224 40.1174448122981957,-107.42430674991324224 40.1174448122981957,-107.42631019589980212 40.11971708125970082))"
let geomPoly = OGR.Ogr.CreateGeometryFromWkt(polyWkt, null)

let geomPolyPlot = geomPoly |> Plot

(*** hide ***)
geomPolyPlot.SaveAsBitmap("geomPolyPlot")

(**
![geomPolyPlot](./img/geomPolyPlot.png "geomPolyPlot")
*)

(**
Create 4 square polygons
*)

let env2 = geomPoly |> toEnv
let minX = env2.MinX
let minY = env2.MinY
let maxX = env2.MaxX
let maxY = env2.MaxY

// coord0----coord1----coord2
// |           |           |
// coord3----coord4----coord5
// |           |           |
// coord6----coord7----coord8

let coord0 = minX, maxY
let coord1 = minX+(maxX-minX)/2., maxY
let coord2 = maxX, maxY
let coord3 = minX, minY+(maxY-minY)/2.
let coord4 = minX+(maxX-minX)/2., minY+(maxY-minY)/2.
let coord5 = maxX, minY+(maxY-minY)/2.
let coord6 = minX, minY
let coord7 = minX+(maxX-minX)/2., minY
let coord8 = maxX, minY

let ringTopLeft = new OGR.Geometry(OGR.wkbGeometryType.wkbLinearRing)
ringTopLeft.AddPoint_2D(coord0)
ringTopLeft.AddPoint_2D(coord1)
ringTopLeft.AddPoint_2D(coord4)
ringTopLeft.AddPoint_2D(coord3)
ringTopLeft.AddPoint_2D(coord0)
let polyTopLeft = new OGR.Geometry(OGR.wkbGeometryType.wkbPolygon)
polyTopLeft.AddGeometry(ringTopLeft)

let ringTopRight = new OGR.Geometry(OGR.wkbGeometryType.wkbLinearRing)
ringTopRight.AddPoint_2D(coord1)
ringTopRight.AddPoint_2D(coord2)
ringTopRight.AddPoint_2D(coord5)
ringTopRight.AddPoint_2D(coord4)
ringTopRight.AddPoint_2D(coord1)
let polyTopRight = new OGR.Geometry(OGR.wkbGeometryType.wkbPolygon)
polyTopRight.AddGeometry(ringTopRight)

let ringBottomLeft = new OGR.Geometry(OGR.wkbGeometryType.wkbLinearRing)
ringBottomLeft.AddPoint_2D(coord3)
ringBottomLeft.AddPoint_2D(coord4)
ringBottomLeft.AddPoint_2D(coord7)
ringBottomLeft.AddPoint_2D(coord6)
ringBottomLeft.AddPoint_2D(coord3)
let polyBottomLeft = new OGR.Geometry(OGR.wkbGeometryType.wkbPolygon)
polyBottomLeft.AddGeometry(ringBottomLeft)

let ringBottomRight = new OGR.Geometry(OGR.wkbGeometryType.wkbLinearRing)
ringBottomRight.AddPoint_2D(coord4)
ringBottomRight.AddPoint_2D(coord5)
ringBottomRight.AddPoint_2D(coord8)
ringBottomRight.AddPoint_2D(coord7)
ringBottomRight.AddPoint_2D(coord4)
let polyBottomRight = new OGR.Geometry(OGR.wkbGeometryType.wkbPolygon)
polyBottomRight.AddGeometry(ringBottomRight)

let quartersCol = new OGR.Geometry(OGR.wkbGeometryType.wkbGeometryCollection)
quartersCol.AddGeometry(polyTopLeft)
quartersCol.AddGeometry(polyTopRight)
quartersCol.AddGeometry(polyBottomLeft)
quartersCol.AddGeometry(polyBottomRight)
quartersCol.AddGeometry(geomPoly)

let quartersColPlot = quartersCol |> Plot

(*** hide ***)
quartersColPlot.SaveAsBitmap("quartersColPlot")

(**
![quartersColPlot](./img/quartersColPlot.png "quartersColPlot")
*)

(**
Intersect 4 squares polygons with test polygon
*)

let quaterPolyTopLeft = polyTopLeft.Intersection(geomPoly)
let quaterPolyTopRight = polyTopRight.Intersection(geomPoly)
let quaterPolyBottomLeft = polyBottomLeft.Intersection(geomPoly)
let quaterPolyBottomRight = polyBottomRight.Intersection(geomPoly)

(**
Create centroids of each intersected polygon
*)

let centroidTopLeft = quaterPolyTopLeft.Centroid()
let centroidTopRight = quaterPolyTopRight.Centroid()
let centroidBottomLeft = quaterPolyBottomLeft.Centroid()
let centroidBottomRight = quaterPolyBottomRight.Centroid()

let quartersWithCentroidsCol = new OGR.Geometry(OGR.wkbGeometryType.wkbGeometryCollection)
quartersWithCentroidsCol.AddGeometry(polyTopLeft)
quartersWithCentroidsCol.AddGeometry(polyTopRight)
quartersWithCentroidsCol.AddGeometry(polyBottomLeft)
quartersWithCentroidsCol.AddGeometry(polyBottomRight)
quartersWithCentroidsCol.AddGeometry(geomPoly)
quartersWithCentroidsCol.AddGeometry(centroidTopLeft)
quartersWithCentroidsCol.AddGeometry(centroidTopRight)
quartersWithCentroidsCol.AddGeometry(centroidBottomLeft)
quartersWithCentroidsCol.AddGeometry(centroidBottomRight)

let quartersWithCentroidsColPlot = quartersWithCentroidsCol |> Plot

(*** hide ***)
quartersWithCentroidsColPlot.SaveAsBitmap("quartersWithCentroidsColPlot")

(**
![quartersWithCentroidsColPlot](./img/quartersWithCentroidsColPlot.png "quartersWithCentroidsColPlot")
*)