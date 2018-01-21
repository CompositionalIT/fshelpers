module String

open System

let [<Literal>] private DefaultComparison = StringComparison.Ordinal

[<CompiledName("Contains")>]
let contains (value:string) (str:string) =
    str.Contains(value)

[<CompiledName("Compare")>]
let compare (strB:string) (strA:string) =
    String.Compare(strA, strB, DefaultComparison)

[<CompiledName("EndsWith")>]
let endsWith (value:string) (str:string) =
    str.EndsWith(value, DefaultComparison)

[<CompiledName("Equals")>]
let equals (comparisonType:StringComparison) (value:string) (str:string) =
    str.Equals(value, comparisonType)

let inline private checkIndex func (comparisonType:StringComparison) value =
    let index = func(value, comparisonType)
    if index = -1 then None
    else Some index
    
[<CompiledName("IndexOf")>]
let indexOf (value:string) (str:string) =
    checkIndex str.IndexOf DefaultComparison value

[<CompiledName("LastIndexOf")>]
let lastIndexOf (value:string) (str:string) =
    checkIndex str.LastIndexOf DefaultComparison value

[<CompiledName("ReplaceChar")>]
let replaceChar (oldChar:char) (newChar:char) (str:string) =
    str.Replace(oldChar, newChar)

[<CompiledName("Replace")>]
let replace (oldValue:string) (newValue:string) (str:string) =
    str.Replace(oldValue, newValue)

[<CompiledName("Split")>]
let split (separator:char) (str:string) =
    str.Split([| separator |], StringSplitOptions.None)

[<CompiledName("SplitRemoveEmptyEntries")>]
let splitRemoveEmptyEntries (separator:char) (str:string) =
    str.Split([| separator |], StringSplitOptions.RemoveEmptyEntries)

[<CompiledName("SplitString")>]
let splitString (separator:string) (str:string) =
    str.Split([| separator |], StringSplitOptions.None)

[<CompiledName("SplitStringRemoveEmptyEntries")>]
let splitStringRemoveEmptyEntries (separator:string) (str:string) =
    str.Split([| separator |], StringSplitOptions.RemoveEmptyEntries)

[<CompiledName("StartsWith")>]
let startsWith (value:string) (str:string) = 
    str.StartsWith(value, DefaultComparison)

[<CompiledName("SubstringLength")>]
let substringLength (startIndex:int) (length: int) (str:string) =
    str.Substring(startIndex, length)

[<CompiledName("Substring")>]
let substring (startIndex:int) (str:string) =
    str.Substring(startIndex)

[<CompiledName("ToLower")>]
let toLower(str:string) =
    str.ToLowerInvariant()

[<CompiledName("ToUpper")>]
let toUpper(str:string) =
    str.ToUpperInvariant()

[<CompiledName("Trim")>]
let trim(str:string) =
    str.Trim()

[<CompiledName("TrimChars")>]
let trimChars (trimChars:char []) (str:string) =
    str.Trim(trimChars)

[<CompiledName("TrimStart")>]
let trimStart (trimChars:char []) (str:string) =
    str.TrimStart(trimChars)
    
[<CompiledName("TrimEnd")>]
let trimEnd(trimChars:char []) (str:string) =
    str.TrimEnd(trimChars)

[<CompiledName("ToList")>]
let toList(str:string) =
    str |> Seq.toList

[<CompiledName("ToArray")>]
let toArray(str:string) =
    str.ToCharArray()

[<CompiledName("OfArray")>]
let ofArray(chars:char array) =
    chars |> String

[<CompiledName("OfList")>]
let ofList(chars:char list) =
    chars |> List.toArray |> ofArray

[<CompiledName("OfSeq")>]
let ofSeq(chars:char seq) =
    chars |> Seq.toArray |> ofArray
