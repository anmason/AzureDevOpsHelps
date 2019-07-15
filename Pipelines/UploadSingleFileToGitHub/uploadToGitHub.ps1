# INPUT YOUR VALUES HERE:
$userEmail = "<your email>"
$userDisplayName = "<your name>"
$githubUsername = "<your GitHub username>"
$repoName = "<your repo name>"
$pathToFile = "<path to the file you're uploading to GitHub>"

# Do not modify below this line ---------------------------------------------------------------
$cloneURL = "https://${githubUsername}:$env:githubpat@github.com/$githubUsername/$repoName.git"
$date = Get-Date

# check to see if the repo exists first
# - git ls-remote will return "Repository not found" if the repo does not exist
$REPOFOUND = git ls-remote $cloneURL | findstr "Repository not found"
IF ([string]::IsNullOrEmpty($REPOFOUND)) {
	# repo does exist, so clone the repo
	cd D:\
	git config --global user.email $userEmail
	git config --global user.name $userDisplayName
	git clone $cloneURL

    # copy the updated file to the cloned repo
	copy $pathToFile D:\$repoName

    # push the file to the remote repo on GitHub
	cd D:\$repoName
	git add .
	git commit -m "push from Azure DevOps ($date)"
	git push
}
ELSE {
	# Repository does not exist, inform user
	Write-Host "Could not find repository."
    Write-Host "If you have created a repository already, please check the provided URL."
    Write-Host "If you have not created a repository on GitHub yet, please do so."
}