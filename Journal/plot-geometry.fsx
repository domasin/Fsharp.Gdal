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
This section defines a `Plot` utility to give a graphical visualization of GDAL geometries 
in a Xaml Window Page using a Canvas as a Cartesian Plan where to put the coorrdinates of 
the geometries.
*)

(**
Drawing coordinates with svg patterns
------------------------
Geometries in the Canvas are rendered as `Paths` whose `Data` property follows 
the Standard Vector Graphics pattern. A path consists of a sequence of path
segments and each path segment consists of a sequence of commands where the first defines a new
current position and the remainder define a line or curve from the current position to some new
position which becomes the current position for the next part of the curve and so on. The form of the
path element is as follows:
*)

let stringPath = "M 0 0 L 100 100"

(** 
The example above defines a path that consists of establishing a
current position at the origin (`M`ove to 0,0) and the path goes from there to the point (100,100) as a
straight `L`ine.
*)

(**
We define specific functions for each shapes type (points, line, 
rings) that return their path data as svg strings.
*)

(**
Points will be rendered graphically as little squares whose dimension must be choosen 
in a way appropriate to give a sense of a puntual shape.

Fo this reason the `pointPath` function takes an `(x,y)` argument representing the point's coordinates  
together with a `scale` argument. The function will return the svg reppresentation of a little square 
centered at the point's coordinates and sized at the given scale as to give a sense of a 
punctual shape. The square will have a side of 6 points in a Canvas of 400 x 400 points. 
and so the scale should be calculated dividing the actual bounding box by 400.
*)

/// Takes a `scale` argument and a tuple reppresenting a point' coordinates type and returns the svg 
/// reppresentation of a little square centered at the point's coordinates and sized 
/// at the given scale as to give a sense of a punctual shape. The square will have 
/// a side of 6 points in a Canvas of 400 x 400 points. The appropriate scale should 
/// be calculated dividing the actual bounding box by 400.
let pointPath scale (x,y) = 
//    let x,y = coordinates |> List.head 
    let x1 = x - (3. / scale)
    let y1 = y - (3. / scale)
    let x2 = x + (3. / scale)
    let y2 = y + (3. / scale)
    sprintf "M %f,%f %f,%f %f,%f %f,%f %f,%fz" x1 y1 x2 y1 x2 y2 x1 y2 x1 y1

(**
The `linePath` function takes a list representing a line's coordinatesand returns its svg reppresentation.
*)

/// Takes and a list representing a line's coordinates and returns its svg reppresentation as a string.
let linePath coordinates = 
    coordinates
    |> List.fold 
        (fun acc (x,y) -> 
            if acc = "" then
                acc + (sprintf "M %f,%f" x y)
            else
                acc + (sprintf " L %f,%f" x y)
        ) ""

(**
Rings are the base elements of an OGR Polygon. Actually they are just lines but they have 
to be drawn in a different way for the Xaml Engine to fill the space they enclose with a 
solid color.

The `ringPath` function takes a list representing a ring's coordinates and returns its 
svg reppresentation.
*)

/// Takes a list representing a ring's coordinates and returns its svg reppresentation as a string.
let ringPath coordinates = 
    coordinates
    |> List.fold 
        (fun acc (x,y) -> 
            if acc = "" then
                acc + (sprintf "M %f,%f" x y)
            else
                acc + (sprintf " %f,%f" x y)
        ) ""

(**
Creating Canvas Paths from OGR.Geometries
------------------------
OGR includes different geometry types that for our purpose can be rendered 
as the same shape type.

Below we define 3 classes of shapes (for points, lines and polygons) that actually we will 
draw on the canvas, plus a `ShapeCollection` type to treat each type of Geometry Collection 
or Multi-Geometry. For these two last we won't draw just a single shape but more shapes at 
the same time.

Then we define the `toShape` function that maps an OGR.Geometry to the appropiate shape.
*)

open OSGeo

/// Classes of shapes reppresenting OGR.Geometries.
type Shape = 
    | Point
    | Line
    | Polygon
    | ShapeCollection

/// Just an utility function to more easily get the OGR.Geometry type.
let geomType (geom:OGR.Geometry) = 
    geom.GetGeometryType()
    
