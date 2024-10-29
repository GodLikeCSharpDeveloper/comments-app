using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace CommentApp.Common.Services.SecretService
{
    public class SecretService()
    {
        public static async Task<string> GetSecret()
        {
            string secretName = "commentsbucketkeys";
            string region = "us-east-1";

            IAmazonSecretsManager client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

            GetSecretValueRequest request = new()
            {
                SecretId = secretName,
                VersionStage = "AWSCURRENT",
            };

            GetSecretValueResponse response;
            try
            {
                response = await client.GetSecretValueAsync(request);
            }
            catch
            {
                throw;
            }
            return response.SecretString;
        }
    }
}
