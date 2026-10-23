using System;
using System.Threading.Tasks;
using Google.Cloud.SecretManager.V1;

namespace BaseFramework.Technology.Util
{
    /// <summary>
    /// Utility class providing data integration methods to extract tokens and keys from Google Secret Manager.
    /// </summary>
    public class VaultCredentialUtility
    {
        /// <summary>
        /// Fetches the target secret version data payload from the cloud environment asynchronously.
        /// </summary>
        public async static Task<string> GetSecretAsync(string projectId, string secretName, string versionId = "latest")
        {
            SecretManagerServiceClient client = await SecretManagerServiceClient.CreateAsync();
            SecretVersionName secretVersionName;
            
            try
            {
                string envOverride = Environment.GetEnvironmentVariable($"ConnectionStrings__{secretName}") ?? "";
                if (!string.IsNullOrEmpty(envOverride))
                {
                    return envOverride;
                }

                client = await SecretManagerServiceClient.CreateAsync();
                secretVersionName = new SecretVersionName(projectId, secretName, "latest");
                AccessSecretVersionResponse result = await client.AccessSecretVersionAsync(secretVersionName);
                return result.Payload.Data.ToStringUtf8();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to resolve secret '{secretName}' from Google Secret Manager infrastructure: {ex.Message}");
            }
        }
    }
}
