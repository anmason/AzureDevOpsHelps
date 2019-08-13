using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static BuildsTriggeredPerUser.models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;

namespace BuildsTriggeredPerUser
{
    public static class BuildsTriggeredPerUser
    {
        public class inputs
        {
            public string Pat { get; set; } // personal access token for the Azure DevOps, pulled from app settings 
            public string Org { get; set; } // name of the Azure DevOps org to search in
            public string scope { get; set; } // what to scope the count by, either Project, Repository, Queue, or All
            public string witID { get; set; }  // ID of the existing work item to upload the report to
            public string project { get; set; } // project where the above work item (witID) exists
        }

        [FunctionName("BuildsTriggeredPerUser")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Begin BuildsTriggeredPerUser function");

            // read the request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            inputs theInputs = JsonConvert.DeserializeObject<inputs>(requestBody);
            theInputs.Pat = Environment.GetEnvironmentVariable("pat");
            theInputs.scope = theInputs.scope.ToLower();

            // check that all required parameters are present
            inputCheck ic = InvalidInputs(theInputs, log);
            if (ic.failed)
            {
                // at least one input was missing, inform user and end
                log.LogInformation($"The parameter \"{ic.Field}\" is {ic.HowFailed}");
                return new BadRequestObjectResult($"The parameter \"{ic.Field}\" is {ic.HowFailed}");
            }
            else
            {
                // all inputs are present, proceed with getting the builds
                
                // first get all the projects in the org
                List<projectDetails> projects = getProjects(theInputs);

                // quick error check
                if(projects == null)
                {
                    log.LogInformation("GetProjects API failed");
                    return new BadRequestObjectResult("Unable to retrieve projects - API call unsuccessful");
                }

                // next, get the builds
                List<buildDetails> builds = getBuilds(projects, theInputs);

                // count the builds per user
                // using SortedDictionary here to ensure columns line up correctly for CSV
                SortedDictionary<string, SortedDictionary<string, int>>  usage = getUsage(builds, theInputs.scope);

                // check if we found any users who performed builds
                if (usage.Count > 0)
                {
                    // create CSV report
                    string csv = createCSV(usage);
                    bool success = uploadToOrg(theInputs, csv);
                    if(success)
                    {
                        log.LogInformation("Successfully uploaded the report");
                        return new OkObjectResult($"CSV Uploaded Successfully");
                    }
                    else
                    {
                        // inform that we failed to upload the report to Azure DevOps
                        log.LogInformation("Failed to upload the report");
                        return new BadRequestObjectResult("Failed to upload the report");
                    }
                }
                else
                {
                    // inform that no usage was found
                    log.LogInformation("Didn't find any build usage");
                    return new BadRequestObjectResult("No Usage Found");
                }
            }
        }

        private static inputCheck InvalidInputs(inputs theInputs, ILogger log)
        {
            inputCheck ic = new inputCheck();
            ic.failed = false;

            if (string.IsNullOrEmpty(theInputs.Pat))
            {
                log.LogInformation("PAT is null");
                ic.failed = true;
                ic.Field = "PAT";
                ic.HowFailed = "null or missing";
                return ic;
            }
            else if (string.IsNullOrEmpty(theInputs.Org))
            {
                log.LogInformation("Org is null");
                ic.failed = true;
                ic.Field = "Org";
                ic.HowFailed = "null or missing";
                return ic;
            }
            else if (string.IsNullOrEmpty(theInputs.scope))
            {
                log.LogInformation("Scope is null");
                ic.failed = true;
                ic.Field = "Scope";
                ic.HowFailed = "null or missing";
                return ic;
            }
            else if (string.IsNullOrEmpty(theInputs.witID))
            {
                log.LogInformation("witID is null");
                ic.failed = true;
                ic.Field = "witID";
                ic.HowFailed = "null or missing";
                return ic;
            }
            else if (string.IsNullOrEmpty(theInputs.project))
            {
                log.LogInformation("Project is null");
                ic.failed = true;
                ic.Field = "Project";
                ic.HowFailed = "null or missing";
                return ic;
            }
            else
            {
                // nothing was wrong with the input
                return ic;
            }
        }

