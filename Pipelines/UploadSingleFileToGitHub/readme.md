# UploadSingleFileToGitHub

This PowerShell script will upload a single file into a repository hosted on GitHub.com, but can be easily modified to copy multiple files.

**Useful for:**
Saving backups to GitHub for added protection, such as a test results file

**Required Inputs:**
- userEmail - the email that will be associated with your Git commit
- userDisplayName - the display name that will show for the Git commit
- githubUsername - your username on GitHub, i.e. github.com/{username}
- repoName - the name of the repository on GitHub to upload to
- pathToFile - the file path for the item to be uploaded to GitHub
	- Example: $env:myLocalPath\my_file.txt

**To Use:**
1. Add a build variable for your GitHub personal access token (PAT)
2. Save the PowerShell script to your Azure DevOps repository after filling out the required inputs
3. In your pipeline, add a PowerShell script task
	a. Click the ellipses ("...") under Script Path to select the uploaded PowerShell script
	b. Expand the Environment Variables section and add a variable for your GitHub PAT
	c. Name the variable githubpat and set the value to $({your build variable name})
	d. If desired, you can set another Environment Variable for your local build path: set the value to $(Build.Repository.LocalPath)