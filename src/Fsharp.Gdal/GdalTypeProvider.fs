namespace FSharp.Gdal

open System
open System.IO
open ProviderImplementation
open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices
open System.Reflection
open System.Text.RegularExpressions

open OSGeo.OGR
open OSGeo.GDAL
open OSGeo.OSR
open FSharp.Gdal.Vector
open FSharp.Gdal

// Simple type wrapping vector data
type VectorFile(filename) =
    // Cache the sequence of all data lines (all lines but the first)
    let dataset = Vector.openVector filename
    let vlay = dataset.GetLayerByIndex(0)
    let fields = vlay |> Vector.fields
    let features = 
            vlay
            |> Vector.features
            |> List.filter (fun f -> f <> null)

    let data = 
        [
            
            for feature in features -> 
                [|feature.GetGeometryRef() :> obj|]
                |> Array.append 
                    [|
                        for fdIndex,fdName,fdType in fields ->
                            if fdType.Equals(FieldType.OFTString) then
                                feature.GetFieldAsString(fdIndex) :> obj
                            elif fdType.Equals(FieldType.OFTInteger) then
                                feature.GetFieldAsInteger(fdIndex) :> obj
                            elif fdType.Equals(FieldType.OFTReal) then
                                feature.GetFieldAsDouble(fdIndex) :> obj
                            else
                                "" :> obj
                    |] 
        ] |> Seq.ofList

    let values = 
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

//        let mutable globalIndex = 0
//        [
//            for feature in features -> 
//                globalIndex <- globalIndex + 1
//                [globalIndex, "Geometry", feature.GetGeometryRef() :> obj]
//                |> List.append 
//                    [
//                        for fdIndex,fdName,fdType in fields ->
//                            if fdType.Equals(FieldType.OFTString) then
//                                globalIndex, fdName, feature.GetFieldAsString(fdIndex) :> obj
//                            elif fdType.Equals(FieldType.OFTInteger) then
//                                globalIndex, fdName, feature.GetFieldAsInteger(fdIndex) :> obj
//                            elif fdType.Equals(FieldType.OFTReal) then
//                                globalIndex, fdName, feature.GetFieldAsDouble(fdIndex) :> obj
//                            else
//                                globalIndex, fdName, "" :> obj
//                    ] 
//        ] |> List.concat

    member __.Features = data
    member __.Values = values

[<TypeProvider>]
type public OgrTypeProvider(cfg:TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces()

    // Get the assembly and namespace used to house the provided types.
    let asm = System.Reflection.Assembly.GetExecutingAssembly()
    let ns = "FSharp.Gdal"

    // Create the main provided type.
    let shpFlTy = ProvidedTypeDefinition(asm, ns, "OgrTypeProvider", Some(typeof<obj>))

    // Parameterize the type by the file to use as a template.
    let filename = ProvidedStaticParameter("filename", typeof<string>)
    do shpFlTy.DefineStaticParameters([filename], fun tyName [| :? string as filename |] ->

        Configuration.Init()

        // Resolve the filename relative to the resolution folder.
        let resolvedFilename = Path.Combine(cfg.ResolutionFolder, filename)

        // Get the first line from the file.
        let headerLine = File.ReadLines(resolvedFilename) |> Seq.head

        // Define a provided type for each row, erasing to a float[].
        let rowTy = ProvidedTypeDefinition("Feature", Some(typeof<obj[]>))

        // Extract header names from the file, splitting on commas.
        // use Regex matching to get the position in the row at which the field occurs
//        let headers = Regex.Matches(headerLine, "[^,]+")

        let dataset = Vector.openVector filename
        let vlay = dataset.GetLayerByIndex(0)
        let fields = vlay |> Vector.fields

        let geomIndex = fields.Length

        // Add one property per CSV field.
        for fdIndex,fdName,fdType in fields do

            let headerText = fdName

            if fdType.Equals(FieldType.OFTInteger) then
                let fieldName, fieldTy = headerText, typeof<int>

                let prop = ProvidedProperty(fieldName, fieldTy, 
                                                 GetterCode = fun [row] -> <@@ (%%row:obj[]).[fdIndex] @@>)

                // Add metadata that defines the property's location in the referenced file.
                prop.AddDefinitionLocation(1, fdIndex + 1, filename)
                rowTy.AddMember(prop) 
            elif fdType.Equals(FieldType.OFTReal) then
                let fieldName, fieldTy = headerText, typeof<float>

                let prop = ProvidedProperty(fieldName, fieldTy, 
                                                 GetterCode = fun [row] -> <@@ (%%row:obj[]).[fdIndex] @@>)

                // Add metadata that defines the property's location in the referenced file.
                prop.AddDefinitionLocation(1, fdIndex + 1, filename)
                rowTy.AddMember(prop) 
            else
                // Try to decompose this header into a name and unit.
                let fieldName, fieldTy = headerText, typeof<string>

                let prop = ProvidedProperty(fieldName, fieldTy, 
                                                 GetterCode = fun [row] -> <@@ (%%row:obj[]).[fdIndex] @@>)

                // Add metadata that defines the property's location in the referenced file.
                prop.AddDefinitionLocation(1, fdIndex + 1, filename)
                rowTy.AddMember(prop) 
        
        let geomProp = ProvidedProperty("Geometry", typeof<Geometry>, 
                                            GetterCode = fun [row] -> <@@ (%%row:obj[]).[geomIndex] @@>)

        // Add metadata that defines the property's location in the referenced file.
        geomProp.AddDefinitionLocation(1, geomIndex + 1, filename)
        rowTy.AddMember(geomProp) 

        // Define the provided type, erasing to VectorFile.
        let ty = ProvidedTypeDefinition(asm, ns, tyName, Some(typeof<VectorFile>))

        // Add a parameterless constructor that loads the file that was used to define the schema.
        let ctor0 = ProvidedConstructor([], 
                                        InvokeCode = fun [] -> <@@ VectorFile(resolvedFilename) @@>)
        ty.AddMember ctor0

        // Add a constructor that takes the file name to load.
        let ctor1 = ProvidedConstructor([ProvidedParameter("filename", typeof<string>)], 
                                        InvokeCode = fun [filename] -> <@@ VectorFile(%%filename) @@>)
        ty.AddMember ctor1

        // Add a more strongly typed Data property, which uses the existing property at runtime.
        let prop = ProvidedProperty("Features", typedefof<seq<_>>.MakeGenericType(rowTy), 
                                    GetterCode = fun [VectorFile] -> <@@ (%%VectorFile:VectorFile).Features @@>)
        ty.AddMember prop

        let propValues = ProvidedProperty("Values", typeof<(int * string * obj) list>, 
                                    GetterCode = fun [VectorFile] -> <@@ (%%VectorFile:VectorFile).Values @@>)
        ty.AddMember propValues

        // Add the row type as a nested type.
        ty.AddMember rowTy
        ty)

    // Add the type to the namespace.
    do this.AddNamespace(ns, [shpFlTy])

[<assembly:TypeProviderAssembly>]
do ()