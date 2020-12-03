#r "nuget: FSharp.Data"

open System
open System.IO
open System.Globalization
open System.Xml.Linq
open System.Xml.Schema
open FSharp.Data
open FSharp.Data.Runtime.CsvInference
// open ProviderImplementation.XmlInference


let f = CsvFile.Load("""C:\Work\UMG\optima\FSharp\csv.csv""")
f.Headers

f.InferColumnTypes(0, [||], CultureInfo.InvariantCulture, null, true, false) 
|> Seq.map (fun r -> {| Name = r.Name; InferedType = r.InferedType.FullName |})
|> Seq.toArray


let j = JsonValue.Load ("""C:\Work\UMG\optima\FSharp\json.json""")
match j with
| 


// let x = [| XDocument.Load("""C:\Work\UMG\optima\FSharp\xml.xml""").Root |] 
//         |> FSharp.Data.ProviderImplementation.XmlInference.XmlInference.inferType inferTypesFromValues (TextRuntime.GetCulture cultureStr) (*allowEmptyValues*)false (*globalInference*) true

// let inferType inferTypesFromValues cultureInfo allowEmptyValues globalInference (elements:XElement[]) =