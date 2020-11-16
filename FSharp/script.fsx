#r "nuget: FSharp.Data"

open System
open System.IO
open System.Globalization
open System.Xml.Linq
open System.Xml.Schema
open FSharp.Data
open FSharp.Data.Runtime.CsvInference



let f = CsvFile.Load("""C:\Work\UMG\optima\FSharp\csv.csv""")
f.Headers

f.InferColumnTypes(0, [||], CultureInfo.InvariantCulture, null, true, false) 
|> Seq.map (fun r -> {| Name = r.Name; InferedType = r.InferedType.FullName |})
|> Seq.toArray


let j = JsonValue.Load ("""C:\Work\UMG\optima\FSharp\json.json""")


let x = [| XDocument.Parse("""C:\Work\UMG\optima\FSharp\xml.xml""").Root |] 

let inferType inferTypesFromValues cultureInfo allowEmptyValues globalInference (elements:XElement[]) =