(*** hide ***)
#I "../bin/Fsharp.Gdal"

#r "Fsharp.Gdal.dll"
#r "gdal_csharp.dll"
#r "gdalconst_csharp.dll"
#r "ogr_csharp.dll"
#r "osr_csharp.dll"
//
#r "PresentationCore"
#r "WindowsBase"
#r "presentationframework"
#r "System.Xaml"
//
open System
open System.IO
open System.Windows
open System.Windows.Media
open System.Windows.Media.Imaging
open System.Windows.Markup
open System.Windows.Shapes
open System.Windows.Controls
//
//open FSharp.Gdal


(**
Plot Geometry
========================
This section defines a `plot` function to give a graphical visualization of GDAL geometries 
in a Xaml Window Page using a Canvas as a Cartesian Plan.
*)

(**
The `coordinates` function just extracts a list of tuples 
made of the longitude and latitude of the corrdinates that consistute 
the geometry.
*)

open OSGeo

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
Geometries in the Canvas are rendered as `Paths` whose `Data` property follows 
the Standard Vector Graphics pattern.

We define functions specific for each shapes type (points, line, 
polygons) to return their path data as svg strings.
*)

(**
Points will be rendered graphically as little squares whose dimension must be choosen 
in a way appropriate to give a sense of a puntual shape.

Fo this reason the `pointPath` function takes an `OGR.Geometry` argument together with 
a `scale` argument.
*)

/// Takes a `scale` argument and an OGR.Geometry of point type and returns the svg 
/// reppresentation of a little square sized at the given scale as to give a sense 
/// of a punctual shape.
let pointPath scale geom = 
    let x,y = geom |> coordinates |> List.head 
    let x1 = x - (3. / scale)
    let y1 = y - (3. / scale)
    let x2 = x + (3. / scale)
    let y2 = y + (3. / scale)
    sprintf "M %f,%f %f,%f %f,%f %f,%f %f,%fz" x1 y1 x2 y1 x2 y2 x1 y2 x1 y1

(**
The `linePath` function takes an `OGR.Geometry` which should be of linear type for the function 
to work appropriately and returns its svg reppresentation.
*)

/// Takes an OGR.Geometry of linear type and returns its svg reppresentation as a string.
let linePath geom = 
    geom
    |> coordinates
    |> List.fold 
        (fun acc (x,y) -> 
            if acc = "" then
                acc + (sprintf "M %f,%f" x y)
            else
                acc + (sprintf " L %f,%f" x y)
        ) ""

(**
The `ringPath` function takes an `OGR.Geometry` of ring type and returns its 
svg reppresentation.
*)

/// Takes an OGR.Geometry of ring type and returns its svg reppresentation as a string.
let ringPath geom = 
    geom
    |> coordinates
    |> List.fold 
        (fun acc (x,y) -> 
            if acc = "" then
                acc + (sprintf "M %f,%f" x y)
            else
                acc + (sprintf " %f,%f" x y)
        ) ""

(**
An OGR Polygon is a complex structure made of one or more rings. The `polygonPath` 
traverses the polygon structure to find all its rings and then concatenate their 
svg reppresentation in a single string.
*)

/// Takes an OGR.Geometry of polygon type and returns its svg reppresentation.
let polygonPath (geom:OGR.Geometry) = 
    let count = geom.GetGeometryCount()
    [for i in [0..(count-1)] -> 
        (geom.GetGeometryRef(i)) |> ringPath
    ]
    |> List.fold (fun acc ringPath -> ringPath + " " + acc) ""

(**
OGR includes different geometry types that for our purpose can be rendered 
as the same shape type.

Below we define 5 classes of shapes and then a function that maps an OGR.Geometry 
to the appropiate shape.
*)

/// Classes of shapes reppresenting OGR.Geometries
type Shape = 
    | Point
    | Line
    | Ring
    | Polygon
    | GeometryCollection

/// Just an utility function to more easily get the OGR.Geometry type
let geomType (geom:OGR.Geometry) = 
    geom.GetGeometryType()
    
/// Maps an OGR.Geometry to the appropriate shape
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

let shapePath scale geom = 
    let geomType = geom |> geometryShape
    match geomType with
    | Point -> geom |> pointPath scale
    | Line -> geom |> linePath
    | Polygon -> geom |> polygonPath

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
    
    let data = geom |> shapePath scale
    path.Data <- Media.Geometry.Parse(data)

    let thickness = 1. / scale
    path.StrokeThickness <- thickness

    let renderTrasnform = sprintf "%.5f 0 0 %.5f 0 0" scale scale
    path.RenderTransform <- Transform.Parse(renderTrasnform)

    match geomType with
    | Point     -> path.Fill <- Brushes.Black
    | Polygon   -> path.Fill <- seed |> getRandomBrush
    | Line      -> path.Stroke <- Brushes.Black
    | _         -> ()

    printfn "Shape Properties:"
    printfn "Data = %s" data |> ignore
    printfn "StrokeThickness = %f" thickness |> ignore
    printfn "RenderTransform = %s" renderTrasnform |> ignore
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
    let xMargin = if dx = 0. then 200. else ((dx / zoom) - dx) / 2.
    let yMargin = if dy = 0. then 200. else ((dy / zoom) - dy) / 2.
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

let save fileName (bmp:RenderTargetBitmap)  = 

    let fileName = __SOURCE_DIRECTORY__ + sprintf "\output\img\%s.png" fileName

    let enc = new PngBitmapEncoder()
    let b = BitmapFrame.Create(bmp)
    enc.Frames.Add(b)

    use stm = File.Create(fileName)
    enc.Save(stm)

