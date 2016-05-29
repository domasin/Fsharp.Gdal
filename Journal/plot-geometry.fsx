#I "../bin/Fsharp.Gdal"

#r "Fsharp.Gdal.dll"
#r "gdal_csharp.dll"
#r "gdalconst_csharp.dll"
#r "ogr_csharp.dll"
#r "osr_csharp.dll"

#r "PresentationCore"
#r "WindowsBase"
#r "presentationframework"
#r "System.Xaml"

open System
open System.Xml
open System.Windows
open System.Windows.Media
open System.Windows.Markup
open System.Windows.Shapes
open System.Windows.Controls

open FSharp.Gdal
open OSGeo

Configuration.Init() |> ignore

// Define geometries to test the functions

let point = new OGR.Geometry(OGR.wkbGeometryType.wkbPoint)
point.AddPoint(200., 200., 0.)

let line = new OGR.Geometry(OGR.wkbGeometryType.wkbLineString)
line.AddPoint(50., 50., 0.)
line.AddPoint(100., 120., 0.)
line.AddPoint(200., 150., 0.)
line.AddPoint(300., 300., 0.)

let ring = new OGR.Geometry(OGR.wkbGeometryType.wkbLinearRing)
ring.AddPoint(100., 100., 0.)
ring.AddPoint(200., 100., 0.)
ring.AddPoint(200., 200., 0.)
ring.AddPoint(100., 200., 0.)
ring.AddPoint(100., 100., 0.)

let poly = new OGR.Geometry(OGR.wkbGeometryType.wkbPolygon)
poly.AddGeometry(ring)

// Create outer ring
let outRing = new OGR.Geometry(OGR.wkbGeometryType.wkbLinearRing)
outRing.AddPoint(100., 100., 0.)
outRing.AddPoint(200., 100., 0.)
outRing.AddPoint(200., 200., 0.)
outRing.AddPoint(100., 200., 0.)
outRing.AddPoint(100., 100., 0.)

// Create inner ring
let innerRing = new OGR.Geometry(OGR.wkbGeometryType.wkbLinearRing)
innerRing.AddPoint(125., 125., 0.)
innerRing.AddPoint(175., 125., 0.)
innerRing.AddPoint(175., 175., 0.)
innerRing.AddPoint(125., 175., 0.)
innerRing.AddPoint(125., 125., 0.)

// Create polygon
let polyWithHoles = new OGR.Geometry(OGR.wkbGeometryType.wkbPolygon)

polyWithHoles.AddGeometry(innerRing)
polyWithHoles.AddGeometry(outRing)

let coordinates (geom:OGR.Geometry) = 
    let last = geom.GetPointCount() - 1
    [
        for i in 0..last ->
            let p = [|0.;0.|]
            geom.GetPoint(i,p)
            (p.[0], p.[1])
    ]

let pointPath geom = 
    let x,y = geom |> coordinates |> List.head 
    sprintf "M %.2f,%.2f a 4,4 0 1,0 8,0 a 4,4 0 1,0 -8,0" x y

point |> pointPath

let linePath geom = 
    geom
    |> coordinates
    |> List.fold 
        (fun acc (x,y) -> 
            if acc = "" then
                acc + (sprintf "M %.2f,%.2f" x y)
            else
                acc + (sprintf " L %.2f,%.2f" x y)
        ) ""

line |> linePath

let ringPath geom = 
    geom
    |> coordinates
    |> List.fold 
        (fun acc (x,y) -> 
            if acc = "" then
                acc + (sprintf "M %.2f,%.2f" x y)
            else
                acc + (sprintf " %.2f,%.2f" x y)
        ) ""

ring |> ringPath

let polygonPath (geom:OGR.Geometry) = 
    let count = geom.GetGeometryCount()
    [for i in [0..(count-1)] -> 
        (geom.GetGeometryRef(i)) |> ringPath
    ]
    |> List.fold (fun acc ringPath -> ringPath + " " + acc) ""

polyWithHoles |> polygonPath

type GeomType = 
    | Point
    | Line
    | Ring
    | Polygon
    | GeometryCollection

let geomType (geom:OGR.Geometry) = 
    geom.GetGeometryType()
    
let geometryShape geom = 
    let geomType = geom |> geomType
    match geomType with
    | OGR.wkbGeometryType.wkbPoint
    | OGR.wkbGeometryType.wkbPoint25D               -> Point

    | OGR.wkbGeometryType.wkbLineString
    | OGR.wkbGeometryType.wkbLineString25D          -> Line

    | OGR.wkbGeometryType.wkbLinearRing             -> Ring

    | OGR.wkbGeometryType.wkbPolygon
    | OGR.wkbGeometryType.wkbPolygon25D             -> Polygon

    | OGR.wkbGeometryType.wkbMultiPoint 
    | OGR.wkbGeometryType.wkbMultiPoint25D 
    | OGR.wkbGeometryType.wkbMultiLineString
    | OGR.wkbGeometryType.wkbMultiLineString25D
    | OGR.wkbGeometryType.wkbMultiPolygon
    | OGR.wkbGeometryType.wkbMultiPolygon25D    
    | OGR.wkbGeometryType.wkbGeometryCollection
    | OGR.wkbGeometryType.wkbGeometryCollection25D  -> GeometryCollection

