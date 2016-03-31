using System;
using System.Collections;

namespace CommonContract
{
    public delegate void ConfigServerEventHandler(string objectName, string objectType, object updatedObject);

    public interface IConfigServer
    {

        event ConfigServerEventHandler ConfigUpdatedEvent;
        ICollection GetObjectProperties(string objectName, string objectType, bool subscribeForChanges = false);
        String GetSpecificObjectValue(string genesysObjectName, string genesysObjectType, string propertyToRetrieve, bool subscribeForChanges = false, int dbid = 0);
        ICollection GetAllObjectsForType(string objectType);
        object MessageServer { get; set; }
        void Shutdown();
    }
}
