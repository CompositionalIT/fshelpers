module Cit.Helpers.Arm

open Microsoft.Azure.Management.Fluent
open Microsoft.Azure.Management.ResourceManager.Fluent
open Microsoft.Azure.Management.ResourceManager.Fluent.Authentication
open Microsoft.Azure.Management.ResourceManager.Fluent.Deployment.Definition
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open System

type ParameterValue = { value : string } static member Create value = { value = value }
type OutputResult = { Type : string; Value : string }
type DeploymentOutputs = Map<string, string>
type AuthenticationCredentials = { ClientId : Guid; ClientSecret : string; TenantId : Guid }
type DeploymentStatus = DeploymentInProgress of state:string * operations:int | DeploymentError of statusCode:string * message:string | DeploymentCompleted of deployment:IDeployment * DeploymentOutputs

let private (|Accepted|Running|Succeeded|Failed|Other|) = function
    | "Accepted" -> Accepted
    | "Running" -> Running
    | "Failed" -> Failed
    | "Succeeded" -> Succeeded
    | other -> Other other

/// Authenticates to Azure using the supplied credentials for a specific subscription.
let authenticateToAzure credentials (subscriptionId:Guid) =
    let spi = AzureCredentialsFactory().FromServicePrincipal(string credentials.ClientId, credentials.ClientSecret, string credentials.TenantId, AzureEnvironment.AzureGlobalCloud)
    Azure
        .Configure()
        .Authenticate(spi)
        .WithSubscription(string subscriptionId)

let private toDeploymentOutputs : obj -> DeploymentOutputs = function
    | :? JObject as outputs ->
        outputs
        |> string
        |> Newtonsoft.Json.JsonConvert.DeserializeObject<Map<string, OutputResult>>
        |> Map.map(fun _ v -> v.Value)
    | _ -> failwith "Unknown output type!"

/// Deploys an ARM template, providing a stream of progress updates and culminating with the deployment and any outputs.
let rec deployTemplate (deployment:IDeployment) = seq {
    let deployment = deployment.Refresh()
    let operations = deployment.DeploymentOperations.List() |> Seq.toArray
    yield DeploymentInProgress(deployment.ProvisioningState, operations.Length)
    match deployment.ProvisioningState with
    | Running | Accepted | Other _ ->            
        Async.Sleep 5000 |> Async.RunSynchronously
        yield! deployTemplate deployment
    | Failed ->
        yield!
            operations
            |> Array.choose(fun operation ->
                match operation.ProvisioningState with
                | Failed -> Some(operation.StatusCode, string operation.StatusMessage)
                | _ -> None)
            |> Seq.map DeploymentError
        failwith "Failed to complete deployment successfully."
    | Succeeded -> yield DeploymentCompleted (deployment, deployment.Outputs |> toDeploymentOutputs) }

/// Creates parameters from key/value string pairs used by the Fluent API.
let buildArmParameters keyValues =
    keyValues
    |> Seq.map(fun (k, v) -> k, ParameterValue.Create v)
    |> JsonConvert.SerializeObject