/// Maps an OGR.Geometry to the appropriate shape if the mapping is known and 
/// fails in the other case.
let toShape geom = 
    let geomType = geom |> geomType
    match geomType with
    | OGR.wkbGeometryType.wkbPoint
    | OGR.wkbGeometryType.wkbPoint25D               -> Point

    | OGR.wkbGeometryType.wkbLineString
    | OGR.wkbGeometryType.wkbLineString25D          -> Line

    | OGR.wkbGeometryType.wkbPolygon
    | OGR.wkbGeometryType.wkbPolygon25D             -> Polygon

    | OGR.wkbGeometryType.wkbMultiPoint 
    | OGR.wkbGeometryType.wkbMultiPoint25D 
    | OGR.wkbGeometryType.wkbMultiLineString
    | OGR.wkbGeometryType.wkbMultiLineString25D
    | OGR.wkbGeometryType.wkbMultiPolygon
    | OGR.wkbGeometryType.wkbMultiPolygon25D    
    | OGR.wkbGeometryType.wkbGeometryCollection
    | OGR.wkbGeometryType.wkbGeometryCollection25D  -> ShapeCollection
    | _                                             -> failwith (sprintf "can't map geomType %A to a known shape" geomType)

(**
Below we will define a global `shapePath` function that actually will return a 
path only for geometry types that can be classified as points, lines or 
polygons. For every other geometry types the function will fail.

First of all we need to extract the coordinates from the OGR.Geometries.

The `coordinates` function extracts a list of tuples made of the x (longitude 
if we are in a geographic space) and y (latitude) coordinates that constitute 
the geometry:
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
While we already have function to calculate the svg pattern for point and line 
geometries we have not defined a corresponding function for polygons but instead 
a function for rings.

An OGR Polygon is a complex structure made of one or more rings. In this case we
need to traverse the geometry structure to find all its rings and then concatenates their 
svg reppresentation in a single string.
*)

/// Returns the appropriate path for geometry types that can be classified as 
/// points, lines or polygons. Fails for every other geometry types.
let shapePath scale geom = 
    let shape = geom |> toShape
    match shape with
    | Point     -> geom |> coordinates |> List.head |> pointPath scale
    | Line      -> geom |> coordinates|> linePath
    | Polygon   -> 
                   let count = geom.GetGeometryCount()
                   [for i in [0..(count-1)] -> 
                       (geom.GetGeometryRef(i)) |> coordinates |> ringPath
                   ]
                   |> List.fold (fun acc ringPath -> ringPath + " " + acc) ""
    | _         -> failwith (sprintf "can't draw a shape of geomType %A" geomType)

(**
Frequently will be plotting more geometries in the same chart and to distinguish one 
from the others we will have to choose a different color.

Colors in Xaml are rendered by `Brushes` so we define a `getRandomBrush` function that 
will return each time a `Brush` with a new random color:
*)

/// Returna a `Brush` with a new random color.
let getRandomBrush seed  = 
    let brushArray = 
        typeof<Brushes>.GetProperties() 
        |> Array.map (fun c -> c.Name)

    let randomGen = new Random(seed)
    let randomColorName = brushArray.[randomGen.Next(brushArray.Length)]
    let color = (new BrushConverter()).ConvertFromString(randomColorName) :?> SolidColorBrush
    color

(**
Since now, the function defined above return paths as strings. The `toPath` function below returns 
the actual Xaml `Path` that we will add to the canvas for each `OGR.Geometry` that is simple i.e. 
is a point, a line or a polygon. Then we will define a more generic `toPaths` function that for 
every type of geometry (either simple or compound) will return a list of shapes to be drawn on the 
Canvas.

The `scale` argument is needed to re-scale the sapes based on the fraction of the current bounding 
box respect to the 400 x 400 points Canvas. To re-scale the shapes we use the `RenderTransform` property 
of the `Path`. (More on the `RenderTransform` property below.)

The `seed` argument is needed to feed the `getRandomBrush` function to have a real random color.
*)

/// Maps a simple OGR.Geometry to a Xaml Path.
let toPath scale seed (geom:OGR.Geometry) = 
    let shape = geom |> toShape
    let path = new Path()
    
    let data = geom |> shapePath scale
    path.Data <- Media.Geometry.Parse(data)

    let thickness = 1. / scale
    path.StrokeThickness <- thickness

    let renderTransform = sprintf "%.5f 0 0 %.5f 0 0" scale scale
    path.RenderTransform <- Transform.Parse(renderTransform)

    match shape with
    | Point
    | Polygon   -> path.Fill    <- seed |> getRandomBrush
    | Line      -> path.Stroke  <- seed |> getRandomBrush
    | _         -> ()

