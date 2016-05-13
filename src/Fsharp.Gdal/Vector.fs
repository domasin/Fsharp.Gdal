module Fsharp.Gdal.Vector

open OSGeo.OGR
open System

let openVector path = 
    Ogr.OpenShared(path,0)

let features (vlay:Layer) = 
    vlay.ResetReading() |> ignore
    let fc = vlay.GetFeatureCount(0)
    [for i in [1..fc] -> vlay.GetNextFeature()]

type BoundingBox = 
    {
        XMin : float
        XMax : float
        YMin : float
        YMax : float
    }

//let getBoundingBox layer = 
//    let coords = 
//        layer
//        |> features
//        |> List.map (fun f -> 
//            f.Properties.ComuneNom, 
//            f.Geometry.Coordinates
//            |> List.ofArray
//            |> List.map (fun c -> c |> List.ofArray)
//            |> List.concat
//            |> List.map (fun c -> c.[0], c.[1])
//            )
//    let xMin,xMax,yMin,yMax = 
//        coords
//        |> List.map (fun (_,xs) -> xs)
//        |> List.concat
//        |> List.fold 
//            (
//                fun (xMin, xMax, yMin, yMax) (x,y) -> 
//                        Math.Min(x |> float, xMin), 
//                        Math.Max(x |> float, xMax), 
//                        Math.Min(y |> float, yMin), 
//                        Math.Max(y |> float, yMax)
//            ) (Decimal.MaxValue |> float, 0., Decimal.MaxValue |> float, 0.)
//    {XMin = xMin; XMax = xMax; YMin = yMin; YMax = yMax}

let fields (vlay:Layer) = 
    let lDef = vlay.GetLayerDefn()
    let fieldCount = lDef.GetFieldCount()
    [for fdIndex in [0..fieldCount-1] 
        ->
            let fd = lDef.GetFieldDefn(fdIndex)
            fdIndex, fd.GetName().ToUpperInvariant(), fd.GetFieldType()
    ]

let points (geom:Geometry) = 
    let numPoints = geom.GetPointCount()
    [for i in [1..numPoints - 1]
        -> 
            let x = geom.GetX(i) |> string
            let y = geom.GetY(i) |> string
            let wkt = sprintf "POINT(%s %s)" x y
            let p = Ogr.CreateGeometryFromWkt(ref wkt, geom.GetSpatialReference())
            p
    ]