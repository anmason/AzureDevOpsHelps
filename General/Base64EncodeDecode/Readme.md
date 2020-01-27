# Base64 Encode and Decode
This example shows how to create an HTTP Trigger Azure Function that will encode an Azure DevOps personal access token (PAT) into a base64 string. It will also decode the base64 string into the original value you encoded.

This can be useful for when you want to quickly encode a PAT but don't have access to Visual Studio, or whichever IDE you prefer.

## Steps
1. If you don't already have one, create a Function App in Azure 
	1. ![Create a Function App](https://github.com/anmason/AzureDevOpsHelps/blob/master/_Images/General/functionApp_PatEncoder.PNG)
2. Once your Function is created, follow the steps [here](https://docs.microsoft.com/en-us/azure/azure-functions/functions-twitter-email#create-an-http-triggered-function) to create an HTTP triggered function in-portal
	1. When you get to step 3, replace the contents of the run.csx file with the code found [here](https://github.com/anmason/AzureDevOpsHelps/blob/master/General/Base64EncodeDecode/run.csx)
3. To use the function, you would need to call the API using the POST method and provide two values in the request body:
	{
    "Pat": "myPAT",
    "Encode" : "True"
	}