//    printfn "Path Properties:"
//    printfn "Data = %s" data |> ignore
//    printfn "StrokeThickness = %f" thickness |> ignore
//    printfn "RenderTransform = %s" renderTransform |> ignore
//    printfn ""

    path

/// Maps any OGR.Geometry (simple or compound) to a a Xaml Paths list.
let rec toPaths scale seed xs (geom:OGR.Geometry) = 
    let shape = geom |> toShape

    match shape with
    | ShapeCollection -> 
        let count = geom.GetGeometryCount()
        [for i in [0..(count-1)] -> 
                (geom.GetGeometryRef(i)) |> toPaths scale i xs
            ] |> List.concat
    | _ -> let path = geom |> toPath scale seed
           [path]@xs

(**
Setting the Canvas space: OGR.Envelope and RenderTransform
------------------------
We need to set the space of the cartesian plan represented by the Canvas 
at a size and at a position that makes the shape visibile.

First with `env` we extract the bounding box of the geometry as an 
`OGR.Envelope`
*)

/// Extracs the bounding box of the geometry.
let env (geom:OGR.Geometry) = 
    let res = new OGR.Envelope()
    geom.GetEnvelope(res)
    res

(**
... then we `resize` it based on a choosen `zoom` as a percentage of the 
shape size. If the geometry is made of just a point it has not a real size 
so in this case we just calculate a margin based on the coordinate's magnitude.
*)

/// Resizes the envelope based on a choosen zoom.
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

(**
`RenderTranform` can be used to scale, skew, rotate and translate a wpf control.

We will use the envelop information to calculate 1) the appropriate renderTransform 
both to scale the sapes in the canvas to render them bigger or smaller enough to 
be visibile; and 2) to translate the canvas at the shape positions

The pattern used by the property is:
*)

let renderTransformString = "scaleX rotateAngle skewAngle scaleY translateX translateY"

(**
Drawing a coordinates grid on X and Y Axis
------------------------
Now before to define the final `Plot` class a little tricky thing. 

I want to see on my canvas not only the geometries but also the scale at which 
they have been drawn. So I need to draw steps on the X and Y Axis with the 
associated coordinate number at the given scale.

The `steps` function writes the coordinate number on the steps:
*)

/// Writes the coordinate number at the given x y coordinates.
let steps x y text color renderTransform (canvas:Canvas) = 
//    printfn "x = %f y = %f renderTransform = %s" x y renderTransform
    let textBlock = new TextBlock()
    textBlock.Text <- text
    textBlock.Foreground <- new SolidColorBrush(color)
    Canvas.SetLeft(textBlock, x)
    Canvas.SetTop(textBlock, y)
    textBlock.RenderTransform <- Transform.Parse(renderTransform)
    canvas.Children.Add(textBlock)

(**
The `blackPath` function returns the Xaml Path for a black line at the given coordinates 
and will be used to draw both the x and y axis and also the single steps lines on them.
*)

/// Returns the Xaml Path for a black line at the given coordinates.
let blackPath scale coordinates = 
    let data = coordinates |> linePath
    let path = new Path()
    path.Data <- Media.Geometry.Parse(data)
    let thickness = 1. / scale
    path.StrokeThickness <- thickness
    let renderTransform = sprintf "%.5f 0 0 %.5f 0 0" scale scale
    path.RenderTransform <- Transform.Parse(renderTransform)
    path.Stroke  <- Brushes.Black
    path

(**
Finally the `axisXY` draws on a give Canvas the X and Y axis, the steps on them 
and writes the numbers of their coordinates.
*)

