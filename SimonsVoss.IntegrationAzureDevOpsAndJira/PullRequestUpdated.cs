using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SimonsVoss.IntegrationAzureDevOpsAndJira.Models;

namespace SimonsVoss.IntegrationAzureDevOpsAndJira;

public static class PullRequestUpdated
{
    [FunctionName("PullRequestUpdated")]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
        HttpRequest req, ILogger log)
    {
        var jiraAddress = Environment.GetEnvironmentVariable("JiraBaseAddress") ?? throw new ArgumentNullException("JiraBaseAddress");
        var authToken = Environment.GetEnvironmentVariable("JiraToken") ?? throw new ArgumentNullException("JiraToken");
        var jiraKeysConf = Environment.GetEnvironmentVariable("JiraProjectKeys") ?? throw new ArgumentNullException("JiraProjectKeys");
        var azureDevOpsOrganization = Environment.GetEnvironmentVariable("AzureDevOpsOrganization") ?? throw new ArgumentNullException("AzureDevOpsOrganization");
        var azureDevOpsPersonalAccessToken = Environment.GetEnvironmentVariable("AzureDevOpsToken") ?? throw new ArgumentNullException("AzureDevOpsToken");

        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var data = JsonConvert.DeserializeObject<PullRequest>(requestBody);

        var jiraKeys = jiraKeysConf.Split(';');
        var task = data.Resource.Title;
        var url = data.Resource._links.Web.Href;
        var status = data.Resource.Status;
        var project = data.Resource.Repository.Project.Name;
        var branch = data.Resource.SourceRefName;
        var repositoryId = data.Resource.Repository.Id;
        var pullRequestId = data.Resource.PullRequestId;
        var jiraKeyNumber = "";

        foreach (var jiraKey in jiraKeys)
        {
            var result = GetJiraKey(task, jiraKey, branch, out var runAsync);
            if (result.result)
            {
                jiraKeyNumber = result.key;
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(jiraKeyNumber))
        {
            log.LogInformation("{Key} does not exist in request");
            return new NotFoundResult();
        }

        await SendLinkToJira(log, jiraAddress, authToken, url, status, project, task, jiraKeyNumber);
        await UpdateAzrueDevops(log, azureDevOpsPersonalAccessToken, jiraAddress, jiraKeyNumber, azureDevOpsOrganization, project, repositoryId, pullRequestId);

        return new OkResult();
    }

    private static async Task UpdateAzrueDevops(ILogger log, string personalAccessToken, string jiraAddress,
        string jiraKeyNumber, string azureDevOpsOrganization, string project, string repositoryId, int pullRequestId)
    {
        using var client = new HttpClient();

        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", personalAccessToken);

        var requestAzure = new
        {
            Description = $"{jiraAddress}/browse/{jiraKeyNumber}"
        };

        var json = JsonConvert.SerializeObject(requestAzure);
        var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
        var azureDevOpsUrl =
            $"https://dev.azure.com/{azureDevOpsOrganization}/{project}/_apis/git/repositories/{repositoryId}/pullrequests/{pullRequestId}?api-version=7.2-preview.1";

        using var response = await client.PatchAsync(azureDevOpsUrl, stringContent);
        log.LogInformation("Azure update response: {Response}, {Code}", response.StatusCode, response.RequestMessage);
    }

    private static async Task SendLinkToJira(ILogger log, string jiraAddress, string authToken, string url,
        string status,
        string project, string task, string jiraKeyNumber)
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(jiraAddress)
        };

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

        var request = new
        {
            globalId = url,
            @object = new
            {
                title = $"{status.ToUpper()}. {project} ({task})",
                url = url
            }
        };

        var json = JsonConvert.SerializeObject(request);
        var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
        log.LogInformation("Json: {Json}", json);
        var requestResult = await httpClient.PostAsync($"/rest/api/3/issue/{jiraKeyNumber}/remotelink", stringContent);
        log.LogInformation("Result: {Result}, Code: {Code}", requestResult.RequestMessage, requestResult.StatusCode);
    }

    private static (bool result, string key) GetJiraKey(string task, string jiraKey, string branch,
        out IActionResult runAsync)
    {
        var jiraKeyWithNumber = $"{jiraKey}-";
        if (task.Contains(jiraKeyWithNumber, StringComparison.InvariantCultureIgnoreCase))
        {
            var startIndex = task.IndexOf(jiraKeyWithNumber, StringComparison.InvariantCultureIgnoreCase);
            for (var i = startIndex + jiraKeyWithNumber.Length; i < task.Length; i++)
            {
                if (char.IsNumber(task[i]))
                {
                    jiraKeyWithNumber += task[i];
                    continue;
                }

                break;
            }
        }
        else if (branch.Contains(jiraKeyWithNumber, StringComparison.InvariantCultureIgnoreCase))
        {
            var startIndex = branch.IndexOf(jiraKeyWithNumber, StringComparison.InvariantCultureIgnoreCase);
            for (var i = startIndex + jiraKeyWithNumber.Length; i < branch.Length; i++)
            {
                if (char.IsNumber(branch[i]))
                {
                    jiraKeyWithNumber += branch[i];
                    continue;
                }

                break;
            }
        }
        else
        {
            {
                runAsync = new NotFoundResult();
                return (false, "");
            }
        }

        runAsync = new OkResult();
        return (true, jiraKeyWithNumber);
    }
}