/// Units and unit conversions
module FSharp.Gdal.UM

/// grades
[<Measure>] type g

/// radians
[<Measure>] type c

/// Converts grades to radians
let toRadians x = 
    let g = x / 1.0<g>
    MathNet.Numerics.Trig.DegreeToRadian(g) * 1.<c>

/// meters
[<Measure>] type m

/// kilometers
[<Measure>] type km
let kmToM = 1000.0<m/km>
let mToKm = 1.0 / kmToM

/// hectars
[<Measure>] type ha
let haToMq = 10000.0<m^2/ha>
let mqToHa = 1.0 / haToMq

[<Measure>] type s
[<Measure>] type h
let hrToSec = 3600.0<s/h>
let secToHr = 1.0 / hrToSec

let msToKmph (speed : float<m/s>) = speed / kmToM * hrToSec
let kmphToMs (speed : float<km/h>) = speed * kmToM / hrToSec