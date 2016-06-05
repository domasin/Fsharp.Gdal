(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.

// this is for the future not to include an image but generating it directly in the code
//FSharp.Gdal.GdalConfiguration.Configure() |> ignore
//
//let municipalities =  Ogr.OpenShared(__SOURCE_DIRECTORY__ + @".\data\municipalities.shp",0)
//let vlay = municipalities.GetLayerByIndex(0)
//let feature = vlay |> Vector.features |> List.head
//let geom = feature.GetGeometryRef()
//let env = ref (new Envelope())
//geom.GetEnvelope(!env)

#load "packages/FsLab/FsLab.fsx"

//#I "../bin/FSharp.Gdal"
#I "../src/FSharp.Gdal/bin/Debug"

(**
Land use in the Big Valley
========================
Exploring gdal's rasters functions
------------------------
*)

#r "FSharp.Gdal.dll"
#r "gdal_csharp.dll"
#r "gdalconst_csharp.dll"
#r "ogr_csharp.dll"
#r "osr_csharp.dll"

open System
open System.IO
open System.Windows.Forms
open System.Drawing

open Deedle
open FSharp.Data
open FSharp.Charting

open OSGeo.GDAL
open OSGeo.OGR

open FSharp.Gdal
open FSharp.Gdal.UM
open FSharp.Gdal.Raster

(**
For this section I will use data from the [Corine Land Cover](http://www.eea.europa.eu/data-and-maps/data/corine-land-cover-2006-raster) 
provided by the European Enivronment Agency clipped (with an external program for now) to the extension of all the municipalities that have part of 
their land in the [Big Valley National Park](https://en.wikipedia.org/wiki/Val_Grande_National_Park).
![Big Valleys Municipalities](/Fsharp.Gdal/img/big_valleys_commons.png "Big Valleys Municipalities")
*)

(**
The clipped raster looks like this:
*)

let image = new Bitmap(__SOURCE_DIRECTORY__ + "\data\clc_valgrande.tif")
let form = new Form(Visible = true, TopMost = true, Width = 700, Height = 500, 
                    BackgroundImage = image, BackgroundImageLayout = ImageLayout.Center)
form.Show()

(** ![Corinne Land Cover](/Fsharp.Gdal/img/clc_valgrande.png "Corinne Land Cover")*)

(**
We call the `Configuration.Init()` to configure the gdal library
and then open the raster:
*)

Configuration.Init() |> ignore

let dataset = Gdal.OpenShared(__SOURCE_DIRECTORY__ + @".\data\clc_valgrande.tif", Access.GA_ReadOnly)

(**
Getting Dataset Information
*)

(*** define-output:datasetInfo ***)
let mutable (geotransform:float[]) = [|0.;0.;0.;0.;0.;0.|]
dataset.GetGeoTransform(geotransform)
let topLeftX                    = geotransform.[0]
let topLeftY                    = geotransform.[3]
let westEastPixelResolution     = geotransform.[1] * 1.<m>
let northSouthPixelResolution   = Math.Abs(geotransform.[5]) * 1.<m> // take absolute of negative number
let xSize = dataset.RasterXSize
let ySize = dataset.RasterYSize 
let bands = dataset.RasterCount

printfn "Driver = %s / %s" (dataset.GetDriver().ShortName) (dataset.GetDriver().LongName)
printfn "Size = X:%i x Y:%i x Bands:%i" xSize ySize bands
printfn "Projection = %s..." (dataset.GetProjection().ToString().Substring(0,110))
printfn "Origin = (%f,%f)" topLeftX topLeftY
printfn "Pixel Size = (%f,%f)" westEastPixelResolution northSouthPixelResolution
(*** include-output:datasetInfo ***)

(**
In particular the pixel resolution is of about 100m x 100m so a square in the grid 
equals about 1ha and we can calculate the total size of the area covered by the 
file multiplying this value by the number of pixels.
*)

(*** define-output:area ***)
let pixelArea = westEastPixelResolution * northSouthPixelResolution * mqToHa
let totalArea = pixelArea * (xSize * ySize |> float)

printfn "pixelArea : float<ha> = %A\ntotalArea : float<ha> = %A" pixelArea totalArea
(*** include-output:area ***)

(**
Corine Land Cover stores for each pixel a number that reppresents the 
type of land use in that area. 0 represents the white area in the picture above 
for which there is no information beacuase I clipped the raster to just the 
municipalty limits.

The European Environment Agency gives a legend of the number meanings 
with an excel file.

Now what I want to do is to extract the category number 
of land use for each pixel coordinate from the file.
*)

(*** define-output:values ***)
let values = 
    [for x in 0..(xSize-1) ->
        [for y in 0..(ySize-1) -> 
            (x,y),(getImgValue dataset (x,y))]]
    |> List.concat

printf "%s" (values.ToString())
(*** include-output:values ***)

(**
The values are represented here as a tuple of pixel's coordinate and their value.
To get some insight let's group all the pixel and count them by their value. 
From the number of pixels and the pixel's area we can calculate the area in hectars 
occupied by each category.

With this information we populate a Deedle Frame.
*)

let hectarsByCategory = 
    values
    |> List.groupBy (fun (c,v) -> v)
    |> List.map (fun (v,xs) -> 
            v.Value |> string, 
            "Hectars", 
            (xs.Length |> float) * pixelArea
        )
    |> List.sortByDescending (fun (v,c,l) -> l)
    |> Frame.ofValues

(*** include-value:hectarsByCategory ***)

(**
Just to check we can see that the sum is still equivalent 
with the total area we've calculated before:
*)

(*** define-output:sumHectars ***)
let sumHectars = hectarsByCategory.Sum()?Hectars

printf "sumHectars = %f" sumHectars
(*** include-output:sumHectars ***)

(**
Now we want to decode the category number in meaningfull description.

For this puprose I will create another frame with the information stored in the corine legend 
indexing the frame by the GRID_CODE column which corresponds to the code number 
stored in the image file.
*)

let corineLegend = 
    Frame.ReadCsv(__SOURCE_DIRECTORY__ + "./data/clc_legend.csv",separators = ";")
    |> Frame.indexRowsString "GRID_CODE"

(*** include-value:corineLegend ***)

(**
And finally join the two frames:
*)

let landUse = hectarsByCategory.Join(corineLegend, kind=JoinKind.Left)

(*** include-value:landUse ***)

(**
Our frame now contains the total hectars for each category in the corine legend 
that has some value in the image file together with a meaningfull description..

We don't have any description for the category number 0 but this is ok beacuse we want 
just the information in the municipality limits that intersect the Big Valley National Park.
*)

(**
The corine land cover legend has a hierarchical structure which we can 
exploit together with deedle's functions to make some analysis. 

Just to get a taste we can aggregate all the hectars by the main level 
in the legend.
*)

let grossLandUse = 
    landUse
    |> Frame.aggregateRowsBy ["LABEL1"] ["Hectars"] Stats.sum
    |> Frame.indexRowsString "LABEL1"
    |> Frame.sortRowsWith "Hectars" (fun _ c -> -c)

(*** include-value:grossLandUse ***)

(**
And to visualize the proportions let's plot it in a chart:
*)

(*** define-output:chart ***)
Chart.Pie(grossLandUse?Hectars |> Series.observations, Name = "Big Valley's gross land use") 
(*** include-it:chart ***)