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

let coordinates (geom:OGR.Geometry) = 
    let last = geom.GetPointCount() - 1
    [
        for i in 0..last ->
            let p = [|0.;0.|]
            geom.GetPoint(i,p)
            (p.[0], p.[1])
    ]

let mediaGeomStr closed coords = 
    let res = 
        coords
        |> List.fold (fun acc (x,y) -> 
                if acc = "" then
                    acc + (sprintf "F1 M%f,%f " x y)
                else
                    acc + (sprintf " L%f,%f" x y)
            ) ""
    res + if closed then "z" else ""

let getRandomBrush seed  = 
    let brushArray = 
        typeof<Brushes>.GetProperties() 
        |> Array.map (fun c -> c.Name)

    let randomGen = new Random(seed)
    let randomColorName = brushArray.[randomGen.Next(brushArray.Length)]
    let color = (new BrushConverter()).ConvertFromString(randomColorName) :?> SolidColorBrush
    color

let geometryDrawing seed (geomType:OGR.wkbGeometryType) = 
    let geometryDrawing = new GeometryDrawing()

    match (geomType) with
    | OGR.wkbGeometryType.wkbLineString
    | OGR.wkbGeometryType.wkbLineString25D -> 
        let pen = new Pen()
        pen.Brush <- getRandomBrush seed
        pen.Thickness <- 0.1
        geometryDrawing.Pen <- pen
        geometryDrawing.Brush <- Brushes.White
    | _ ->
        geometryDrawing.Brush <- getRandomBrush seed

    geometryDrawing

let rec geometryDrawings seed xs (geomType:OGR.wkbGeometryType) (geom:OGR.Geometry) = 
    let count = geom.GetGeometryCount()
    match count with
    | count when count = 0 -> 
        let geometryDrawing = geomType |> geometryDrawing seed
        let mediaGeomStr = geom |> coordinates |> mediaGeomStr (geom.IsRing())
        geometryDrawing.Geometry <- Media.Geometry.Parse(mediaGeomStr)
        [geometryDrawing]@xs
    | _ -> 
        [for i in [0..(count-1)] |> List.rev -> // we need to reverse to visualize holes
            (geom.GetGeometryRef(i)) |> geometryDrawings i xs geomType
        ] |> List.concat

[0..10] |> List.rev

let plotShape (geom:OGR.Geometry) = 
    let geomDrawings = geom |> geometryDrawings 0 [] (geom.GetGeometryType())
    let drawingGroup = new DrawingGroup()
    for gd in geomDrawings do 
        drawingGroup.Children.Add(gd) |> ignore

    let drawingImage = new DrawingImage()
    drawingImage.Drawing <- drawingGroup

    let img = new Image()
    img.Source <- drawingImage

    let win = new Window()
    win.Content <-img

    win.Show()

// Create outer ring
let outRing = new OGR.Geometry(OGR.wkbGeometryType.wkbLinearRing)
outRing.AddPoint(1., 1., 0.)
outRing.AddPoint(4., 1., 0.)
outRing.AddPoint(4., 4., 0.)
outRing.AddPoint(1., 4., 0.)
outRing.AddPoint(1., 1., 0.)

//outRing |> plotShape

// Create inner ring
let innerRing = new OGR.Geometry(OGR.wkbGeometryType.wkbLinearRing)
innerRing.AddPoint(2., 2., 0.)
innerRing.AddPoint(3., 2., 0.)
innerRing.AddPoint(3., 3., 0.)
innerRing.AddPoint(2., 3., 0.)
innerRing.AddPoint(2., 2., 0.)

// Create polygon
let polyWithHoles = new OGR.Geometry(OGR.wkbGeometryType.wkbPolygon)

(** 
Inner rings must be added before to create a hole
*)
polyWithHoles.AddGeometry(innerRing)
polyWithHoles.AddGeometry(outRing)

polyWithHoles |> plotShape

let line = new OGR.Geometry(OGR.wkbGeometryType.wkbLineString)
line.AddPoint(1.,1., 0.)
line.AddPoint(2.,2.5, 0.)
line.AddPoint(5.,3., 0.)
line.AddPoint(10.,0., 0.)
line.AddPoint(1.,1., 0.)

line |> plotShape

let point = new OGR.Geometry(OGR.wkbGeometryType.wkbPoint)
point.AddPoint(1., 1.,0.)

point |> plotShape
