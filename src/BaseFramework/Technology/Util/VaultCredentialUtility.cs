using System;
using Google.Cloud.SecretManager.V1;

namespace BaseFramework.Technology.Util
{
    public class VaultCredentialUtility
    {

        public async static Task<string> GetSecretAsync(string projectId, string secretName, string versionId = "latest")
        {
            // Cria o cliente do Secret Manager
            SecretManagerServiceClient client = await SecretManagerServiceClient.CreateAsync();

            // Constrói o nome do recurso do segredo no GCP
            SecretVersionName secretVersionName;
            try
            {
                string envOverride = Environment.GetEnvironmentVariable($"ConnectionStrings__{secretName}") ?? "";
                if (!string.IsNullOrEmpty(envOverride))
                {
                    return envOverride;
                }

                // Fluxo normal de produção quando estiver rodando no Google Cloud Functions
                client = await SecretManagerServiceClient.CreateAsync();
                secretVersionName = new SecretVersionName(projectId, secretName, "latest");
                AccessSecretVersionResponse result = await client.AccessSecretVersionAsync(secretVersionName);
                return result.Payload.Data.ToStringUtf8();
            }
            catch (Exception ex)
            {
                throw new Exception($"Err to find secret {secretName} in GCP: {ex.Message}");
            }
        }
    }
}