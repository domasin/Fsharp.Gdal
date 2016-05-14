(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#load "packages/FsLab/FsLab.fsx"

//#I "../bin/Fsharp.Gdal"
#I "../src/Fsharp.Gdal/bin/Debug"

(**
Extimate walk time for routes stored in shape file
========================
*)
#r "Fsharp.Gdal.dll"
#r "gdal_csharp.dll"
#r "gdalconst_csharp.dll"
#r "ogr_csharp.dll"
#r "osr_csharp.dll"

open System
open System.Collections
open System.Collections.Generic
open System.IO

open FSharp.Charting

open OSGeo.OGR
open OSGeo.GDAL
open OSGeo.OSR

open FSharp.Gdal
open FSharp.Gdal.UM

(**
In this section I want to access the data of a shape file using the methods provided by the 
Gdal Library. Fsharp.Gdal now contains a module that cofigures the path needed by gdal and registers 
all its provider.

To do this just call the `GdalConfiguration.Configure` method:
*)

Configuration.Init() |> ignore

(**
Extract my favourite paths in the bigvalley.
*)

let dataset =  Ogr.OpenShared(__SOURCE_DIRECTORY__ + @".\data\itinerari.shp",1)

let vlay = dataset.GetLayerByIndex(0)

let features = 
    vlay
    |> Vector.features
    |> List.filter (fun f -> f <> null)

(**
Let's see the fields of shape file.
*)

let fields = 
    vlay
    |> Vector.fields

(*** include-value:fields ***)

(**
Take the Cima Sasso feature (Cima Sasso = Stone Peak)
*)

let cimaSasso = 
    features
    |> List.where (fun f -> 
        let title = f.GetFieldAsString(3)
        title = "Cima Sasso")
    |> List.head

(**
I know that my vector lines don't have elevation information.
So let's open an elevation raster of the bigvalley.
*)

let rDataset = Gdal.OpenShared(__SOURCE_DIRECTORY__ + @".\data\dem20_valg.tif", Access.GA_ReadOnly)

let mutable (geotransform:float[]) = [|0.;0.;0.;0.;0.;0.|]
rDataset.GetGeoTransform(geotransform)

(**
Now I extract points from my track and calculate information
*)

type InfoTrack = 
    { 
        Point:Geometry; 
        Elev:float; 
        Distance:float; 
        Slope:float; 
        ExtimatedForwardVelocity:float<km/h>;
        ExtimatedForwardTime:float<s>;
        ExtimatedReverseVelocity:float<km/h>;
        ExtimatedReverseTime:float<s>; 
    }

let trackRecs = 
    let geom = cimaSasso.GetGeometryRef()
    geom
    |> Vector.points
    |> List.map 
        (
            fun (p) -> 
                let x,y = (p.GetX(0), p.GetY(0))
                let elev = 
                    (x,y)
                    |> Raster.groundToImage geotransform
                    |> Raster.getImgValue geotransform rDataset
                match elev with
                | Some(e) -> (p, e)
                | None -> (p, 0.)
        )
    |> List.pairwise
    |> List.map 
        (
            fun ((prevPoint,prevElev),(currPoint,currElev)) -> 
                let dx = currPoint.Distance(prevPoint)
                let dh = currElev - prevElev
                let slope = dh/dx
                // tobbler's hiking function
                let tobbler slope = (6. * Math.Exp(-3.5 * Math.Abs(slope + 0.05))) 
                let extimatedForwardVel = 
                    // floor the extimated velocity at 1.0 km/h as 
                    // I saw from the experiments with gpx that works
                    let v = Math.Max(tobbler slope, 1.0)  
                    v * 1.0<km/h>
                let extimatedReverseVel = 
                    // floor the extimated velocity at 1.0 km/h
                    let v = Math.Max(tobbler -slope, 1.0)  
                    v * 1.0<km/h>
                let extimatedForwardTime = (dx * 1.0<m>) / (extimatedForwardVel |> kmphToMs)
                let extimatedReversTime = (dx * 1.0<m>) / (extimatedReverseVel |> kmphToMs)
                { 
                    Point = currPoint; 
                    Elev = currElev; 
                    Distance = dx; 
                    Slope = slope; 
                    ExtimatedForwardVelocity = extimatedForwardVel;
                    ExtimatedForwardTime = extimatedForwardTime; 
                    ExtimatedReverseVelocity = extimatedReverseVel;
                    ExtimatedReverseTime = extimatedReversTime; 
                }
        )

(*** define-output:extimatedTime ***)
let extimatedTime = 
    (trackRecs
     |> List.fold (fun acc rc -> acc + rc.ExtimatedForwardTime + rc.ExtimatedReverseTime) 0.<s>) *
    secToHr

printf "extimatedTime = %f" extimatedTime
(*** include-output:extimatedTime ***)
