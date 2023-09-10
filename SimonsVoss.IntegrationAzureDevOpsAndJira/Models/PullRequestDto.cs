using System.Text.Json.Serialization;
using SimonsVoss.IntegrationAzureDevOpsAndJira.Models.SubModels;

namespace SimonsVoss.IntegrationAzureDevOpsAndJira.Models;

public class PullRequest
{
    public string Id { get; set; }
    public string EventType { get; set; }
    public string PublisherId { get; set; }
    public string Scope { get; set; }
    public Message Message { get; set; }
    public Message DetailedMessage { get; set; }
    public Resource Resource { get; set; }
    public string ResourceVersion { get; set; }
    public string CreatedDate { get; set; }
}