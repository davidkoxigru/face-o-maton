using GPhotosClientApi;
using log4net;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Collections.Generic;

namespace GooglePhotoUploader
{
    public class GPhotosUploader : IGPhotosUploader
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public String CredentialsFile { get; private set; }
        public String TokenStoreFolder { get; private set; }

        public String UploadDirectory { get; private set; }

        public String AlbumName { get; private set; }

        public String AlbumId { get; private set; }

        public String UserName { get; private set; }

        public GPhotosClient Client { get; private set; }

        private System.Timers.Timer _syncTimer;

        public GPhotosUploader(String credentialsFile, String tokenStoreFolder, String albumName, string userName, String uploadDir, int syncIntervalSeconds = 900)
        {
            CredentialsFile = credentialsFile;
            TokenStoreFolder = tokenStoreFolder;
            UserName = UserName;
            AlbumName = albumName;
            UploadDirectory = uploadDir;

            // init GPhotos client
            log.Info("Init GPhotos client");
            Client = new GPhotosClient(
               @credentialsFile,
               @tokenStoreFolder,
               userName);

            // mirroring sync
            // idle time
            _syncTimer = new System.Timers.Timer();
            _syncTimer.Interval = syncIntervalSeconds * 1000;
            _syncTimer.Elapsed += Sync;
            _syncTimer.Enabled = true;



            //// create dir if not exists
            //if (!Directory.Exists(dir))
            //{
            //    log.Info(string.Format("create upload directory: '{0}'", dir));
            //    Directory.CreateDirectory(dir);
            //}

            //// Create a new FileSystemWatcher and set its properties.
            //using (FileSystemWatcher watcher = new FileSystemWatcher())
            //{
            //    watcher.Path = dir;

            //    // Watch for changes in LastAccess and LastWrite times, and
            //    // the renaming of files or directories.
            //    watcher.NotifyFilter = NotifyFilters.LastAccess
            //                         | NotifyFilters.LastWrite
            //                         | NotifyFilters.FileName
            //                         | NotifyFilters.DirectoryName;

            //    // Only watch text files.
            //    watcher.Filter = "*.*";

            //    // Add event handlers.
            //    watcher.Created += OnFileCreated;
            //    //watcher.Changed += OnChanged;
            //    //watcher.Deleted += OnChanged;
            //    //watcher.Renamed += OnRenamed;

            //    // Begin watching.
            //    watcher.EnableRaisingEvents = true;
            //}
        }

        #region sync

        private bool _syncLock = false;

        private async void Sync(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_syncLock)
            {
                log.Warn("sync process is locked: skip it");
                return;
            }

            try
            {
                _syncLock = true;

                // list local files
                var localFiles = ListLocalPhotos();
                log.Info(String.Format("found {0} local files", localFiles.Count));

                // list google files
                var remoteFiles = await ListRemotePhotos();
                log.Info(String.Format("found {0} remote files", remoteFiles.Count));

                // find local files that need to be uploaded
                var remoteFilesHash = new HashSet<String>(remoteFiles);
                var candidates = localFiles.Where(x => !remoteFiles.Contains(x)).ToList();
                log.Info(String.Format("find {0} candidates to sync upload:\n{1}", candidates.Count, String.Join("\n\t-", candidates.ToArray())));

                if (candidates.Count > 0)
                {
                    // upload 
                    candidates.ForEach(c => Upload(new PhotoPath(c)).Wait());

                    log.Info("sync succeeded");
                }
                else
                {
                    log.Info("nothing to sync");
                }
            }
            catch (Exception ex)
            {
                log.Error("failed to sync google photos", ex);
            }
            finally
            {
                _syncLock = false;
            }

        }



        private List<string> ListLocalPhotos()
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg" };
            var files = Directory
                 .GetFiles(UploadDirectory)
                 .Select(file => Path.GetFileName(file).ToLower())
                 .Where(file => allowedExtensions.Any(file.EndsWith))
                 .ToList();
            return files;
        }

        private async Task<List<String>> ListRemotePhotos()
        {
            // check album
            if (AlbumId == null)
                await CheckAlbum();

            var items = await Client.GetAllAlbumMediaItems(AlbumId);
            return items.Select(x => x.filename.ToLower()).ToList();
        }

        #endregion




        public async Task CheckAlbum()
        {
            // check album exists
            log.Info(String.Format("Check album '{0}' exists", AlbumName));

            var albums = await Client.GetAllAlbums();
            log.Info("albums = " + String.Join(", ", albums.Select(a => a.title)));
            AlbumId = albums.FirstOrDefault(a => a.title == AlbumName)?.id;

            if (AlbumId == null)
            {
                log.Info(String.Format("Create album '{0}'", AlbumName));
                var createdAlbum = await Client?.CreateAlbum(AlbumName);
                log.Info(String.Format("Album successfully created"));
                AlbumId = createdAlbum.id;
            }
        }


        public async Task<PhotoPath> Upload(PhotoPath photo)
        {
            log.Info(String.Format("GPhotos upload: '{0}'", photo.fileName));
            try
            {
                // check album
                if (AlbumId == null)
                    await CheckAlbum();

                // upload photon then add to 
                var fileFullName = Path.Combine(UploadDirectory, photo.fileName);
                var uploadToken = await Client.UploadMedia(fileFullName);
                log.Info("uploadToken = " + uploadToken);
                if (!String.IsNullOrEmpty(uploadToken))
                {
                    var result = await Client.BatchCreate(uploadToken, AlbumId, photo.fileName);
                    result.newMediaItemResults.ForEach(r => Console.WriteLine(string.Format("mediaItem='{0}' added to album='{1}'", r.mediaItem.filename, AlbumId)));

                    photo.url = result.newMediaItemResults[0].mediaItem.baseUrl;
                }
            }
            catch (Exception ex)
            {
                log.Error(String.Format("failed to upload photo '{0}' to Google Photos", photo.fileName), ex);
            }

            return photo;
        }

    }
    public class PhotoPath
    {

        public String fileName { get; set; }
        public String url { get; set; }

        public PhotoPath(String name)
        {
            fileName = name;
        }

    }
}
