using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace SoundSyncInterface
{
    [ServiceContract]
    public interface ISoundSyncContract
    {
        [OperationContract]
        void Play();
        [OperationContract]
        void Pause();
        [OperationContract]
        void Stop();
        [OperationContract]
        Boolean Display();
    }
}
