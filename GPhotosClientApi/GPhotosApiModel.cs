using System;
using System.Collections.Generic;

namespace GPhotosClientApi
{
    // method mediaItems.list
    // http request: GET https://photoslibrary.googleapis.com/v1/mediaItems
    // see https://developers.google.com/photos/library/reference/rest/v1/mediaItems/list
    public class MediaItemsResponseBody
    {
        public List<MediaItem> mediaItems { get; set; }
        public string nextPageToken { get; set; }
    }

    // method albums.list
    // http request: GET https://photoslibrary.googleapis.com/v1/albums
    // see https://developers.google.com/photos/library/reference/rest/v1/albums/list
    public class AlbumsResponseBody
    {
        public List<Album> albums { get; set; }
        public string nextPageToken { get; set; }
    }

    // method albums.create
    // http request: POST https://photoslibrary.googleapis.com/v1/albums
    // see https://developers.google.com/photos/library/reference/rest/v1/albums/create
    public class AlbumCreateRequestBody
    {
        public Album album { get; set; }
    }

    #region MediaItems

    public class MediaItem
    {
        public string id { get; set; }
        public string productUrl { get; set; }
        public string baseUrl { get; set; }
        public string mimeType { get; set; }
        public MediaMetadata mediaMetadata { get; set; }
        public string filename { get; set; }
    }

    public class MediaMetadata
    {
        public DateTime creationTime { get; set; }
        public string width { get; set; }
        public string height { get; set; }
        public Photo photo { get; set; }
    }

    public class Photo
    {
        public string cameraMake { get; set; }
        public string cameraModel { get; set; }
        public double focalLength { get; set; }
        public double apertureFNumber { get; set; }
        public int isoEquivalent { get; set; }
    }

    public class MediaItemsBatchCreate
    {
        public String albumId { get; set; }
        public List<NewMediaItem> newMediaItems { get; set; }

        public AlbumPosition albumPosition { get; set; }
    }

    public class MediaItemsBatchCreateResponse
    {
        public List<NewMediaItemResult> newMediaItemResults { get; set; }
    }

    public class NewMediaItem
    {
        public String description { get; set; }
        public SimpleMediaItem simpleMediaItem { get; set; }
    }

    public class NewMediaItemResult
    {
        public String uploadToken { get; set; }
        public Status status { get; set; }
        public MediaItem mediaItem { get; set; }
    }

    public class SimpleMediaItem
    {
        public string uploadToken { get; set; }
    }

    public class AlbumPosition
    {
        public PositionType position { get; set; }
        public String relativeMediaItemId { get; set; }

        public String relativeEnrichmentItemId { get; set; }
    }

    public enum PositionType { POSITION_TYPE_UNSPECIFIED, FIRST_IN_ALBUM, LAST_IN_ALBUM, AFTER_MEDIA_ITEM, AFTER_ENRICHMENT_ITEM }

    public class Status
    {
        public Decimal code { get; set; }
        public String message { get; set; }

        // TODO: test
        public List<KeyValuePair<String, Object>> details { get; set; }
    }


    #endregion

    #region Albums

    public class Album
    {
        public String id { get; set; }
        public String title { get; set; }
        public String productUrl { get; set; }
        public Boolean isWriteable { get; set; }

        public ShareInfo shareInfo { get; set; }

        public String mediaItemsCount { get; set; }

        public String coverPhotoBaseUrl { get; set; }

        public String coverPhotoMediaItemId { get; set; }
    }


    public class ShareInfo
    {
        public SharedAlbumOptions sharedAlbumOptions { get; set; }
        public String shareableUrl { get; set; }

        public String shareToken { get; set; }

        public Boolean isJoined { get; set; }
    }

    public class SharedAlbumOptions
    {
        public Boolean isCollaborative { get; set; }
        public Boolean isCommentable { get; set; }
    }

    #endregion
}
