
# BuildsTriggeredPerUser

This Azure function will generate a build usage report over the last 30 days. It will show the number of builds triggered per user per project, repository, queue, or all three. The function uploads the csv report to an existing work item in an Azure DevOps organization.

**Useful for:**
- Seeing how many builds were run per agent queue
- Seeing which users are the most active in submitting builds
- Seeing which repositories are the most actively developed

**Required Inputs:**
- Pat - Personal access token for the Azure DevOps organization, this is pulled from App Settings
- Org - The Azure DevOps organization to build the report for
- scope - Determines what you want the report summarized by: either "Project", "Repository", "Queue", or "All"
- witID - The ID of the existing work item to upload the finished report to
- project - The project where the above work item (witID) exists

**Additional Notes:**
- This is written in C#, targeting .NET Core 2.1, so be sure to select **Azure Functions 2.x** for the function runtime when creating your function app
- For more info on creating Azure Functions in Visual Studio, see [here](https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-your-first-function-visual-studio)
- This is written for an HTTP trigger, but you could change it to a Timer trigger and have it run automatically once a month
- The names of each project, repository, and queue will be prefaced with one of the following to differentiate items with the same name:
	- For projects: p_
	- For repositories: r_
	- For queues: q_
- The resulting csv will be titled buildUsageReport_MM-dd-yyyy.csv
- The more builds you've run and the more projects you have, the longer it will take to generate the csv file
- Here is an example csv file:

|User| r_MyFirstRepo | r_MySecondRepo | p_MyFirstProject | q_MyAgentPool|
|--|--|--|--|--|
|me@myemail.com| 18 | 6 | 24 | 24 |
|you@youremail.com | 11 | 4 | 15 | 15 |