        private static List<projectDetails> getProjects(inputs theInputs)
        {
            // get all the projects in the org
            string url = $"https://dev.azure.com/{theInputs.Org}/_apis/projects?api-version=5.1";

            string response = RunGetAPI(theInputs.Pat, url);
            if (!string.IsNullOrEmpty(response))
            {
                project ret = JsonConvert.DeserializeObject<project>(response);
                return ret.value;
            }
            else
            {
                return null;
            }
        }

        private static List<buildDetails> getBuilds(List<projectDetails> projects, inputs theInputs)
        {
            List<buildDetails> builds = new List<buildDetails>();

            // get the builds for the last 30 days
            string startDate = string.Format("{0:M-d-yyyy}", DateTime.Now.AddDays(-30));

            foreach (projectDetails p in projects)
            {
                string url = $"https://dev.azure.com/{theInputs.Org}/{p.name}/_apis/build/builds?minTime={startDate}&api-version=5.1";

                string response = RunGetAPI(theInputs.Pat, url);
                if (!string.IsNullOrEmpty(response))
                {
                    build ret = JsonConvert.DeserializeObject<build>(response);

                    // set the project name for all the builds and then append to our master list
                    builds.AddRange(ret.value.Select(c => { c.theproject = p.name; return c; }).ToList());                    
                }
            }

            return builds;
        }

        private static SortedDictionary<string, SortedDictionary<string, int>> getUsage(List<buildDetails> builds, string scope)
        {
            // { user1: {proj1/repo1/queue1: count, proj2/repo2/queue2: count, ...}}
            SortedDictionary<string, SortedDictionary<string, int>> usage = new SortedDictionary<string, SortedDictionary<string, int>>();
            
            // will use this as dictionary key to store all proj/repo/queues
            string masterList = "MasterList";

            foreach (buildDetails b in builds)
            {
                // update project
                if (scope == "all" || scope == "project")
                {
                    updateUsageList(b.requestedFor.uniqueName, $"p_{b.theproject}", usage);
                    updateUsageList(masterList, $"p_{b.theproject}", usage);
                }

                // update repository
                if (scope == "all" || scope == "repository")
                {
                    updateUsageList(b.requestedFor.uniqueName, $"r_{b.repository.name}", usage);
                    updateUsageList(masterList, $"r_{b.repository.name}", usage);
                }

                // update queue
                if (scope == "all" || scope == "queue")
                {
                    updateUsageList(b.requestedFor.uniqueName, $"q_{b.queue.name}", usage);
                    updateUsageList(masterList, $"q_{b.queue.name}", usage);
                }
            }

            // now add zeros for all the proj/repo/queue that each user didn't have
            // we only need to do this if there is more than one user, 1 user = 2 (user + masterlist)
            if (usage.Keys.Count > 2)
            {
                // foreach project/repo/queue in our master list
                foreach (string item in usage[masterList].Keys)
                {
                    // foreach user
                    foreach (string user in usage.Keys)
                    {
                        if (user != masterList) // to make sure we don't waste time
                        {
                            if (!usage[user].ContainsKey(item))
                            {
                                // add an empty entry for the proj/repo/queue the user didn't have
                                usage[user].Add(user, 0);
                            }
                        }
                    }
                }
            }
            
            return usage;
        }

        private static void incrementCount(SortedDictionary<string, int> userCount, string key)
        {
            if (userCount.ContainsKey(key))
            {
                // increment project/repo/queue since it already exists
               ++userCount[key];
            }
            else
            {
                // create new project/repo/queue count since it doesn't exist
                userCount.Add(key, 1);
            }
        }
        
        private static void updateUsageList(string key, string item,
            SortedDictionary<string, SortedDictionary<string, int>> usage)
        {
            if (usage.ContainsKey(key))
            {
                // user exists, so just update the count
                incrementCount(usage[key], item);
            }
            else
            {
                // initialize and add new item count to our usage list
                SortedDictionary<string, int> count = new SortedDictionary<string, int>()
                {
                    { item, 1 }
                };
                usage.Add(key, count);
            }
        }
        
