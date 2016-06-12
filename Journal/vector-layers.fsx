(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "packages/FSharp.Data/lib/net40"
#r "FSharp.Data.dll"
#load "packages/Deedle/Deedle.fsx"
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
Vector Layers
========================

1. [Configure the library](#configure)
2. [Supported formats](#formats)
3. [Open datasources](#datasource)
4. [Inspecting layers](#layers)
5. [Get layers features](#features)
6. [Get layers fields](#fields)
7. [Get features geometries](#geometries)
8. [Layer Spatial Referece Systems](#spatialReference)
9. [Visualize the non geometric data in a deedle frame](#deedleFrame)
10. [Getting and Querying Open Street Map Data](#osm)

[References](#references)
*)

(**
The OGR part of GDAL/OGR Library provides a single vector abstract data model to the calling application 
for a variety of different supported formats including among others the well known ESRI Shapefile, but 
also RDBMSes, directories full of files, or even remote web services depending on the driver being used.
*)

(**
<a name="configure"></a>Configure the library
------------------------
Initially it is necessary to configure the library and register all the format drivers that are desired. 
The FSharp.Gdal library accomplishes this calling the function `Configuration.Init()`:
*)

Configuration.Init()

(**
<a name="formats"></a>Supported formats
------------------------
The list of the supported formats is very big: to check that the library is correctly configured and 
all OGR drivers are properly registered we call:
*)

(*** define-output:ogrDrivers ***)
Configuration.printOgrDrivers()
(*** include-output:ogrDrivers ***)

(**
<a name="datasource"></a>Open datasources
------------------------
Next we need to open the input OGR datasource. Despite the varietry of formats 
the datasource name is always a single string. 

In this case we will open a shapefile provided by the 
[Italian National Institute of Statistics (Istat)](http://www.istat.it/it/archivio/24613/) 
containing the administrative limits of italian regions. 
*)

open OSGeo

let italyShp = __SOURCE_DIRECTORY__ + @"\data\reg2001_g\reg2001_g.shp"
let driver = OGR.Ogr.GetDriverByName("ESRI Shapefile")
let ds = driver.Open(italyShp, 0)

(*** include-value:ds ***)

(**
An `OGR.DataSource` contains a set of `OGR.Layer`s each of which in turn contains a 
set of `OGR.Feature`s and an `OGR.FeatureDefn`.

Globally we can though of the datasource as a database, a layer as a table in the database 
and the layer's features as its records while the feature definition provides its columns.

From the output above this structure is not visbile. The aim of `FSharp.Gdal` is to improve 
the use of the standard OGR/GDAL library within the fsharp interactive shell and to 
provide utility functions to make it easier (and more idiomatic) writing F# scripts 
to process geospatial data.

To this aim the first thing that we want is to get immediately from the output an overview 
of the objects we're dealing with and an hint to successive inspections. The first of this functions 
is `datasourceInfo` implemented in the `FSharp.Gdal.Vector` module (below we will use more of 
these functions the implementation of which can be viewed directly in the source code of the 
[Vector module](https://github.com/domasin/Fsharp.Gdal/blob/master/src/Fsharp.Gdal/Vector.fs) ).
*)

let dsInfo = ds |> Vector.datasourceInfo
(*** include-value:dsInfo ***)

(**
Not only we can call directly the `datasourceInfo` function but we can also use it to define a 
custom printer for the `OGR.DataSource`:
*)

fsi.AddPrintTransformer (fun (datasource:OGR.DataSource) -> box (datasource |> Vector.datasourceInfo))

(**
In the sequel we will inspect the layer's structure and define a custom printer also for the `OGR.Layer` after that 
whenever we will open again a new datasource we will get all the infos describing its structure.
*)

(**
<a name="layers"></a>Inspecting layers
------------------------
As pointed out above an `OGR.DataSource` can potentially have many layers associated with it. The number 
of available layers can be queried with `GetLayerCount()` and individual layers fetched by index using `GetLayerByIndex`.
*)

(*** define-output:layersCount ***)
let layersCount = ds.GetLayerCount()

printfn "There is %i layer" layersCount
(*** include-output:layersCount ***)

let layer0 = ds.GetLayerByIndex(0)

(**
instead of using these low level GDAL/OGR methods however the `Vector` module provides the 
function `layers` which returns the layers in a Vector DataSource as an F# list.
*)

let layers = ds |> Vector.layers
(*** include-value:layers ***)

(**
As we can see the output is still not very informative so we call the function 
`layerInfo` to immediately inspect the layer's content:
*)

let layerInfo = 
    layers
    |> List.map (fun ly -> ly |> Vector.layerInfo)

(*** define-output:printLayerContents ***)
printfn "%s" (layerInfo.ToString())
(*** include-output:printLayerContents ***)

(**
This is better: now we can see that the layer has 20 features (records) in it, 
is a layer of geometry type polygon and all features in it provide three non geometric 
informations (attributes or fields): COD_REG, REGIONE and POP2001.

To complete the configuration of the F# interactive shell we use the function 
to define the custom printer for the `OGR.Layer` type:
*)

fsi.AddPrintTransformer (fun (layer:OGR.Layer) -> box (layer |> Vector.layerInfo))

(**
<a name="features"></a>Get layers features
------------------------
A layer contains a set of features that can be viewed as records of a database table.
To get them just call `features` to obtain an F# list:
*)

let layer = layers |> List.head
let features = layer |> Vector.features

(**
<a name="fields"></a>Get layers fields
------------------------
The feature definition is globally associated to the containing layer. Here the function to use 
is `fields` which returns a tuple of all the index, name and type of the fields 
in the layer's definition.
*)

(*** define-output:printAttributes ***)
let attributes = layer |> Vector.fields

for fieldIndex, fieldName, fieldType in attributes do
    printfn "Field Index = %i; Field Name = %s; Field Type = %A" fieldIndex fieldName fieldType
(*** include-output:printAttributes ***)

(**
In this case the attribute "Regione" is the region's name so we can iterate over all features 
to get all the italian regions' names quering the field with index 1:
*)

(*** define-output:regionsNames ***)
for feat in features do 
    let regionName = feat.GetFieldAsString(1)
    printfn "%s" regionName
(*** include-output:regionsNames ***)

(**
<a name="geometries"></a>Get features geometries
------------------------
In addition to attributes a feature contains a geometry field. In this example each feature 
corresponds to an italian region. To graphically plot them we can collect all features' geometries 
in a geometry collection and call the `Plot` utility (see [Plot Geometry](plot-geometry.html)) to get 
a map of Italy divided by regions:
*)

let geomcol = new OGR.Geometry(OGR.wkbGeometryType.wkbGeometryCollection)

for feat in features do 
    geomcol.AddGeometry(feat.GetGeometryRef()) |> ignore

let italyRegionsPlot = geomcol |> Plot

(*** hide ***)
italyRegionsPlot.SaveAsBitmap("italyRegionsPlot")

(**
![italyRegionsPlot](./img/italyRegionsPlot.png "italyRegionsPlot")
*)

(**
<a name="spatialReference"></a>Spatial reference system
------------------------
A geometry is always defined in a coordinate system but in the case of geospatial 
data we talk more properly of Spatial Referece Systems:
*)

let _,sr = layer.GetSpatialRef().ExportToWkt()

(*** define-output:printSr ***)
printfn "%s" sr
(*** include-output:printSr ***)

(**
<a name="deedleFrame"></a>Visualize the non geometric data in a deedle frame
------------------------
[Deedle](http://bluemountaincapital.github.io/Deedle/) is an F# library for data 
exploration and manipulation. FSharp.Gdal wants to be deedle friendly and while 
it does not reference directly this library it provides the function `toValues` 
to immediately convert vector geospatial data in a deedle frame:
*)

open Deedle

let fmItalyRegion = 
    layer
    |> Vector.toValues
    |> Frame.ofValues
(*** include-value:fmItalyRegion ***)

(**
As we can see the data converted in a deedle frame are more readable and we can take 
advantage of the library capabilities to make aggregations, pivoting and scientific 
and statistical calculations on the data.
*)

(**
<a name="osm"></a>Getting and Querying Open Street Map Data
========================
As pointed in the beginning GDAL/OGR library has the capabilites of access not only file datasources 
as shapefiles, but also RDBMSes, web services, etc. In this section we can see how to use it to 
get data from the OpenStreetMap.

OpenStreetMap is one of largest (probably the largest accessible one) source of geospatial data. It 
provides not only a free map accessible from the website [Openstreetmap.org](https://www.openstreetmap.org) 
but also an [Editing API](http://wiki.openstreetmap.org/wiki/API_v0.6) for fetching and saving raw geodata 
from/to the OpenStreetMap database and 
an [Overpass API](http://wiki.openstreetmap.org/wiki/Overpass_API) which provides read-only API access.

In this section we will see how to

1. fetch data from the Overpass API using the methods in the `FSharp.Data` library 
2. save the data to a file (standard .NET methods)
3. querying the saved data mixing the GDAL/OGR library's methods with F# and in particular with the Deedle library
*)

(**
The first thing is to download the data. In this case we want all the streets 
in Malesco (one of the "biggest" city centre at the border of the Val Grande National 
Park).

I won't go much into the deatils of the query syntax here which can be found on the wiki site 
[Overpass API](http://wiki.openstreetmap.org/wiki/Overpass_API) but just to be clear 
we are querying for all the ways (streets, waterway, etc.): `way[name=*]`;in the bounding 
box containing Malesco city: `[bbox=8.4872,46.1220,8.5140,46.1331]`
*)

open FSharp.Data

//bbox = left, bottom, right, top (min long, min lat, max long, max lat)

let baseUrl = "http://overpass.osm.rambler.ru/cgi/xapi?way[name=*][bbox=8.4872,46.1220,8.5140,46.1331]"

// Download the content
let osm = Http.RequestString(baseUrl)

(**
Then we save the downloaded data to a new file:
*)

let fileName = __SOURCE_DIRECTORY__ + @"\data\malescoWays.osm"

let save  (fileName:string) (data:string) = 
    use w = new System.IO.StreamWriter(fileName)
    w.Write(data)

osm |> save fileName

(**
Now we can inspect the data using the GDAL/OGR library.

First we open the datasource and get the layers inside it:
*)

let osmDataSource = OGR.Ogr.Open(fileName, 0)

(*** hide ***)
let osmDataSourceContents = 
    osmDataSource
    |> Vector.datasourceInfo

let printOsmDataSourceContents = sprintf "%A" osmDataSourceContents
(*** include-value:printOsmDataSourceContents ***)

(**
We have five layers respectively of wkbPoint, wkbLineString, wkbMultiLineString, 
wkbMultiPolygon, wkbGeometryCollection geometry type and as we can see the 
third and fifth layers are empty.

We are interested in the streets so let's convert the second wkbLineString layer 
in a deedle frame to see its content:
*)

let osmLayers = 
    osmDataSource 
    |> Vector.layers

let fmMalescoWays = 
    osmLayers
    |> Seq.item 1
    |> Vector.toValues
    |> Frame.ofValues
(*** include-value:fmMalescoWays ***)

(**
We can see also that this layer contains ways of different kind: only the records 
with the a value in the column HIGHWAY are streets so let's see the list of its 
distinct values:
*)

let highways = 
    fmMalescoWays.GetColumn<string>("HIGHWAY").Values 
    |> Seq.distinct
    |> List.ofSeq
(*** include-value:highways ***)

(**
We can now filter just the streets of residential type:
*)

let fmMalescoStreets = 
    fmMalescoWays 
    |> Frame.filterRowValues 
        (
            fun row -> row.GetAs<string>("HIGHWAY") = "residential"
        )
(*** include-value:fmMalescoStreets ***)

(**
<a name="references"></a>References
------------------------

- [Deedle](http://bluemountaincapital.github.io/Deedle/)
- [Italian National Institute of Statistics (Istat)](http://www.istat.it/it/archivio/24613/) 
- [OGR API Tutorial](http://www.gdal.org/ogr_apitut.html)
- [Openstreetmap.org](https://www.openstreetmap.org) 
- [Overpass API](http://wiki.openstreetmap.org/wiki/Overpass_API)
- [Python GDAL/OGR Cookbook](http://pcjericks.github.io/py-gdalogr-cookbook/vector_layers.html)

*)