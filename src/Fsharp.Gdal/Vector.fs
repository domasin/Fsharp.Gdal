/// Utility functions to access OGR Vectors
module FSharp.Gdal.Vector

open OSGeo.OGR
open System

/// Get all layers in an OGR.DataSource
let layers (ds:DataSource) = 
    let layersCount = ds.GetLayerCount()
    [for i in [0..(layersCount-1)] -> ds.GetLayerByIndex(i)]

/// Get all features in an OGR.Layer
let features (layer:Layer) = 
    layer.ResetReading() |> ignore
    let fc = layer.GetFeatureCount(0)
    [for _ in [1..fc] -> layer.GetNextFeature()]

/// Get index, name and type of all fields in an OGR.Layer
let fields (layer:Layer) = 
    let layerDefinition = layer.GetLayerDefn()
    let fieldCount = layerDefinition.GetFieldCount()
    [for fdIndex in [0..fieldCount-1] 
        ->
            let fd = layerDefinition.GetFieldDefn(fdIndex)
            fdIndex, fd.GetName().ToUpperInvariant(), fd.GetFieldType()
    ]