(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#load "packages/FsLab/FsLab.fsx"

//#I "../bin/Fsharp.Gdal"
#I "../src/Fsharp.Gdal/bin/Debug"

#r "Fsharp.Gdal.dll"
#r "gdal_csharp.dll"
#r "gdalconst_csharp.dll"
#r "ogr_csharp.dll"
#r "osr_csharp.dll"

(**
Gdal type provider
========================
This is a starting point in developing a type provider for geospatial data files managed with the gdal library
*)

open FSharp.Gdal

open OSGeo
open OSGeo.GDAL
open OSGeo.OGR

open Deedle
open FSharp.Data
open FSharp.Charting

(**
To get the data from a file we can just call the type provider constructor giving 
the file path as the type parameter of the provider. Internally the provider calls 
the logic needed to configure the gdal library on the system and to initialize all 
the gdal data providers:
*)

let vLayer = new OgrTypeProvider<"G:/Data/itinerari.shp">()

(**
The type provider provides two properties:  

`Values` which we can convert directly 
to a deedle frame to explore immediately the data in the vector:
*)

let fmData = vLayer.Values |> Frame.ofValues
(*** include-value:fmData ***)

(**
and `Features` which we can iterate to get, for each feature, attributes and geometry 
converted in dinamically created types that match the type of geometry and attributes 
and come up directly with the intellisense in the iteractive editor:
*)

(*** define-output:printAttributes ***)
for feat in vLayer.Features do
    printfn "Geomtry type = %A\tID = %i\tTITLE = %s\t" 
        (feat.Geometry.GetGeometryType()) feat.ID feat.TITLE
(*** include-output:printAttributes ***)

(**
With the deedle frame and gdal methods at hand we can do some calculations based on geometries.
The data file contains hiking paths in the Big Valley 
and we can for example get the lenght of each with the gdal library methods 
and use the deedle functions to aggregate them.
*)

let fmLengths = 
    vLayer.Features
    |> Seq.mapi (fun i feat -> i, "Length", feat.Geometry.Length())
    |> Frame.ofValues

let fmWithLengths = fmData.Join(fmLengths)
(*** include-value:fmWithLengths ***)


(**
And just to do an aggregation:
*)

(*** define-output:sumLenghts ***)
let sumLenghts = fmWithLengths.Sum()?Length

printf "sumLenghts = %f" sumLenghts
(*** include-output:sumLenghts ***)