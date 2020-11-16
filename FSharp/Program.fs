// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open System.IO
open System.Globalization
open System.Xml.Linq
open System.Xml.Schema
open FSharp.Data
open FSharp.Data.Runtime.CsvInference
// open ProviderImplementation.XmlInference


let f = CsvFile.Load("""C:\Work\UMG\optima\FSharp\csv.csv""")
// f.Headers

// f.InferColumnTypes(0, [||], CultureInfo.InvariantCulture, null, true, false) 
// |> Seq.map (fun r -> {| Name = r.Name; InferedType = r.InferedType.FullName |})
// |> Seq.toArray


let j = JsonValue.Load ("""C:\Work\UMG\optima\FSharp\json.json""")
let infer record =
    function 
    | JsonValue.Record props -> props |> Array.map (function    | (name, JsonValue.Boolean _) -> name, "System.Boolean"
                                                                | (name, JsonValue.Float _) -> name, "System.Double"
                                                                | (name, JsonValue.Number _) -> name, "System.Int32"
                                                                | (name, _) -> name, "System.String")  
    | _ -> [||]
    
let v = 
    match j with
    | JsonValue.Array elems when elems.Length > 0 -> elems |> Array.head |> infer
    | JsonValue.Record(properties) -> infer properties
    | _ -> failwith "Unsupported file format"
    


[<EntryPoint>]
let main argv =
    let message = from "F#" // Call the function
    printfn "Hello world %s" message
    0 // return an integer exit code