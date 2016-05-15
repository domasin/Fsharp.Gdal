(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#load "packages/FsLab/FsLab.fsx"
#load "plot-geometries.fsx"
open ``Plot-geometries``

//#I "../bin/Fsharp.Gdal"
#I "../src/Fsharp.Gdal/bin/Debug"

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
Extimate walk time for routes stored in shape file
========================
*)

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
Gpx stores points in EPSG:4326 but is better to convert in EPSG:32632 to get lenghts in meters from gdal library
*)

// input SpatialReference
let inSpatialRef = new OSR.SpatialReference("")
inSpatialRef.ImportFromEPSG(4326)

// output SpatialReference
let outSpatialRef  = new OSR.SpatialReference("")
outSpatialRef.ImportFromEPSG(32632)

line.AssignSpatialReference(inSpatialRef)
let _ = line.TransformTo(outSpatialRef)

(** 
With the gpx track converted in a line string we can obtain its length with the 
Gdal's functions:
*)

let length = line.Length()
(*** include-value:length ***)

(** 
And taking advantage of the function `plot` defined in [Plot Geometries](plot-geometries.html) 
we can graphically visualize its shape:
*)

(*** define-output:chartLineString ***)
line |> plot
(*** include-it:chartLineString ***)

(**
The length of the path is an important variable to calculate an extimation of the 
walk time but anohter important variabile is its elevation profile.

Infact, normally a person walks at about 5 km/h in plain but the velocity decreases both 
ascending and descending on a steep path like the one in object. 

It is usefull to extract the elevation profile of the track. 

For this first we combine the distance from the start of the track with the current elevation
*)

let rec distanceElev (geom:OGR.Geometry) = 
    let mutable distance = 0.<m>
    let last = geom.GetPointCount() - 1
    [
        for i in 0..last ->
            let point = new OGR.Geometry(OGR.wkbGeometryType.wkbPoint)
            point.AddPoint(geom.GetX(i) |> float, geom.GetY(i) |> float, geom.GetZ(i) |> float)
            point.AssignSpatialReference(outSpatialRef)
            point
    ]
    |> List.pairwise
    |> List.map
        (
            fun (prev,curr) -> 
                distance <- distance + prev.Distance(curr) * 1.<m>
                distance,curr.GetZ(0) * 1.<m>
        )

(**
and then we plot the tuples on a chart:
*)

(*** define-output:elevationProfile ***)
let xy = line |> distanceElev
Chart.Line([for (x,y) in xy -> (x, y)], "Elevation Profile")
(*** include-it:elevationProfile ***)

(**
So knowing the elevation of each point, how we can predict at which velocity a normal 
person will walk at each and the calculate the total time multiplying this 
velocity with the distance walked from the previous point? 

Well an attempt to answer this question was given by Trobbler with its "hiking function": 
*)

let trobblersHikingFunction slope = 
    6. * Math.Exp(-3.5 * Math.Abs(slope + 0.05))

(**
Trobbler's hiking function calculates an expected velocity based on the slope 
of the hiking and extimates a maximum velocity at 5% descendind slope which 
decreases for increasing slopes both descending or ascending.

We can plot the function to visualize its curve:
*)

(*** define-output:trobbler ***)
Chart.Line([for x in -70.0 .. 70.0 -> (x, (trobblersHikingFunction (x/100.)))])
(*** include-it:trobbler ***)

(**
I want to use this function and extract the following metrics for 
each point in the track: elevation, distance from previous point, 
total distance, slope, track time, duration from previous point, 
actual velocity, extimated velocity based on trobbler's hiking 
function.

The linestring is not really convenient because looses the information 
of the time associated to each track point and it does not give me 
fields to store every information.

So I define a more structured Data Type:
*)

type Info = 
    {
        Geom : OGR.Geometry
        Distance : float<m>
        Slope : float
        TrackTime : System.DateTime
        ActualDuration : float<s>
        ActualVelocity : float<km/h>
        ExtimatedlVelocity : float<km/h>
        ExtimatedDuration : float<s>
    }

(** 
and populate a collection of elements of this type:
*)

type Point = {Geom : Geometry; Time : DateTime}

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
                    Geom                 = curr.Geom
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
Let's plot the actual velocity for each point:
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
It looks better and we can combine it with the plot of the trobbler's hiking function to see the correlation:
*)