        private static string createCSV(SortedDictionary<string, SortedDictionary<string, int>> usage)
        {
            string masterList = "MasterList";
            var csv = new StringBuilder();

            // write our column headers
            csv.Append("User," + string.Join(",", usage[masterList].Select(x => x.Key)));
            csv.Append(Environment.NewLine);

            // we no longer need the master list so we can discard
            usage.Remove(masterList);

            // write the count for each user
            foreach (string u in usage.Keys)
            {
                // add the unique ID
                csv.Append(u + ",");
                // add the totals
                csv.Append(string.Join(",", usage[u].Select(x => x.Value)));
                csv.Append(Environment.NewLine);
            }

            return csv.ToString();
        }

        private static bool uploadToOrg(inputs theInputs, string csv)
        {
            string filename = "buildUsageReport_" + String.Format("{0:MM-dd-yyyy}", DateTime.Now) + ".csv";
            string baseUrl = $"https://dev.azure.com/{theInputs.Org}/{theInputs.project}/_apis/wit/";
            string attachUrl = $"attachments?fileName={filename}&api-version=5.1";
            string linkUrl = $"workitems/{theInputs.witID}?api-version=5.1";

            // first we must upload the file to Azure DevOps
            string response = RunPostAPI(theInputs.Pat, baseUrl + attachUrl, csv);

            if (!string.IsNullOrEmpty(response))
            {
                // response is not null so the upload was successful
                uploadAttachmentResponse attach = JsonConvert.DeserializeObject<uploadAttachmentResponse>(response);

                // then we can link the work item to the uploaded file
                workItemLink link = new workItemLink();
                link.rel = "AttachedFile";
                link.url = attach.url;
                link.attributes = new attribute("Adding CSV report to work item");
                object[] patchDocument = new object[1];
                patchDocument[0] = new { op = "add", path = "/relations/-", value = link };
                bool success = RunPatchAPI(theInputs.Pat, baseUrl + linkUrl, patchDocument);

                // return true if we successfully linked the work item, false if not
                return success ? true : false;
            }
            else
            {
                // response was null so upload failed
                return false;
            }
        }

        private static string RunGetAPI(string pat, string url)
        {
            string _credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", pat)));

            using (var client = new HttpClient())
            {
                // generate the request headers
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _credentials);

                var method = new HttpMethod("GET");
                var request = new HttpRequestMessage(method, url) { };
                var response = client.SendAsync(request).Result;

                //if the response is successful, return the result
                if (response.IsSuccessStatusCode && !response.ReasonPhrase.Contains("Non-Authoritative Information"))
                {
                    var result = response.Content.ReadAsStringAsync().Result;
                    return result;
                }
                else
                {
                    return null;
                }
            }
        }

        private static string RunPostAPI(string pat, string url, string postData)
        {
            string _credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", pat)));

            using (var client = new HttpClient())
            {
                // generate the request headers
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _credentials);

                // Encode the csv file and send the request
                var postValue = new StringContent(postData, Encoding.UTF8, "application/octet-stream");
                var method = new HttpMethod("POST");
                var request = new HttpRequestMessage(method, url) { Content = postValue };
                var response = client.SendAsync(request).Result;

                //if the response is successful, return the result
                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsStringAsync().Result;
                    return result;
                }
                else
                {
                    return null;
                }
            }
        }

        private static bool RunPatchAPI(string pat, string url, object postData)
        {
            string _credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", pat)));

            using (var client = new HttpClient())
            {
                // generate the request headers
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json-patch+json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _credentials);

                // Encode the patch object and send the request
                var postValue = new StringContent(JsonConvert.SerializeObject(postData), Encoding.UTF8, "application/json-patch+json");
                var method = new HttpMethod("PATCH");
                var request = new HttpRequestMessage(method, url) { Content = postValue };
                var response = client.SendAsync(request).Result;

                //if the response is successful, return the true, false if not
                return response.IsSuccessStatusCode ? true : false;
            }
        }
    }
}
