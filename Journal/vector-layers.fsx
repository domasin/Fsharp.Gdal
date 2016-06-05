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
Vector Layers
========================
This section will use the [OGR API Tutorial](http://www.gdal.org/ogr_apitut.html) as the principal reference 
to show how to use OGR classes to read and write data from a file.
*)

(**
Configure gdal
------------------------
Initially it is necessary to register all the format drivers that are desired. The FSharp.Gdal library 
accomplishes this calling the function `Configuration.Init()`:
*)

Configuration.Init() |> ignore

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
a database table containing records with the same columns definition.

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
function `layers` defined in the `FSharp.Gdal.Vector` moduel which returns the layers 
in a Vector DataSource as a list.

Below I will use more of these utility functions whose definitions can be viewd directly 
in the source code of the 
[Vector module](https://github.com/domasin/Fsharp.Gdal/blob/master/src/Fsharp.Gdal/Vector.fs).
*)

let layers = ds |> Vector.layers
(*** include-value:layers ***)

let layer = layers |> List.head

(**
Get layers features
------------------------
A layer contains a set of features that can be viewed as records of a database table.

Again, I will use an utility function `features` that returns the features stored in 
a vector layer as an F# list:
*)

let features = layer |> Vector.features
let featuresCount = features.Length

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

for feat in features do 
    let regionName = feat.GetFieldAsString(1)
    printfn "%s" regionName

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