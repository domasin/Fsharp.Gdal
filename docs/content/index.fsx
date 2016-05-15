(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.

(**
Fsharp.Gdal
======================

Documentation

<!--<div class="row">
  <div class="span1"></div>
  <div class="span6">
    <div class="well well-small" id="nuget">
      The Fsharp.Gdal library can be <a href="https://nuget.org/packages/Fsharp.Gdal">installed from NuGet</a>:
      <pre>PM> Install-Package Fsharp.Gdal</pre>
    </div>
  </div>
  <div class="span1"></div>
</div>-->

Example
-------

This example demonstrates using a function defined in this sample library.

*)

#I "../../src/Fsharp.Gdal/bin/Debug"

#r "Fsharp.Gdal.dll"
#r "gdal_csharp.dll"
#r "gdalconst_csharp.dll"
#r "ogr_csharp.dll"
#r "osr_csharp.dll"

open FSharp.Gdal
open OSGeo

Configuration.Init() |> ignore

let point = new OGR.Geometry(OGR.wkbGeometryType.wkbPoint)
point.AddPoint(1198054.34, 648493.09,0.)
printfn "%A" (point.ExportToWkt())

(**
Some more info

Samples & documentation
-----------------------

The library comes with comprehensible documentation. 
It can include tutorials automatically generated from `*.fsx` files in [the content folder][content] 
and documents generated with FsLab Journal Template in the project named "Journal". 
The API reference is automatically generated from Markdown comments in the library implementation.

 * FSharp Gdal CookBook: an equivalent of Python Gdal / Ogr Cookbook
    - [Geometry](geometry.html): working with geometries with `OGR` functions
    - [Appendix A: Plot Geometries](plot-geometries.html): a rudimentary `plot` function for geometries
    - [Appendix B: F# Gdal Type Provider](gdal-type-provider.html): an experimental gdal type provider
 * Applications: 
    - [Extimated Walk Time](extimated-walk-time.html): calculate an extimated time for hiking
    - [Land Cover in Valgrande](land-cover.html): land cover analysis in the [Val Grande National Park](https://en.wikipedia.org/wiki/Val_Grande_National_Park)
    - Crimes in San Francisco: TODO

 * [API Reference](reference/index.html) contains automatically generated documentation for all types, modules
   and functions in the library. This includes additional brief samples on using most of the
   functions.
 
Contributing and copyright
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork 
the project and submit pull requests. If you're adding a new public API, please also 
consider adding [samples][content] that can be turned into a documentation. You might
also want to read the [library design notes][readme] to understand how it works.

The library is available under Public Domain license, which allows modification and 
redistribution for both commercial and non-commercial purposes. For more information see the 
[License file][license] in the GitHub repository. 

  [content]: https://github.com/domasin/Fsharp.Gdal/tree/master/docs/content
  [gh]: https://github.com/domasin/Fsharp.Gdal
  [issues]: https://github.com/domasin/Fsharp.Gdal/issues
  [readme]: https://github.com/domasin/Fsharp.Gdal/blob/master/README.md
  [license]: https://github.com/domasin/Fsharp.Gdal/blob/master/LICENSE.txt
*)
