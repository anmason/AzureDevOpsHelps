# UploadSingleFileToGitHub

This script will upload a single file into a repository hosted on GitHub.com, but can be easily modified to copy multiple files. I've provided both a PowerShell script for Windows agents and a Bash script for Linux agents.

**Useful for:**
Saving backups to GitHub for added protection, such as a test results file

**Required Inputs:**
- userEmail - the email that will be associated with your Git commit
- userDisplayName - the display name that will show for the Git commit
- githubUsername - your username on GitHub, i.e. github.com/{username}
- repoName - the name of the repository on GitHub to upload to
- pathToFile - the file path for the item to be uploaded to GitHub
	- Examples:
		- $env:myLocalPath\my_file.txt (Windows)
		- ${SYSTEM_DEFAULTWORKINGDIRECTORY}/my_file.txt (Linux)

**To Use:**
1. Add a build variable for your GitHub personal access token (PAT)
2. Save the script to your Azure DevOps repository after filling out the required inputs
3. In your pipeline, add either a PowerShell script or Bash script task, depending on which version you're using
	1. Click the ellipses ("...") under Script Path to select the uploaded script
	2. Expand the Environment Variables section and add a variable for your GitHub PAT
		1. Name the variable githubpat and set the value to $({your build variable name})
