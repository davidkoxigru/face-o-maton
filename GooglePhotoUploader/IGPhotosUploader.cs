using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GooglePhotoUploader
{
    public interface IGPhotosUploader
    {
        String UploadDirectory { get; }

        Task<PhotoPath> Upload(PhotoPath photo);
    }
}
