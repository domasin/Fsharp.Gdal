module FGis.Raster

open OSGeo.GDAL
open System
open MathNet.Numerics
open MathNet.Numerics.LinearAlgebra

// geotransform[0] top left x
// geotransform[1] w-e pixel resolution
// geotransform[2] 0
// geotransform[3] top left y
// geotransform[4] 0
// geotransform[5] n-s pixel resolution (negative value)

let imageToGround (geotransform:float[]) (x, y) = 
    let x = geotransform.[0] + geotransform.[1] * x + geotransform.[2] * y
    let y = geotransform.[3] + geotransform.[4] * x + geotransform.[5] * y
    (x, y)

let groundToImage (geotransform:float[]) (x, y) = 
    let A = matrix [[ geotransform.[1]; geotransform.[2]]
                    [ geotransform.[4]; geotransform.[5]]]
    let b = vector [ x -  geotransform.[0]; y -  geotransform.[3]]
    let x = A.Solve(b)
    (x |> (Seq.nth 0) |> int, x |> (Seq.nth 1) |> int)

let getImgValue (geotransform:float[]) (dataset:Dataset) (xPixel,yLine) = 
    let band = dataset.GetRasterBand(1)
    let mutable (buffer:float[]) = [|0.|]
//    let (xPixel,yLine) = groundToImage geotransform (x, y)
    let scanline = band.ReadRaster(xPixel, yLine, 1, 1, buffer, 1, 1, 0, 0)
    match scanline with
    | CPLErr.CE_None -> Some (buffer.[0])
    | _ -> None