#I "../bin/Fsharp.Gdal"

#r "Fsharp.Gdal.dll"
#r "gdal_csharp.dll"
#r "gdalconst_csharp.dll"
#r "ogr_csharp.dll"
#r "osr_csharp.dll"

open FSharp.Gdal
open OSGeo

#r "System.Xml.Linq.dll"
open FSharp.Data
open System
open System.IO

let randomColor seed = 
    let random = new Random(seed)
    let color = String.Format("#{0:X6}", random.Next(0x1000000))
    color

let coordinates (geom:OGR.Geometry) = 
    let last = geom.GetPointCount() - 1
    [
        for i in 0..last ->
            let p = [|0.;0.|]
            geom.GetPoint(i,p)
            (p.[0], p.[1])
    ]

let path closed coords = 
    let res = 
        coords
        |> List.fold (fun acc (x,y) -> 
                if acc = "" then
                    acc + (sprintf "M %.1f %.1f" x y)
                else
                    acc + (sprintf " L %.1f %.1f" x y)
            ) ""
    res + if closed then "Z" else ""

let rec drawGeometries seed xs (geomType:OGR.wkbGeometryType) (geom:OGR.Geometry) = 
    let count = geom.GetGeometryCount()
    match count with
    | count when count = 0 -> 
        let gemostr = geom |> coordinates |> path (geom.IsRing())
        let color = randomColor seed
        let pathstr = sprintf """<path style="fill:%s;" d="%s" />""" color gemostr
        [pathstr]@xs
    | _ -> 
        [for i in [0..(count-1)] |> List.rev -> // we need to reverse to visualize holes
            (geom.GetGeometryRef(i)) |> drawGeometries i xs geomType
        ] |> List.concat

/// Extracs the bounding box of the geometry
let env (geom:OGR.Geometry) = 
    let res = new OGR.Envelope()
    geom.GetEnvelope(res)
    res

let plot (geom:OGR.Geometry) = 
    let bb = geom |> env
    let startstr = 
        sprintf """<?xml version="1.0" encoding="utf-8"?><svg xmlns="http://www.w3.org/2000/svg" viewBox="%.1f %.1f %.1f %.1f">""" 
            bb.MinX bb.MinY bb.MaxX bb.MaxY
    let paths = 
        geom 
        |> drawGeometries 0 [] (geom.GetGeometryType())
        |> List.fold (fun acc x -> acc + x) ""
    let svgstr = startstr + paths + "</svg>"
    let filename = @"G:\GitHub\Fsharp.Gdal\Journal\data\test.svg"
    use writer = File.CreateText filename
    writer.Write(svgstr)
    System.Diagnostics.Process.Start(filename)