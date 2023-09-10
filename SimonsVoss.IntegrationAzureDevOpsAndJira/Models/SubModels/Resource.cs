using System.Text.Json.Serialization;
using SimonsVoss.IntegrationAzureDevOpsAndJira.Models.SubModels;

namespace SimonsVoss.IntegrationAzureDevOpsAndJira.Models;

public class Resource
{
    public Repository Repository { get; set; }
    public int PullRequestId { get; set; }
    public string Status { get; set; }
    public User CreatedBy { get; set; }
    public string CreationDate { get; set; }
    public string ClosedDate { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string SourceRefName { get; set; }
    public string TargetRefName { get; set; }
    public string MergeStatus { get; set; }
    public string MergeId { get; set; }
    public Commit LastMergeSourceCommit { get; set; }
    public Commit LastMergeTargetCommit { get; set; }
    public Commit LastMergeCommit { get; set; }
    public User[] Reviewers { get; set; }
    public Commit[] Commits { get; set; }
    public string Url { get; set; }
    public Links _links { get; set; }
}