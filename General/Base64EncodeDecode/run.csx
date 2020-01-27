#r "Newtonsoft.Json"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.Text;

public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    log.LogInformation("Starting Decode/Encode function");

    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    inputs theInputs = JsonConvert.DeserializeObject<inputs>(requestBody);
    
    if(theInputs.Encode)
    {
        string encoded = string.Empty;
        try
        {
            encoded = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", theInputs.Pat)));
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(ex.ToString());
        }
        return new OkObjectResult($"Encoded PAT: {encoded}");
    }
    else
    {
        string decoded = string.Empty;
        try
        {
            byte[] decodedBytes = Convert.FromBase64String(theInputs.Pat);
            decoded = System.Text.Encoding.UTF8.GetString(decodedBytes);
            return new OkObjectResult($"Decoded PAT: {decoded}");
        }
        catch (Exception ex)
        {
            return new BadRequestObjectResult(ex.ToString());
        }
    }
}

public class inputs
{
    public string Pat { get; set; } // personal access token for the Azure DevOps
    public bool Encode { get; set; } // If true, encodes the PAT; if false, decodes the PAT 
}