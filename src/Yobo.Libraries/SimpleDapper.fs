module Yobo.Libraries.SimpleDapper

open System.Text

type OrderDirection =
    | Asc
    | Desc

type OrderBy = string * OrderDirection
    
type WhereComparison =
    | Eq of obj
    | Ne of obj
    | Gt of obj
    | Lt of obj
    | Ge of obj
    | Le of obj
    | In of obj list
    | NotIn of obj list
    | IsNull
    | IsNotNull

type WhereCombination =
    | And
    | Or

type Where =
    | Empty
    | Field of string * WhereComparison
    | Binary of Where * WhereCombination * Where
    static member (+) (a, b) = Binary(a, And, b)
    static member (*) (a, b) = Binary(a, Or, b)

type private FieldWhereMetadata = {
    Key : string * WhereComparison
    Name : string
    ParameterName : string
}

type SelectQuery<'a> = {
    Table : string
    Where : Where
    OrderBy : OrderBy list
    Paging : (int * int) option
}

type InsertQuery<'a> = {
    Table : string
    Values : 'a list
}

type UpdateQuery<'a> = {
    Table : string
    Value : 'a
    Where : Where
}

type DeleteQuery = {
    Table : string
    Where : Where
}

let private evalCombination = function
    | And -> "AND"
    | Or -> "OR"

let private evalOrderDirection = function
    | Asc -> "ASC"
    | Desc -> "DESC"

let rec private evalWhere (meta:FieldWhereMetadata list) (w:Where) =
    match w with
    | Empty -> ""
    | Field (field, comp) ->
        let fieldMeta = meta |> List.find (fun x -> x.Key = (field,comp)) 
        let withField op = sprintf "%s %s @%s" fieldMeta.Name op fieldMeta.ParameterName
        match comp with
        | Eq _ -> withField "="
        | Ne _ -> withField "<>"
        | Gt _ -> withField ">"
        | Lt _ -> withField "<"
        | Ge _ -> withField ">="
        | Le _ -> withField "<="
        | In _ -> withField "IN"
        | NotIn _ -> withField "NOT IN"
        | IsNull -> sprintf "%s IS NULL" fieldMeta.Name
        | IsNotNull -> sprintf "%s IS NOT NULL" fieldMeta.Name
    | Binary(w1, comb, w2) ->
        match evalWhere meta w1, evalWhere meta w2 with
        | "", fq | fq , "" -> fq
        | fq1, fq2 -> sprintf "(%s %s %s)" fq1 (evalCombination comb) fq2

let private evalOrderBy (xs:OrderBy list) =
    xs
    |> List.map (fun (n,s) -> sprintf "%s %s" n (evalOrderDirection s))
    |> String.concat ", "

let private evalPaging (pageSize:int, page:int) =
    let skip = pageSize * page
    sprintf "OFFSET %i ROWS FETCH NEXT %i ROWS ONLY" skip pageSize

let private extractWhereParams (meta:FieldWhereMetadata list) =
    let fn (m:FieldWhereMetadata) =
        match m.Key |> snd with
        | Eq p | Ne p | Gt p
        | Lt p | Ge p | Le p -> (m.ParameterName, p) |> Some
        | In p | NotIn p -> (m.ParameterName, p :> obj) |> Some
        | IsNull | IsNotNull -> None
    meta
    |> List.choose fn

let rec private getWhereMetadata (meta:FieldWhereMetadata list) (w:Where)  =
    match w with
    | Empty -> meta
    | Field (field, comp) ->
        let parName =
            meta
            |> List.filter (fun x -> x.Name = field)
            |> List.length
            |> fun l -> sprintf "Where_%s%i" field (l + 1)
        meta @ [{ Key = (field, comp); Name = field; ParameterName = parName }]
    | Binary(w1, _, w2) -> [w1;w2] |> List.fold getWhereMetadata meta            

