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
open FSharp.Gdal.Vector

(**
Vector Layers
========================
This section will use the [OGR API Tutorial](http://www.gdal.org/ogr_apitut.html) as the first reference 
to show how to use OGR classes to read and write data from a geospatial datasource.
*)

(**
Configure gdal
------------------------
Initially it is necessary to register all the format drivers that are desired. The FSharp.Gdal library 
accomplishes this calling the function `Configuration.Init()`:
*)

Configuration.Init()

(**
Check registered drivers
------------------------
To check that all OGR drivers are correctly registered we call:
*)

(*** define-output:ogrDrivers ***)
Configuration.printOgrDrivers()
(*** include-output:ogrDrivers ***)

(**
Open the datasource
------------------------
Next we need to open the input OGR datasource. Datasources can be files, RDBMSes, directories full of files, 
or even remote web services depending on the driver being used. However, the datasource name is always a single string. 

In this case we will open a shapefile provided by the 
[Italian National Institute of Statistics (Istat)](http://www.istat.it/it/archivio/24613/) 
containing the administrative limits of italian regions. 
*)

open OSGeo

let italyShp = __SOURCE_DIRECTORY__ + @"\data\reg2001_g\reg2001_g.shp"
let driver = OGR.Ogr.GetDriverByName("ESRI Shapefile")
let ds = driver.Open(italyShp, 0)

(**
Inspecting the layers
------------------------
A GDALDataset can potentially have many layers associated with it. 

As a GDALDataset can be viewed as a database, a layer in it can be thought of as 
a database table containing records (features) with the same columns definition (fields) 
and a particular type of Geometry.

The number of layers available can be queried with 
`GetLayerCount()` and individual layers fetched by index using `GetLayerByIndex`.
*)

(*** define-output:layersCount ***)
let layersCount = ds.GetLayerCount()

printfn "There is %i layer" layersCount
(*** include-output:layersCount ***)

let layer0 = ds.GetLayerByIndex(0)

(**
One of the goals of this project is to develop functions to make it easier 
(and more idiomatic) to work with OGR/GDAL library in F#.

In this particulare case instead of using the low level GDAL methods we can use an utility 
function `layers` defined in the `FSharp.Gdal.Vector` module which returns the layers 
in a Vector DataSource as a list.

Below I will use more of these utility functions whose definitions can be viewd directly 
in the source code of the 
[Vector module](https://github.com/domasin/Fsharp.Gdal/blob/master/src/Fsharp.Gdal/Vector.fs).
*)

let layers = ds |> Vector.layers
(*** include-value:layers ***)

(**
As we can see the output is not very informative and just says that there is one single layer 
in the datasource. The `Vector` module provides the function `contents` to immediately inspect 
a layer content:
*)

let layerContents = 
    layers
    |> List.map (fun ly -> ly |> Vector.contents)

(*** define-output:printLayerContents ***)
printfn "%s" (layerContents.ToString())
(*** include-output:printLayerContents ***)

(**
This is better: now we can see that the layer has 20 features in it, 
is a layer of points and has three attributes: COD_REG, REGIONE and POP2001.

The function is also suitable to add a custom printer for the `OGR.Layer` type 
if we are working in F# Interactive:
*)

fsi.AddPrintTransformer (fun (layer:OGR.Layer) -> box (layer |> Vector.contents))

(**
Get layers features
------------------------
A layer contains a set of features that can be viewed as records of a database table.

Again, I will use an utility function `features` that returns the features stored in 
a vector layer as an F# list:
*)

let layer = layers |> List.head
let features = layer |> Vector.features

(**
Get layers attributes
------------------------
A layer has a definition containing attributes that can be viewed as columns of a table. 

Here the function `fields` returns a tuple of all the index, name and type of the field 
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
Get geometries of all features in a layer
------------------------
In addition to attributes a feature contains a geometry field.

In this case each feature corresponds to an italian region. 
So We can collect all features' geometries in a geometry collection and plot 
it to get a map of italy divided by regions:
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
Visualize the non geometric data in a deedle frame
------------------------
To visualize the layers non geometric data the `Vector` module 
provides the utility function `toValues` to convert the layer 
in a deedle frame:
*)

open Deedle

let fmItalyRegion = 
    layer
    |> Vector.toValues
    |> Frame.ofValues
(*** include-value:fmItalyRegion ***)

(**
Getting and Querying Open Street Map Data
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

let osmLayers = 
    osmDataSource 
    |> Vector.layers

(*** hide ***)
let osmLayersContents = 
    osmLayers
    |> List.map (fun ly -> ly |> Vector.contents)

let printOsmLayersContents = sprintf "%A" osmLayersContents
(*** include-value:printOsmLayersContents ***)

(**
We have six layers respectively of wkbPoint, wkbLineString, wkbMultiLineString, 
wkbMultiPolygon, wkbGeometryCollection geometry type and as we can see the 
third and sixth layers are empty.

We are interested in the streets so let's convert the second wkbLineString layer 
in a deedle frame to see its content:
*)

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