let axisXY scale (env:OGR.Envelope) renderTransform (canvas:Canvas) = 
    let startX,startY = env.MinX, env.MinY
    let endX = startX + (400. / scale)
    let endY = startY + (400. / scale)

    // Draw X Axis 
    canvas.Children.Add([startX,startY;endX,startY] |> blackPath scale) |> ignore

    // Draw Y Axis 
    canvas.Children.Add([startX,startY;startX,endY] |> blackPath scale) |> ignore

    // Draw X Y Steps and write their coordinates number
    for i in [0.0..50.0..400.0] do
        
        // Draw X steps
        let startStepX = startX + (i / scale)
        let endStepY = startY + (10. / scale)
        canvas.Children.Add([startStepX,startY;startStepX,endStepY] |> blackPath scale) |> ignore

        // Draw Y steps
        let startStepY = startY + (i / scale)
        let endStepX = startX + (10. / scale)
        canvas.Children.Add([startX,startStepY;endStepX,startStepY] |> blackPath scale) |> ignore

        // Write X numbers
        let yOffset = if i % 100. = 0. then 0. else -15.
        let xCoord = startX + (i / scale)
        canvas |> steps i yOffset (sprintf "%.2f" xCoord) Colors.Black renderTransform |> ignore

        // Write Y numbers
        let xOffset = -70.
        let yCoord = startY + (i / scale)
        if i > 0.0 then
            canvas |> steps xOffset i (sprintf "%.2f" yCoord) Colors.Black renderTransform |> ignore

(**
Saving a Canvas as an Image
------------------------
Since I need to write this documentation and I don't want to take a print screen for each plot 
I made, I will define a save function to convert the canvas in a png immage and save it on my drive.

The `saveAsBitmap` function just do this for me:
*)

/// Saves a canvas as a png image of a given width and height size to a given fileName
let saveAsBitmap width height fileName (canvas:Canvas) = 

    let size = new Size(width, height)
    canvas.Measure(size)
    let rtb = new RenderTargetBitmap(width |> int, height |> int, 96., 96., PixelFormats.Default)
    rtb.Render(canvas)

    let fileName = __SOURCE_DIRECTORY__ + sprintf "\output\img\%s.png" fileName

    let enc = new PngBitmapEncoder()
    let bitmapFrame = BitmapFrame.Create(rtb)
    enc.Frames.Add(bitmapFrame)

    use stm = File.Create(fileName)
    enc.Save(stm)

(**
The Plot utility class
------------------------
Armed with all the function defined above we can finally implement our `Plot` utility and 
to render it flexible enough to add future functionalities we define it as a plain F# Class:
*)

/// Plots an OGR.Geometry on a Xaml Window
type Plot(geom:OGR.Geometry)  = 

    let env = geom |> env |> resize 0.8
    let maxXYSide = max (env.MaxX - env.MinX) (env.MaxY - env.MinY)

//    do printfn "Geometry Properties:"
//    do printfn "Geometry Type: %A" (geom |> geomType)
//    do printfn "Envelope: MinX = %.2f, MaxX = %.2f, MinY = %.2f, MaxY = %.2f" env.MinX env.MinY env.MaxX env.MaxY
//    do printfn "Max Envelope XY size: %.2f" maxXYSide
//    do printfn ""

    let canvas = new Canvas()
    let canvasSize = 400.
    do canvas.Width <- canvasSize
    do canvas.Height <- canvasSize
    do canvas.RenderTransformOrigin <- new Point(0.5,0.5)
    
    let scale = canvasSize / maxXYSide

    let originX = env.MinX * scale * -1.
    let originY = env.MinY * scale
    let renderTransform = sprintf "1 0 0 -1 %.2f %.2f" originX originY
    
    do canvas.RenderTransform <- Transform.Parse(renderTransform)
    
//    do printfn "Canvas Properties:"
//    do printfn "Paths scale: (canvas size = %.2f / envelope size = %.2f) = %.5f" canvasSize maxXYSide scale
//    do printfn "RenderTransform = %s" renderTransform |> ignore
//    do printfn ""
    
    let paths = geom |> toPaths scale 0 []
    let addPaths = 
        for path in paths do 
            canvas.Children.Add(path) |> ignore
    do addPaths

    let renderTransformAxisXY = sprintf "1 0 0 -1 %.2f %.2f" -originX originY
    do canvas |> axisXY scale env renderTransformAxisXY

    let winWidth, winHeight = 600., 550.
    let win = new Window()
    do win.Width <- winWidth
    do win.Height <- winHeight
    do win.Content <- canvas
    do win.Topmost <- true

    do win.Title <- "F# Geometry Plot"

    do win.Show()

    member this.SaveAsBitmap(fileName) = 
        canvas |> saveAsBitmap winWidth winHeight fileName