let private getFields<'a> () =
    FSharp.Reflection.FSharpType.GetRecordFields(typeof<'a>)
    |> Array.map (fun x -> x.Name)
    |> Array.toList

let private getValues r =
    FSharp.Reflection.FSharpValue.GetRecordFields r
    |> Array.toList

let private evalSelectQuery fields meta (q:SelectQuery<_>) =
    let fieldNames = fields |> String.concat ", "
    // basic query
    let sb = StringBuilder(sprintf "SELECT %s FROM %s" fieldNames q.Table)
    // where
    let where = evalWhere meta q.Where
    if where.Length > 0 then sb.Append (sprintf " WHERE %s" where) |> ignore
    // order by
    let orderBy = evalOrderBy q.OrderBy
    if orderBy.Length > 0 then sb.Append (sprintf " ORDER BY %s" orderBy) |> ignore
    // paging
    let paging = q.Paging |> Option.map evalPaging |> Option.defaultValue ""
    if paging.Length > 0 then sb.Append (sprintf " %s" paging) |> ignore    
    sb.ToString()

let private evalInsertQuery fields (q:InsertQuery<_>) =
    let fieldNames = fields |> String.concat ", " |> sprintf "(%s)"
    let values = fields |> List.map (sprintf "@%s") |> String.concat ", " |> sprintf "(%s)" 
    sprintf "INSERT INTO %s %s VALUES %s" q.Table fieldNames values        

let private evalUpdateQuery fields meta (q:UpdateQuery<_>) =
    // basic query
    let pairs = fields |> List.map (fun x -> sprintf "%s=@%s" x x) |> String.concat ", "
    let sb = StringBuilder(sprintf "UPDATE %s SET %s" q.Table pairs)
    // where
    let where = evalWhere meta q.Where
    if where.Length > 0 then sb.Append (sprintf " WHERE %s" where) |> ignore
    sb.ToString()

let private evalDeleteQuery meta (q:DeleteQuery) =
    // basic query
    let sb = StringBuilder(sprintf "DELETE FROM %s" q.Table)
    // where
    let where = evalWhere meta q.Where
    if where.Length > 0 then sb.Append (sprintf " WHERE %s" where) |> ignore
    sb.ToString()
    
let select<'a> tableName =
    {
        Table = tableName
        Where = Where.Empty
        OrderBy = []
        Paging = None
    } : SelectQuery<'a>
    
let where w (q:SelectQuery<_>) = { q with Where = q.Where + w }
let orderBy o q = { q with OrderBy = q.OrderBy @ [o] }
let paging pageSize page q = { q with Paging = Some (pageSize, page) }
let insert<'a> tableName values =
    {
        Table = tableName
        Values = values
    } : InsertQuery<'a>
let update<'a> tableName value where =
    {
        Table = tableName
        Value = value
        Where = where
    } : UpdateQuery<'a>
let delete tableName where =
    {
        Table = tableName
        Where = where
    } : DeleteQuery

open System
open Dapper

type System.Data.IDbConnection with
    member this.SelectAsync<'a> (q:SelectQuery<'a>) =
        let fields = getFields<'a>()
        // extract metadata
        let meta = getWhereMetadata [] q.Where
        let query = evalSelectQuery fields meta q
        let pars = extractWhereParams meta |> Map.ofList
        this.QueryAsync<'a>(query, pars)
        
    member this.InsertAsync<'a> (q:InsertQuery<'a>) =
        let fields = getFields<'a>()
        let query = evalInsertQuery fields q
        this.ExecuteAsync(query, q.Values)
        
    member this.UpdateAsync<'a> (q:UpdateQuery<'a>) =
        let fields = getFields<'a>()
        let values = getValues q.Value
        // extract metadata
        let meta = getWhereMetadata [] q.Where
        let pars = (extractWhereParams meta) @ (List.zip fields values) |> Map.ofList
        let query = evalUpdateQuery fields meta q
        this.ExecuteAsync(query, pars)
    
    member this.DeleteAsync<'a> (q:DeleteQuery) =
        // extract metadata
        let meta = getWhereMetadata [] q.Where
        let pars = (extractWhereParams meta) |> Map.ofList
        let query = evalDeleteQuery meta q
        this.ExecuteAsync(query, pars)

module FSharp =
    
    type OptionHandler<'T>() =
        inherit SqlMapper.TypeHandler<option<'T>>()

        override __.SetValue(param, value) = 
            let valueOrNull = 
                match value with
                | Some x -> box x
                | None -> null

            param.Value <- valueOrNull    

        override __.Parse value =
            if isNull value || value = box DBNull.Value 
            then None
            else Some (value :?> 'T)
            
    let registerOptionTypes() =
        SqlMapper.AddTypeHandler (OptionHandler<Guid>())
        SqlMapper.AddTypeHandler (OptionHandler<int64>())
        SqlMapper.AddTypeHandler (OptionHandler<int>())
        SqlMapper.AddTypeHandler (OptionHandler<int16>())
        SqlMapper.AddTypeHandler (OptionHandler<string>())
        SqlMapper.AddTypeHandler (OptionHandler<char>())
        SqlMapper.AddTypeHandler (OptionHandler<DateTime>())
        SqlMapper.AddTypeHandler (OptionHandler<DateTimeOffset>())
        SqlMapper.AddTypeHandler (OptionHandler<bool>())