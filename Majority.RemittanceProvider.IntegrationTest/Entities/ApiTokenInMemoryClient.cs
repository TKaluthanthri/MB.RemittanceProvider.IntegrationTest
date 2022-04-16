using IdentityModel.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Majority.RemittanceProvider.IntegrationTest.Entities
{
    public class ApiTokenInMemoryClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _identityServerUrl;

        private class AccessTokenItem
        {
            public string AccessToken { get; set; } = string.Empty;
            public DateTime ExpiresIn { get; set; }
        }
        public ApiTokenInMemoryClient(
            string serverUrl,
            HttpClient httpClient)
        {
            _httpClient = httpClient;
            _identityServerUrl = serverUrl;
        }


        private ConcurrentDictionary<string, AccessTokenItem> _accessTokens = new ConcurrentDictionary<string, AccessTokenItem>();

        public async Task<string> GetApiToken(string client_id, string client_secret, string scope, string grant_type)
        {
            if (_accessTokens.ContainsKey(client_id))
            {
                var accessToken = _accessTokens.GetValueOrDefault(client_id);
                if (accessToken.ExpiresIn > DateTime.UtcNow)
                {
                    return accessToken.AccessToken;
                }
                else
                {
                    // remove
                    _accessTokens.TryRemove(client_id, out AccessTokenItem accessTokenItem);
                }
            }

            var newAccessToken = await getApiToken(client_id, client_secret, scope, grant_type);
            return newAccessToken.AccessToken;
        }

        private async Task<AccessTokenItem> getApiToken(string client_id, string client_secret, string scope, string grant_type)
        {
            try
            {
                var disco = await HttpClientDiscoveryExtensions.GetDiscoveryDocumentAsync(
                    _httpClient,
                    _identityServerUrl);

                if (disco.IsError)
                {
                    throw new ApplicationException($"Status code: {disco.IsError}, Error: {disco.Error}");
                }

                var tokenResponse = await HttpClientTokenRequestExtensions.RequestClientCredentialsTokenAsync(_httpClient, new ClientCredentialsTokenRequest
                {
                    Scope = scope,
                    ClientSecret = client_secret,
                    Address = disco.TokenEndpoint,
                    ClientId = client_id
                });

                if (tokenResponse.IsError)
                {
                    throw new ApplicationException($"Status code: {tokenResponse.IsError}, Error: {tokenResponse.Error}");
                }

                return new AccessTokenItem
                {
                   
                    ExpiresIn = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn),
                    AccessToken = tokenResponse.AccessToken
                };

            }
            catch (Exception e)
            {
                throw new ApplicationException($"Exception {e}");
            }
        }

    }
}
