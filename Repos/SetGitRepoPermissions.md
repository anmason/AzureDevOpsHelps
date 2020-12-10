# Programmatically Set Group Permissions for a Git Repository in Azure DevOps Services
Setting permissions for Azure DevOps programmatically can be a bit of a mystery, especially if you're new to doing it. Before getting started, I highly recommend checking out the [new documentation](https://docs.microsoft.com/en-us/azure/devops/organizations/security/namespace-reference?view=azure-devops) that gives excellent guidance on permission namespaces. 

This guide walks you through setting the permissions for a group on a Git repository. This guide assumes you've installed and are familiar with Azure CLI, so if you haven't, head over [here](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) and [here](https://docs.microsoft.com/en-us/azure/devops/cli/index?view=azure-devops) first.

## Steps to Set the Permissions
1. Get the group’s descriptor
    1. This starts with "vssgp." for Azure DevOps groups and "aadgp." for Azure AD groups
    2. If you do not have this value already, you can query the groups using this command: ```az devops security group list```
        1. You can also filter it down by project, scope, and type (either vssgp or aadgp)
        2. Reference: https://docs.microsoft.com/en-us/cli/azure/ext/azure-devops/devops/security/group?view=azure-cli-latest#ext-azure-devops-az-devops-security-group-list
    3. The descriptor will look something like this: *vssgp.LongEncryptedValueForTheGroupDescriptor*
2. Determine which permissions to grant allows for
    1. Each permission is assigned a bit (1, 2, 4, 8, etc. which translates to 0001, 0010, 0100, 1000, etc. in binary)
    2. You will need to sum the decimal values for each permission you want to add
        1. So if you wanted permissions 2 and 4, you would use the value 6, which corresponds to 0110 in binary
    3. To see which bits correspond to which permissions, use this command: ```az devops security permission namespace show --id 2e9eb7ed-3c0a-47d4-87c1-0ffdd275fd87 --org https://dev.azure.com/{yourOrg}```
        1. The ID in the above command, "2e9eb7ed-3c0a-47d4-87c1-0ffdd275fd87", is the security namespace for Git repo permissions
        2. Reference: https://docs.microsoft.com/en-us/cli/azure/ext/azure-devops/devops/security/permission/namespace?view=azure-cli-latest#ext-azure-devops-az-devops-security-permission-namespace-show
3. Do the same process as Step 2 for each of the explicit denies you want to set
    1. Add up the decimal values for each permission you want to set a deny for
    2. The "Not Set" permission acts as an implicit deny, so that any user will have a deny unless another group membership elsewhere grants them an allow
        1. So depending on your needs, you may not need to worry about setting explicit denies
4. Next, you will need to get the ID for the project containing the repo
    1. You can get the project ID using this command: ```az devops project show --project <name of project>```
    2. Reference: https://docs.microsoft.com/en-us/cli/azure/ext/azure-devops/devops/project?view=azure-cli-latest#ext-azure-devops-az-devops-project-show
5. Next you need to get the repository ID for the repo you’re adding permissions to
    1. You can get the ID using this command: ```az repos show --repository {repo name} --project {projectName} --org https://dev.azure.com/{yourOrg}```
    2. Reference: https://docs.microsoft.com/en-us/cli/azure/ext/azure-devops/repos?view=azure-cli-latest#ext-azure-devops-az-repos-show
6. Now that we have all those details, we can actually set the permission
    1. We will be using this command: ```az devops security permission update --id 2e9eb7ed-3c0a-47d4-87c1-0ffdd275fd87 --subject {group descriptor from step1} --token repoV2/{project ID from step 4}/{repo ID from step 5} --allow-bit {total value from step 2} --deny-bit {total value from step 3} --org https://dev.azure.com/{your org}```
    2. Example command and response:
    3. ![Example call via PowerShell](https://github.com/anmason/AzureDevOpsHelps/blob/master/_Images/Repos/az_cli_group_permissions_example.png)
    4. Then when we look in our repository settings from the browser:
    5. ![Example view from Azure DevOps after running command](https://github.com/anmason/AzureDevOpsHelps/blob/master/_Images/Repos/az_cli_group_permissions_devops_view.jpg)
