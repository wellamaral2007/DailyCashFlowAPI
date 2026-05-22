using System;
using Google.Cloud.SecretManager.V1;

public class VaultCredentialUtility
{
    
    
    public async static Task<string> GetSecretAsync(string _projectId, string secretName, string versionId = "latest")
    {
        // Cria o cliente do Secret Manager
        SecretManagerServiceClient client = await SecretManagerServiceClient.CreateAsync();

        // Constrói o nome do recurso do segredo no GCP
        SecretVersionName secretVersionName = new SecretVersionName(_projectId, secretName, versionId);

        try
        {
            // Busca o segredo e converte para string
            AccessSecretVersionResponse response = await client.AccessSecretVersionAsync(secretVersionName);
            return response.Payload.Data.ToStringUtf8();
        }
        catch (Exception ex)
        {
            throw new Exception($"Err to find secret {secretName} in GCP: {ex.Message}");
        }
    }
}