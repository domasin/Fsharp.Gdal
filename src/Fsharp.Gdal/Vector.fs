/// Utility functions to access OGR Vectors
module FSharp.Gdal.Vector

open System
open OSGeo.OGR

/// Get all layers in an OGR.DataSource
let layers (ds:DataSource) = 
    let layersCount = ds.GetLayerCount()
    [for i in [0..(layersCount-1)] -> ds.GetLayerByIndex(i)]

/// Get all features in an OGR.Layer
let features (layer:Layer) = 
    layer.ResetReading() |> ignore
    // see http://www.gdal.org/ogr_apitut.html 
    // using OGRLayer::GetNextFeature(). It will return NULL when we run out of features
    let sequenceGenerator _ = layer.GetNextFeature() 
    let isNotNull = (<>) null

    // see https://fsharpforfunandprofit.com/posts/control-flow-expressions/
    // Example: Generating and printing a sequence of random numbers
    Seq.initInfinite sequenceGenerator 
        |> Seq.takeWhile isNotNull
        |> Seq.map id
        |> List.ofSeq

/// Get index, name and type of all fields in an OGR.Layer
let fields (layer:Layer) = 
    let layerDefinition = layer.GetLayerDefn()
    let fieldCount = layerDefinition.GetFieldCount()
    [for fdIndex in [0..fieldCount-1] 
        ->
            let fd = layerDefinition.GetFieldDefn(fdIndex)
            fdIndex, fd.GetName().ToUpperInvariant(), fd.GetFieldType()
    ]

/// Type to store layer's infos and number of features in it
type LayerInfo = 
    {
        Features : int
        Geometry : wkbGeometryType
        Fields   : string list
    }
    with
        override this.ToString() = 
            sprintf "{Features = %i;\nGeometry = %A\nFields = %A}" this.Features this.Geometry this.Fields

/// Returns layer's infos: function suitable to use a custom printer for 
/// layers in fsharp interactive
let layerInfo (layer:Layer) = 
    {
        Features = layer |> features |> List.length
        Geometry = layer.GetGeomType()
        Fields = 
            layer 
            |> fields
            |> List.map (fun (_,name,_) -> name)
    }

/// Type to store datasource's infos as the layers inside it
type DatasourceInfo = 
    {
        Index : int
        Layer : LayerInfo
    }
    with
        override this.ToString() = 
            sprintf "{Index = %i;\Layers = %A}" this.Index this.Layer

/// Returns layer's infos: function suitable to use a custom printer for 
/// layers in fsharp interactive
let datasourceInfo (datasource:DataSource) = 
    datasource 
    |> layers
    |> List.mapi 
        (
            fun i layer -> 
                {
                    Index = i
                    Layer = layer |> layerInfo
                }
        )

/// Returns a list of values for each feature 
/// suitable to be converted in a deedle frame
let toValues (layer:Layer) = 
    let fields = layer |> fields
    let features = layer |> features  
    features
    |> List.mapi (fun i feature -> 
            [i, "Geometry", feature.GetGeometryRef() :> obj]
            |> List.append 
                    [
                        for fdIndex,fdName,fdType in fields ->
                            if fdType.Equals(FieldType.OFTString) then
                                i, fdName, feature.GetFieldAsString(fdIndex) :> obj
                            elif fdType.Equals(FieldType.OFTInteger) then
                                i, fdName, feature.GetFieldAsInteger(fdIndex) :> obj
                            elif fdType.Equals(FieldType.OFTReal) then
                                i, fdName, feature.GetFieldAsDouble(fdIndex) :> obj
                            else
                                i, fdName, "" :> obj
                    ] 
        )
    |> List.concat