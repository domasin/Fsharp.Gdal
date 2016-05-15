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
#r "System.Xml.Linq"
#r "Fsharp.Gdal.dll"
#r "gdal_csharp.dll"
#r "gdalconst_csharp.dll"
#r "ogr_csharp.dll"
#r "osr_csharp.dll"

open System
open System.Collections
open System.Collections.Generic
open System.IO
open System.Xml.Linq

open Deedle
open FSharp.Data
open FSharp.Charting

open OSGeo
open OSGeo.OGR
open OSGeo.GDAL
open OSGeo.OSR

open FSharp.Gdal
open FSharp.Gdal.UM

(**
In this section I want to explore a gpx trace of a trekking I made on 
10 April 2016 to one of the central Valgrande mountains named "Cima Sasso".

I want to calculate the actual duration and compare it with an extimated 
walk time based on trobbler's hiking function.

Then I will use the Gdal's function to access a shape file containing 
the foot paths to the main peaks in the Val Grande Natinal Parks
and I will calculate an extimated time for all of them based on what I've 
learned from the gpx analysis.

I start loading the gpx file with the Xml Type Privider and see its content. 
To correctly instruct the provider I give a sample with two tracks two track segments 
for tracks and two points for each segments even if my gpx contains only one 
track with one track segments in it. I do this because in general a gpx can 
contain more then one of these elements.
*)

type Gpx = (*[omit:XmlProvider<...>]*)XmlProvider<"""<?xml version="1.0" encoding="utf-8"?><gpx xmlns:tc2="http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:tp1="http://www.garmin.com/xmlschemas/TrackPointExtension/v1" xmlns="http://www.topografix.com/GPX/1/1" version="1.1" creator="TC2 to GPX11 XSLT stylesheet" xsi:schemaLocation="http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd http://www.garmin.com/xmlschemas/TrackPointExtension/v1 http://www.garmin.com/xmlschemas/TrackPointExtensionv1.xsd"><trk><name>2016-04-10T06:24:49Z</name><trkseg><trkpt lat="46.0030837" lon="8.4914856"><ele>734.5847168</ele><time>2016-04-10T06:24:49Z</time></trkpt><trkpt lat="46.0030837" lon="8.4914856"><ele>734.5847168</ele><time>2016-04-10T06:24:49Z</time></trkpt></trkseg><trkseg><trkpt lat="46.0030837" lon="8.4914856"><ele>734.5847168</ele><time>2016-04-10T06:24:49Z</time></trkpt><trkpt lat="46.0030837" lon="8.4914856"><ele>734.5847168</ele><time>2016-04-10T06:24:49Z</time></trkpt></trkseg></trk><trk><name>2016-04-10T06:24:49Z</name><trkseg><trkpt lat="46.0030837" lon="8.4914856"><ele>734.5847168</ele><time>2016-04-10T06:24:49Z</time></trkpt><trkpt lat="46.0030837" lon="8.4914856"><ele>734.5847168</ele><time>2016-04-10T06:24:49Z</time></trkpt></trkseg><trkseg><trkpt lat="46.0030837" lon="8.4914856"><ele>734.5847168</ele><time>2016-04-10T06:24:49Z</time></trkpt><trkpt lat="46.0030837" lon="8.4914856"><ele>734.5847168</ele><time>2016-04-10T06:24:49Z</time></trkpt></trkseg></trk></gpx>""">(*[/omit]*)

let content = 
    use sr = new StreamReader (__SOURCE_DIRECTORY__ + "./data/10_04_2016 08_24_49_history.gpx")
    sr.ReadToEnd()

let gpx = Gpx.Parse(content)

(**
The gpx sotres just one track named with the date I made the trecking:
*)

(*** define-output:trackName ***)
let trackCounts = gpx.Trks.Length

let trackName = gpx.Trks.[0].Name

printfn "Number of tracks: %i\nTrack name: '%A'" trackCounts trackName
(*** include-output:trackName ***)

(** 
The gpx can be converted in a linestring:
*)

