using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace GPhotosClientApi
{
    public class GPhotosClient : IDisposable
    {
        private HttpClient client;
        public GPhotosClient(String credentialsFile, String tokenStoreDir, String userName)
        {
            // init HttpClient, with oauth2 handler
            var authHandler = new AuthenticationHandler(credentialsFile, tokenStoreDir, userName);
            client = new HttpClient(authHandler);

        }
        public void Dispose()
        {
            client?.Dispose();
        }

        #region mediaItems

        public async Task<MediaItemsResponseBody> GetMediaItems(String pageToken)
        {
            String baseUri = "https://photoslibrary.googleapis.com/v1/mediaItems";
            if (!string.IsNullOrEmpty(pageToken))
                baseUri += "?pageToken=" + pageToken;

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(baseUri),
                Headers = { }
            };

            var response = await client.SendAsync(httpRequestMessage);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<MediaItemsResponseBody>();
            }

            throw ToException(response);
        }

        public async Task<MediaItemsResponseBody> GetAlbumMediaItems(String albumId, int pageSize = 100, String pageToken = null)
        {
            String baseUri = "https://photoslibrary.googleapis.com/v1/mediaItems:search";
            baseUri += "?albumId=" + albumId;
            baseUri += @"&pageSize=" + pageSize;
            if (!string.IsNullOrEmpty(pageToken))
                baseUri += @"&pageToken=" + pageToken;

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(baseUri),
                Headers = { }
            };

            var response = await client.SendAsync(httpRequestMessage);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<MediaItemsResponseBody>();
            }

            throw ToException(response);
        }

        public async Task<List<MediaItem>> GetAllAlbumMediaItems(String albumId)
        {
            var result = new List<MediaItem>();

            var mediaItemsResponse = await GetAlbumMediaItems(albumId, 100);
            result.AddRange(mediaItemsResponse.mediaItems);
            while (mediaItemsResponse.nextPageToken != null)
            {
                mediaItemsResponse = await GetAlbumMediaItems(mediaItemsResponse.nextPageToken);
                result.AddRange(mediaItemsResponse.mediaItems);
            }
            return result;
        }


        // return upload token
        public async Task<String> UploadMedia(String fileFullName)
        {
            String baseUri = "https://photoslibrary.googleapis.com/v1/uploads";

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(baseUri),
                Headers = {
                    { "X-Goog-Upload-File-Name" , Path.GetFileName(fileFullName)},
                    { "X-Goog-Upload-Protocol"  , "raw" }},
                Content = new ByteArrayContent(File.ReadAllBytes(fileFullName))
            };

            var response = await client.SendAsync(httpRequestMessage);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }

            throw ToException(response);
        }

        /// <summary>
        /// This call adds the media item to the library.
        /// If an album id is specified, the call adds the media item to the album too.
        /// Each album can contain up to 20,000 media items. 
        /// By default, the media item will be added to the end of the library or album.
        /// </summary>
        /// <remarks>
        /// If an album id and position are both defined, the media item is added to the album at the specified position.
        /// 
        /// If the call contains multiple media items, they're added at the specified position. 
        /// If you are creating a media item in a shared album where you are not the owner, you are not allowed to position the media item. 
        /// Doing so will result in a BAD REQUEST error.
        /// </remarks>
        /// <param name="uploadToken">Token identifying the media bytes that have been uploaded to Google.</param>
        /// <param name="albumId">identifier of the album where the media items are added. The media items are also added to the user's library. This is an optional field.</param>
        /// <returns></returns>
        public async Task<MediaItemsBatchCreateResponse> BatchCreate(String uploadToken, String albumId = null, String description = null)
        {
            String baseUri = "https://photoslibrary.googleapis.com/v1/mediaItems:batchCreate";

            // body
            var requestBody = new MediaItemsBatchCreate
            {
                albumPosition = new AlbumPosition
                {
                    position = PositionType.FIRST_IN_ALBUM
                },
                newMediaItems = new List<NewMediaItem>() {
                    new NewMediaItem() {
                        description = description,
                        simpleMediaItem = new SimpleMediaItem() { uploadToken = uploadToken }
                    }
                },
                albumId = albumId
            };

            // request  
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(baseUri),
                Headers = { },

                Content = new StringContent(JsonConvert.SerializeObject(requestBody))
            };

            var response = await client.SendAsync(httpRequestMessage);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsAsync<MediaItemsBatchCreateResponse>();
                return result;
            }

            throw ToException(response);
        }

        #endregion

        #region albums

        public async Task<Album> CreateAlbum(String albumName)
        {
            String baseUri = "https://photoslibrary.googleapis.com/v1/albums";

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(baseUri),
                Headers = { },
                Content = new StringContent(JsonConvert.SerializeObject(new AlbumCreateRequestBody() { album = new Album { title = albumName } }))
            };

            var response = await client.SendAsync(httpRequestMessage);
            if (response.IsSuccessStatusCode)
            {
                var album = await response.Content.ReadAsAsync<Album>();
                Console.WriteLine(string.Format("album created: {0}", album.title));

                return album;
            }

            throw ToException(response);
        }
        public async Task<ShareInfo> ShareAlbum(String albumId)
        {
            String baseUri = "https://photoslibrary.googleapis.com/v1/albums/";
            baseUri += albumId + ":share";
 
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(baseUri),
                Headers = { },
                Content = new StringContent(JsonConvert.SerializeObject(new SharedAlbumOptions { isCollaborative = true, isCommentable = true }))
            };

            var response = await client.SendAsync(httpRequestMessage);
            if (response.IsSuccessStatusCode)
            {
                var shareInfo = await response.Content.ReadAsAsync<ShareInfo>();
                Console.WriteLine(string.Format("album shared: {0}", shareInfo.shareableUrl));
                
                return shareInfo;
            }

            throw ToException(response);
        }

        public async Task<AlbumsResponseBody> GetAlbumsPage(string pageToken = null)
        {
            var baseUri = "https://photoslibrary.googleapis.com/v1/albums";
            // TODO: extarct and rework with query params
            if (!string.IsNullOrEmpty(pageToken))
            {
                if (baseUri.Contains("?"))
                    baseUri += "&pageToken=" + pageToken;
                else
                    baseUri += "?pageToken=" + pageToken;
            }

            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(baseUri),
            };

            var response = await client.SendAsync(httpRequestMessage);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsAsync<AlbumsResponseBody>();
            }

            throw ToException(response);
        }

        public async Task<List<Album>> GetAllAlbums()
        {
            var result = new List<Album>();

            var albumPage = await GetAlbumsPage();
            result.AddRange(albumPage.albums);
            while (albumPage.nextPageToken != null)
            {
                albumPage = await GetAlbumsPage(albumPage.nextPageToken);
                result.AddRange(albumPage.albums);
            }
            return result;
        }

        #endregion


        private Exception ToException(HttpResponseMessage response)
        {
            return new Exception(string.Format("code={0}, reason={1}", response.StatusCode, response.ReasonPhrase));
        }
    }

}
