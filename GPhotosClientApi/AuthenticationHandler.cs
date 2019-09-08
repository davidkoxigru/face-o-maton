using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;
using Google.Apis.Util.Store;

namespace GPhotosClientApi
{
    class AuthenticationHandler : DelegatingHandler
    {
        private readonly String _credentialsFilename;
        private readonly String _tokenStoreDir;
        private readonly String _userName;
        private readonly string[] _scopes = {
            "https://www.googleapis.com/auth/photoslibrary",
            //"https://www.googleapis.com/auth/photoslibrary.sharing",
            //"https://www.googleapis.com/auth/photoslibrary.readonly",
            //"https://www.googleapis.com/auth/photoslibrary.readonly.appcreateddata",
            //"https://www.googleapis.com/auth/photoslibrary.appendonly"
         };

        private UserCredential _credentials;

        public AuthenticationHandler(String credentialsFilename, String tokenStoreDir, String userName)
        {
            _credentialsFilename = credentialsFilename;
            _tokenStoreDir = tokenStoreDir;
            _userName = userName;

            InnerHandler = new HttpClientHandler();

        }

        private int _maxRefreshAttempts = 3;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // First renew acces token if needed
            await CheckAccesToken();

            // set authorization token to original request
            request.Headers.Add(HttpRequestHeader.Authorization.ToString(), _credentials.Token.TokenType + " " + _credentials.Token.AccessToken);

            // Finaly make the call
            var response = await base.SendAsync(request, cancellationToken);

            // in case of unauthorized, try to refresh token once again
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                for (int i = 1; i == _maxRefreshAttempts; i++)
                {
                    if (_credentials.RefreshTokenAsync(CancellationToken.None).Result)
                    {
                        request.Headers.Add(HttpRequestHeader.Authorization.ToString(), _credentials.Token.TokenType + " " + _credentials.Token.AccessToken);
                        response = await base.SendAsync(request, cancellationToken);
                    }
                }
            }

            return response;
        }

        /// <summary>
        /// Retrive Google OAuth user credentials, using credentials.json
        /// Token is stored in filesystem for further useh
        /// <returns></returns>
        private async Task GetCredentials()
        {
            if (!File.Exists(_credentialsFilename))
                throw new Exception(String.Format("Google credentials file = '{0}' doesn't exists", _credentialsFilename));

            var json = File.ReadAllText(_credentialsFilename);

            GoogleClientSecrets clientSecrets;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                clientSecrets = GoogleClientSecrets.Load(stream);
            }

            var dataStore = new FileDataStore(_tokenStoreDir, true);


            //using (var stream = new FileStream(_credentialsFilename, FileMode.Open, FileAccess.Read))
            //{
            var credentials = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets?.Secrets,
                    //GoogleClientSecrets.Load(stream).Secrets,
                    _scopes,
                    _userName,
                    CancellationToken.None,
                    dataStore);

                _credentials = credentials;
            //}
        }

        /// <summary>
        /// Check token is not valid
        /// </summary>
        /// <returns></returns>
        private async Task CheckAccesToken()
        {
            if (_credentials == null)
            {
                Console.WriteLine("Request Google API access token");
                await GetCredentials();
            }

            if (_credentials.Token.IsExpired(SystemClock.Default))
            {
                Console.WriteLine("token has expired: try to refresh it");
                await _credentials.RefreshTokenAsync(CancellationToken.None);
            }
        }
    }
}