Configuration.Init() |> ignore

let line = new OGR.Geometry(OGR.wkbGeometryType.wkbLineString)

for trkpt in gpx.Trks.[0].Trksegs.[0].Trkpts do 
    line.AddPoint(trkpt.Lon |> float, trkpt.Lat |> float, trkpt.Ele |> float)

(** 
Gpx stores points in EPSG:4326 but is better to convert them in 
EPSG:32632 to get lenghts in meters from gdal library
*)

// input SpatialReference
let inSpatialRef = new OSR.SpatialReference("")
inSpatialRef.ImportFromEPSG(4326)

// output SpatialReference
let outSpatialRef  = new OSR.SpatialReference("")
outSpatialRef.ImportFromEPSG(32632)

line.AssignSpatialReference(inSpatialRef)
let _ = line.TransformTo(outSpatialRef)

let length = line.Length()

(** 
We can plot it in a graphical visualization:
*)

(*** define-output:chartLineString ***)
let env = 
    let res = new OGR.Envelope()
    line.GetEnvelope(res)
    res

let coordinates = 
    let last = line.GetPointCount() - 1
    [
        for i in 0..last ->
            let p = [|0.;0.|]
            line.GetPoint(i,p)
            (p.[0], p.[1])
    ]

Chart.Line(coordinates)
    .WithXAxis(Max=env.MaxX,Min=env.MinX)
    .WithYAxis(Max=env.MaxY,Min=env.MinY)
(*** include-it:chartLineString ***)

(**
Now I need to extract the following metrics for 
each trakcpoint: elevation, distance from previous point, 
total distance, slope, track time, duration from previous point, 
actual velocity, extimated velocity based on trobbler's hiking 
function.

The linestring is not really convenient because I loose the information 
of the time associated to each track point.

I need a more structured Data Type:
*)

type Info = 
    {
        Lat : float<g> 
        Lon : float<g>
        Elev : float<m>
        Distance : float<m>
        Slope : float
        TrackTime : System.DateTime
        ActualDuration : float<s>
        ActualVelocity : float<km/h>
        ExtimatedlVelocity : float<km/h>
        ExtimatedDuration : float<s>
    }

(** Then I populate a collection of Infos.*)

type Point = {Geom : Geometry; Time : DateTime}

let trobblersHikingFunction slope = 
    6. * Math.Exp(-3.5 * Math.Abs(slope + 0.05))

let infos = 
    gpx.Trks.[0].Trksegs.[0].Trkpts
    |> List.ofArray
    |> List.map (fun trkpt -> 
            let point = new OGR.Geometry(OGR.wkbGeometryType.wkbPoint)
            point.AddPoint(trkpt.Lon |> float, trkpt.Lat |> float, trkpt.Ele |> float)
            point.AssignSpatialReference(inSpatialRef)
            let _ = point.TransformTo(outSpatialRef)
            {Geom = point; Time = trkpt.Time}
        )
    |> List.pairwise
    |> List.map
        (
            fun (prev,curr) -> 
                let dx = prev.Geom.Distance(curr.Geom) * 1.<m>
                let dh = (curr.Geom.GetZ(0) - prev.Geom.GetZ(0)) * 1.<m>
                let slope = dh/dx
                let duration = (curr.Time - prev.Time).TotalSeconds * 1.<s>
                let actualVelocity = dx / duration |> msToKmph
                let extimatedVel = 
                    let tobbler = trobblersHikingFunction slope
                    // floor the extimated velocity at no less then 1.0 km/h
                    let v = Math.Max(tobbler, 1.0)
                    v * 1.0<km/h>
                let extimatedDuration = dx / (extimatedVel |> kmphToMs)
                {
                    Lat                  = curr.Geom.GetX(0) * 1.<g>
                    Lon                  = curr.Geom.GetY(0) * 1.<g>
                    Elev                 = curr.Geom.GetZ(0) * 1.<m>
                    Distance             = dx
                    Slope                = slope
                    TrackTime            = curr.Time
                    ActualDuration       = duration
                    ActualVelocity       = actualVelocity
                    ExtimatedlVelocity   = extimatedVel
                    ExtimatedDuration    = extimatedDuration
                }
        )