let steps x y text color renderTransform (canvas:Canvas) = 
    printfn "x = %f y = %f renderTransform = %s" x y renderTransform
    let textBlock = new TextBlock()
    textBlock.Text <- text
    textBlock.Foreground <- new SolidColorBrush(color)
    Canvas.SetLeft(textBlock, x)
    Canvas.SetTop(textBlock, y)
    textBlock.RenderTransform <- Transform.Parse(renderTransform)
    canvas.Children.Add(textBlock)

let axisXY scale (env:OGR.Envelope) renderTransform (canvas:Canvas) = 
    let x,y = env.MinX, env.MinY
    let x1 = x + (400. / scale)
    let y1 = y + (400. / scale)

    let axisX = new OGR.Geometry(OGR.wkbGeometryType.wkbLineString)
    axisX.AddPoint(x,  y,  0.)
    axisX.AddPoint(x1, y,  0.)

    let axisY = new OGR.Geometry(OGR.wkbGeometryType.wkbLineString)
    axisY.AddPoint(x, y, 0.)
    axisY.AddPoint(x,  y1, 0.)

    let geomcol = new OGR.Geometry(OGR.wkbGeometryType.wkbGeometryCollection)
    geomcol.AddGeometry(axisX) |> ignore
    geomcol.AddGeometry(axisY) |> ignore

    for i in [0.0..50.0..400.0] do
        let step = new OGR.Geometry(OGR.wkbGeometryType.wkbLineString)
        step.AddPoint(x + (i / scale),  y,                  0.)
        step.AddPoint(x + (i / scale),  y + (10. / scale),  0.)
        geomcol.AddGeometry(step) |> ignore

    for i in [0.0..50.0..400.0] do
        let step = new OGR.Geometry(OGR.wkbGeometryType.wkbLineString)
        step.AddPoint(x,                    y + (i / scale),  0.)
        step.AddPoint(x + (10. / scale),    y + (i / scale),  0.)
        geomcol.AddGeometry(step) |> ignore

    printfn "scael = %f" scale

    // draw x steps
    for i in [0.0..50.0..400.0] do
        let xi = i
        let yi = if i % 100. = 0. then 0. else -15.
        let xCoord = x + (i / scale)
        canvas |> steps xi yi (sprintf "%.2f" xCoord) Colors.Black renderTransform |> ignore

    // draw y steps
    for i in [0.0..50.0..400.0] do
        let yi = i
        let xi = -70.
        let yCoord = y + (i / scale)
        if i > 0.0 then
            canvas |> steps xi yi (sprintf "%.2f" yCoord) Colors.Black renderTransform |> ignore

    let paths = geomcol |> geometryPaths scale 0 []
    for path in paths do 
        canvas.Children.Add(path) |> ignore

let plot fileName (geom:OGR.Geometry) = 

    let env = geom |> env |> resize 0.8
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
    let renderTransform = sprintf "1 0 0 -1 %.2f %.2f" originX originY
    
    canvas.RenderTransform <- Transform.Parse(renderTransform)
    
    printfn "RenderTrasnform = %s" renderTransform |> ignore
    printfn ""
    
    let paths = geom |> geometryPaths scale 0 []
    for path in paths do 
        canvas.Children.Add(path) |> ignore

    let renderTransformAxisXY = sprintf "1 0 0 -1 %.2f %.2f" -originX originY
    canvas |> axisXY scale env renderTransformAxisXY

//    let winSize = 900.
    let win = new Window()
    win.Width <- 600.
    win.Height <- 550.
    win.Content <- canvas
    win.Topmost <- true

    win.Title <- "F# Geometry Plot: " + fileName

    win.Show()

    let size = new Size(win.Width, win.Height)
    canvas.Measure(size)
    let rtb = new RenderTargetBitmap(600, 550, 96., 96., PixelFormats.Default)
    rtb.Render(canvas)
    rtb |> save fileName

type Plot(geom:OGR.Geometry) = 
    let env = geom |> env |> resize 1.
    let maxXYSide = env |> maxSize

    let canvas = new Canvas()
    let canvasSize = 400.
    do canvas.Width <- canvasSize
    do canvas.Height <- canvasSize
    do canvas.RenderTransformOrigin <- new Point(0.5,0.5)

    do printfn "Canvas Properties:"
    let scale = canvasSize / maxXYSide
    do printfn "Scale shapes: (canvas size = %.2f / envelope size = %.2f) = %.5f" canvasSize maxXYSide scale

    let originX = env.MinX * scale * -1.
    let originY = env.MinY * scale
    let renderTrasnform = sprintf "1 0 0 -1 %.2f %.2f" originX originY
    do canvas.RenderTransform <- Transform.Parse(renderTrasnform)
    
    do printfn "RenderTrasnform = %s" renderTrasnform |> ignore
    do printfn ""
    
    let paths = geom |> geometryPaths scale 0 []
    let addPaths = 
        for path in paths do 
            canvas.Children.Add(path) |> ignore
    do addPaths

    let winSize = 500.
    let win = new Window()
    do win.Width <- winSize
    do win.Height <- winSize
    do win.Content <- canvas

    do win.Title <- "F# Geometry Plot"

//    do win.Show()

    member this.Show() = 
        win.Show()

    member this.SaveAsBitmap(fileName) = 
        let size = new Size(win.Width, win.Height)
        canvas.Measure(size)
        let rtb = new RenderTargetBitmap(500, 500, 96., 96., PixelFormats.Default)
        rtb.Render(canvas)
        rtb |> save fileName
