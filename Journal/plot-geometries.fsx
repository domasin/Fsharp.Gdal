(*** hide ***)
#load "packages/FsLab/FsLab.fsx"


//#I "../bin/Fsharp.Gdal"
#I "../src/Fsharp.Gdal/bin/Debug"

#r "Fsharp.Gdal.dll"
#r "gdal_csharp.dll"
#r "gdalconst_csharp.dll"
#r "ogr_csharp.dll"
#r "osr_csharp.dll"

(**
Plot Geometries
========================
This section defines a rudimentary `plot` function to give at least 
some sort of graphical visualization of geometries.

To do this it makes use of `FSharp.Charting`'s charts to plot the 
coordinates of geometries in a cartesian plan. The main limit of the 
function is that polygons are rendered as lines instead of full shapes.
*)

open FSharp.Charting
open OSGeo

(**
The `coordinates` function just extracts a list of tuples 
made of the longitude and latitude of the corrdinates that consistute 
the geometry.
*)

/// Extracts the geometry's coordinates as a list of tuples made of 
/// longitude and latitude.
let coordinates (geom:OGR.Geometry) = 
    let last = geom.GetPointCount() - 1
    [
        for i in 0..last ->
            let p = [|0.;0.|]
            geom.GetPoint(i,p)
            (p.[0], p.[1])
    ]

(**
To properly draw the shapes in the cartesian space we need to 
choose a chart type that most fits with the geometry type to plot.

`createChart` creates a `Chart.Point` for points geometries while 
`Chart.Line` is choosen for every other geometry types: as marked above 
the main limit of this approach is that ploygons are still rendered as 
lines but this at least gives an immage of the geometry.
*)

/// Creates a chart for the geometry
let createChart (geom:OGR.Geometry) = 
    let coords = geom |> coordinates
    match (geom.GetGeometryType()) with
    | OGR.wkbGeometryType.wkbPoint
    | OGR.wkbGeometryType.wkbPoint25D -> Chart.Point(coords)
    | _                               -> Chart.Line(coords)

(**
We need to set the space of the chart at a size that makes the 
shape visibile enough.

First with `env` we extract the bounding box of the geometry as an 
`OGR.Envelope`
*)

/// Extracs the bounding box of the geometry
let env (geom:OGR.Geometry) = 
    let res = new OGR.Envelope()
    geom.GetEnvelope(res)
    res

(**
... then we `resize` it based on a choosen `zoom` as a percentage of the 
shape size. If the geometry is made of just a point it has not a real size 
so in this case we just calculate a margin based on the coordinate's magnitude.
*)

/// Resizes the envelope based on a choosen zoom 
let resize zoom (en:OGR.Envelope) = 
    let dx = en.MaxX - en.MinX
    let dy = en.MaxY - en.MinY
    let xMargin = if dx = 0. then en.MaxX / zoom / 2. else ((dx / zoom) - dx) / 2.
    let yMargin = if dy = 0. then en.MaxY / zoom / 2. else ((dy / zoom) - dy) / 2.
    en.MaxX <- en.MaxX + xMargin
    en.MaxY <- en.MaxY + yMargin
    en.MinX <- en.MinX - xMargin
    en.MinY <- en.MinY - yMargin
    en

(** 
Now we can define a `plotAt` function that plots the geometry at a specified
`zoom`.
 
Geomteries can be simple or compund. If `geom`is compound we need to recursively 
traverse its structure till its simple components and populate a `charts` 
list for each using the `createChart` function defined above.

The recursive part is made by the inner `createCharts` function taking advantage 
of the functional nature of F#.

At the end we can `Chart.Combine` all the elements of the chart list setting the 
cartesian plan's space at the proper size.
*)

/// Plots a geometry at a specified zoom
let plotAt zoom (geom:OGR.Geometry) = 
    let rec createCharts xs (geom:OGR.Geometry) = 
        let count = geom.GetGeometryCount()
        match count with
        | count when count = 0 -> [(geom |> createChart)]@xs
        | _ -> 
            [for i in 0..(count-1) -> 
                (geom.GetGeometryRef(i)) |> createCharts xs
            ] |> List.concat
    let charts = geom |> createCharts []
    let spaceSize = geom |> env |> resize zoom
    Chart.Combine(charts)
        .WithXAxis(Max=spaceSize.MaxX,Min=spaceSize.MinX)
        .WithYAxis(Max=spaceSize.MaxY,Min=spaceSize.MinY)

(**
Since normally a `zoom` at 80% of the shape size is enough to visualize the geometry 
we define the final `plot` function to default at a 80% `zoom` not to have to specify 
it each time:
*)

/// Plots a geometry at a zoom of 80%
let plot = (fun g -> g |> plotAt 0.8)