(**
We can convert this in a frame to visualize the informations extracted:
*)

let infosFrame = 
    infos
    |> Frame.ofRecords

(*** include-value:infosFrame ***)

(**
The Deedle frame provide us with functions, among the others, to aggregate the data 
and to see for example the total distance of the track:
*)

(*** define-output:totalDistance ***)
let totalDistance = infosFrame?Distance.Sum()

printfn "TotalDistance = %f" totalDistance
(*** include-output:totalDistance ***)

(** 
The total length is still equivalent to that calculated for the linestring.
*)

(**
and just for fun we can also visualize the eleveation profile:
*)

(*** define-output:elevationProfile ***)
let rec distanceElev acc (ys:(float<m> * float<m>) list) xs = 
    match xs with
    | [] -> ys
    | p::tail -> 
        let length = p.Distance + acc
        tail |> distanceElev length ([length, p.Elev]@ys)

let xy = infos |> distanceElev 0.<m> []
Chart.Line([for (x,y) in xy -> (x, y)], "Elevation Profile")

(*** include-it:elevationProfile ***)

(**
Now as for the main subject of this section we want to compare 
the actual duration of the trecking with an extimation based 
on the trobbler's hiking function.

Trobbler's hiking function calculates an expected velocity based on the slope 
of the hiking and extimates a maximum velocity at 5% descendind slope which 
decreases for increasing slopes both descending or ascending.

We can plot the function to visualize its curve:
*)

(*** define-output:trobbler ***)
Chart.Line([for x in -70.0 .. 70.0 -> (x, (trobblersHikingFunction (x/100.)))])
(*** include-it:trobbler ***)

(**
The chart shows velocity on the y axis depending from the slope on x axis.
Let's plot the actual velocity from the gpx points:
*)

(*** define-output:trackmetricsChart ***)
Chart.Point([for p in infos -> (p.Slope * 100., p.ActualVelocity)])
(*** include-it:trackmetricsChart ***)

(**
Slopes above 200% (descending or ascending) make the chart too confused so let's filter them out:
*)

(*** define-output:trackmetricsFiltered ***)
Chart.Point([for p in (infos |> List.where(fun x -> Math.Abs(x.Slope) < 2.)) -> (p.Slope * 100., p.ActualVelocity)])
(*** include-it:trackmetricsFiltered ***)

(**
It looks better and we can combine it with the plot of the function to see the correlation:
*)

(*** define-output:combined ***)
Chart.Combine
    [
        Chart.Point([for p in (infos |> List.where(fun x -> Math.Abs(x.Slope) < 2.)) -> (p.Slope * 100., p.ActualVelocity)])
        Chart.Line([ for x in -200.0 .. 200.0 -> (x, (trobblersHikingFunction (x/100.))) ])
    ]
(*** include-it:combined ***)

(**
And finally compare the actual duratio with the extimated one:
*)

(*** define-output:actualVsExtimated ***)
let actualDuration = infosFrame?ActualDuration.Sum() * 1.<s> * UM.secToHr
let extimatedDuration = infosFrame?ExtimatedDuration.Sum() * 1.<s> * UM.secToHr

printfn "Actual Duration = %f\nExtimated Duration = %f" actualDuration extimatedDuration
(*** include-output:actualVsExtimated ***)

(**
To generlize the subject now I will calculate an extimation time for all the 
foot paths stored in a shape file.

To access the data of a shape file we'll use the methods provided by the 
Gdal Library. Fsharp.Gdal now contains a module that cofigures the path needed by gdal and registers 
all its provider.

*)

(**
Extract my favourite paths in the bigvalley.
*)

let dataset =  Ogr.OpenShared(__SOURCE_DIRECTORY__ + @".\data\itinerari.shp",1)

let vlay = dataset.GetLayerByIndex(0)

vlay.GetSpatialRef().__str__()

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