(*** define-output:combined ***)
Chart.Combine
    [
        Chart.Point([for p in (infos |> List.where(fun x -> Math.Abs(x.Slope) < 2.)) -> (p.Slope * 100., p.ActualVelocity)])
        Chart.Line([ for x in -200.0 .. 200.0 -> (x, (trobblersHikingFunction (x/100.))) ])
    ]
(*** include-it:combined ***)

(**
Finally compare the actual duration with the extimated one and see that the extimation 
is really similar to the actual time of the track.
*)

(*** define-output:actualVsExtimated ***)
let actualDuration = infosFrame?ActualDuration.Sum() * 1.<s> * UM.secToHr
let extimatedDuration = infosFrame?ExtimatedDuration.Sum() * 1.<s> * UM.secToHr

printfn "Actual Duration = %f\nExtimated Duration = %f" actualDuration extimatedDuration
(*** include-output:actualVsExtimated ***)

(**
To generlize the subject now I will calculate an extimated time for all the 
foot paths stored in a shape file.

The shapefile valgrande_tracks_crosa_lenz.shp stores the paths to the valgrande peaks with the 
time extimated by the book "Valgrande National Park - Paths, history and nature" 
by Paolo Crosa Lenz. The time is probably calculated in an empirical way so will 
be a good term of comparison for our calculation.

To access the data I will use the `OgrTypeProvider` defined in FSharp.Gdal
*)

let valgrandeTracks = new OgrTypeProvider<"G:/Data/valgrande_tracks_crosa_lenz.shp">()
let fmData = valgrandeTracks.Values |> Frame.ofValues
(*** include-value:fmData ***)

(**
The geometries in this shape file don't store elevation so we'll get this information from a dem raster file:
*)

let rDataset = Gdal.OpenShared(__SOURCE_DIRECTORY__ + @".\data\dem20_valg.tif", Access.GA_ReadOnly)

let mutable (geotransform:float[]) = [|0.;0.;0.;0.;0.;0.|]
rDataset.GetGeoTransform(geotransform)

(**
The function `extractMetrics` below caluclates all the metrics we need in a way similar to that 
we used above to populate the `infos` collection from the gpx file but it also extracts 
from the dem file the elevation information with the FSharp.Gdal functions 
`Raster.groundToImage` and `Raster.getImgValue`. The return type of the function is 
a collection of `PointMetrics` (defined below) for each point in the linestrings of the 
paths:
*)

type PointMetrics = 
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

let extractMetrics (geom:OGR.Geometry) = 
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

(**
`sumTime` aggregates the extimated elapsed forward time of each point 
to finally calculate the total extimated time given a collection of `PointMetrics`
*)

let sumTime trackMetrics = 
    let value = 
        (
            trackMetrics
            |> List.fold (fun acc rc -> acc + rc.ExtimatedForwardTime) 0.<s>
        ) * secToHr / 1.<h>
    Math.Round(value, 2)

(**
Given theese functions we can populate a new deedle frame with our extimated time:
*)

let fmMyExtimatedTime = 
    valgrandeTracks.Features
    |> Seq.mapi (fun i feat -> 
        i, 
        "MYTIME", 
        (feat.Geometry |> extractMetrics |> sumTime))
    |> Frame.ofValues
(*** include-value:fmMyExtimatedTime ***)

(**
and join the two frames to compare the values calculating a delta percentage:
*)

let fmWithExtimatedTime = fmData.Join(fmMyExtimatedTime)

fmWithExtimatedTime?``Delta %`` <- 
    let delta = (fmWithExtimatedTime?MYTIME - fmWithExtimatedTime?TIME) / fmWithExtimatedTime?TIME * 100.
    delta |> Series.map(fun k v -> Math.Round(v))
(*** include-value:fmWithExtimatedTime ***)

let avgDelta = fmWithExtimatedTime.Sum()?``Delta %`` / 16.
(*** include-value:avgDelta ***)

(**
The worst result is on "Cima Saler" track: my extimation is half of that reported by the book and 
should be investigated. Anyway the other results seem good enough: my extimation is just about 20% 
more optimistic than the empirical one.
*)