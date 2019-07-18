#! /bin/bash
# INPUT YOUR VALUES HERE:
userEmail="<your email>"
userDisplayName="<your name>"
githubUsername="<your GitHub username>"
repoName="<your repo name>"
pathToFile="<path to the file you're uploading to GitHub>"
#
# Do not modify below this line --------------------------------------------
cloneURL="https://${githubUsername}:${githubpat}@github.com/${githubUsername}/${repoName}.git"
#
# check to see if the repo exists
git ls-remote $cloneURL -q
REPOFOUND="$(echo $?)"
if [ "$REPOFOUND" -eq "0" ]; then
    # Repository exists so clone the repo;
    cd /home/vsts;
    git config --global user.email $userEmail;
    git config --global user.name $userDisplayName;
    git clone $cloneURL;
    #;
    # copy the updated file to the cloned repo;
    echo Copying file from $pathToFile to /home/vsts/${repoName}
    cp -rf $pathToFile /home/vsts/${repoName};
    #;
    #push the file to the remote repo on GitHub;
    echo pushing the file
    cd $repoName;
    git add .;
    git commit -m "push from Azure DevOps (`date`)";
    git push;
else
    # Repository does not exist, inform user;
    echo Could not find repository.;
    echo If you have created a repository already, please check the provided URL.;
    echo If you have not created a repository on GitHub yet, please do so.;
fi