let shapePath geom = 
    let geomType = geom |> geometryShape
    match geomType with
    | Point -> geom |> pointPath
    | Line -> geom |> linePath
    | Polygon -> geom |> polygonPath

point |> shapePath
line |> shapePath
poly |> shapePath
polyWithHoles |> shapePath

let getRandomBrush seed  = 
    let brushArray = 
        typeof<Brushes>.GetProperties() 
        |> Array.map (fun c -> c.Name)

    let randomGen = new Random(seed)
    let randomColorName = brushArray.[randomGen.Next(brushArray.Length)]
    let color = (new BrushConverter()).ConvertFromString(randomColorName) :?> SolidColorBrush
    color

let geometryToPath scale seed (geom:OGR.Geometry) = 
    let geomType = geom |> geometryShape
    let path = new Path()
    
    
    let data = geom |> shapePath
    path.Data <- Media.Geometry.Parse(data)
    path.StrokeThickness <- 1. / scale

    let renderTrasnform = sprintf "%.5f 0 0 %.5f 0 0" scale scale
    path.RenderTransform <- Transform.Parse(renderTrasnform)

    match geomType with
    | Point     -> path.Fill <- Brushes.Black
    | Polygon   -> path.Fill <- seed |> getRandomBrush
    | Line      -> path.Stroke <- Brushes.Black
    | _         -> ()

    printfn "Shape Properties:"
    printfn "Data = %s" data |> ignore
    printfn "RenderTrasnform = %s" renderTrasnform |> ignore
    printfn ""

    path

let rec geometryPaths scale seed xs (geom:OGR.Geometry) = 
    let geomType = geom |> geometryShape

    match geomType with
    | GeometryCollection -> 
        let count = geom.GetGeometryCount()
        match count with
        | count when count = 0 -> 
            let path = geom |> geometryToPath scale seed
            [path]@xs
        | _ -> 
            [for i in [0..(count-1)] -> 
                (geom.GetGeometryRef(i)) |> geometryPaths scale i xs
            ] |> List.concat
    | _ -> let path = geom |> geometryToPath scale seed
           [path]@xs

/// Extracs the bounding box of the geometry
let env (geom:OGR.Geometry) = 
    let res = new OGR.Envelope()
    geom.GetEnvelope(res)
    res

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

let sizeX (en:OGR.Envelope) = 
    en.MaxX - en.MinX

let sizeY (en:OGR.Envelope) = 
    en.MaxY - en.MinY

let maxSize env = 
    max (env |> sizeX) (env |> sizeY)

let scale (canvasSize:float) (envSize:float) = 
     canvasSize / envSize

// RenderTransform = ScaleX 0 0 ScaleY 

let plot (geom:OGR.Geometry) = 

    let env = geom |> env |> resize 1.
    let maxXYSide = env |> maxSize

    printfn "Geometry Properties:"
    printfn "Geometry Type: %A" (geom |> geomType)
    printfn "Envelope: MinX = %.2f, MaxX = %.2f, MinY = %.2f, MaxY = %.2f" env.MinX env.MinY env.MaxX env.MaxY
    printfn "Max Envelope XY size: %.2f" maxXYSide
    printfn ""

    let canvas = new Canvas()
    let canvasSize = 400.
    canvas.Width <- canvasSize
    canvas.Height <- canvasSize
    canvas.RenderTransformOrigin <- new Point(0.5,0.5)

    printfn "Canvas Properties:"
    let scale = canvasSize / maxXYSide
    printfn "Scale shapes: (canvas size = %.2f / envelope size = %.2f) = %.5f" canvasSize maxXYSide scale

    let originX = env.MinX * scale * -1.
    let originY = env.MinY * scale
    let renderTrasnform = sprintf "1 0 0 -1 %.2f %.2f" originX originY
    canvas.RenderTransform <- Transform.Parse(renderTrasnform)
    
    printfn "RenderTrasnform = %s" renderTrasnform |> ignore
    printfn ""
    
    let paths = geom |> geometryPaths scale 0 []
    for path in paths do 
        canvas.Children.Add(path) |> ignore

    let winSize = 500.
    let win = new Window()
    win.Width <- winSize
    win.Height <- winSize
    win.Content <- canvas

    win.Title <- "F# Geometry Plot"
    win.Show()

//point |> plot
//line |> plot
//poly |> plot
//polyWithHoles |> plot

let ring2 = new OGR.Geometry(OGR.wkbGeometryType.wkbLinearRing)
//ring2.AddPoint(1000., 1000., 0.)
//ring2.AddPoint(2000., 1000., 0.)
//ring2.AddPoint(2000., 2000., 0.)
//ring2.AddPoint(1000., 2000., 0.)
//ring2.AddPoint(1000., 1000., 0.)

ring2.AddPoint(100., 100., 0.)
ring2.AddPoint(300., 100., 0.)
ring2.AddPoint(200., 200., 0.)
ring2.AddPoint(100., 100., 0.)


let poly2 = new OGR.Geometry(OGR.wkbGeometryType.wkbPolygon)
poly2.AddGeometry(ring2)

//poly2 |> shapePath
//poly2 |> plot
