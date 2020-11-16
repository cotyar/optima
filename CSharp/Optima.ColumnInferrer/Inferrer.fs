namespace Optima.ColumnInferrer

module Inferrers =
    open System
    open System.IO
    open System.Globalization
//    open System.Xml.Linq
//    open System.Xml.Schema
    open FSharp.Data
    open FSharp.Data.Runtime.CsvInference
    
    let inferCsvColumns () =
        let f = CsvFile.Load("""C:\Work\UMG\optima\FSharp\csv.csv""")

        f.InferColumnTypes(0, [||], CultureInfo.InvariantCulture, null, true, false) 
//        |> Seq.map (fun r -> {| Name = r.Name; InferedType = r.InferedType.FullName |})
        |> Seq.map (fun r -> r.Name, r.InferedType.FullName)
        |> Seq.toArray
        
    let inferJsonColumns () =
        let j = JsonValue.Load ("""C:\Work\UMG\optima\FSharp\json.json""")
        let infer =
            function 
            | JsonValue.Record props -> props |> Array.map (function    | (name, JsonValue.Boolean _) -> name, "System.Boolean"
                                                                        | (name, JsonValue.Float _) -> name, "System.Double"
                                                                        | (name, JsonValue.Number _) -> name, "System.Int32"
                                                                        | (name, _) -> name, "System.String")  
            | _ -> [||]
            
        match j with
        | JsonValue.Array elems when elems.Length > 0 -> elems |> Array.head |> infer
        | JsonValue.Record _ as r -> infer r
        | _ -> failwith "Unsupported file format"