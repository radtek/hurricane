using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.ServiceModel.Web;
using System.ServiceModel.Channels;
using System.IO;

namespace GSeries.DataDistributionService {
    /// <summary>
    /// A service that provides access to files in the Hurricane distributed 
    /// system as if they are hosted on a web server locally.
    /// </summary>
    [ServiceContract]
    public interface IFileService {
        [OperationContract]
        [WebGet(UriTemplate = "/PathStatus/{path}", ResponseFormat = WebMessageFormat.Json)]
        PathStatusDto GetPathStatus(string path);

        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        byte[] Read(string path, long offset, int count);

        [OperationContract(Name = "RestRead")]
        [WebGet(UriTemplate = "/File/{path}/{offset}/{count}", ResponseFormat = WebMessageFormat.Json)]
        byte[] Read(string path, string offset, string count);

        [OperationContract]
        [WebGet(UriTemplate = "/Echo/{message}", ResponseFormat = WebMessageFormat.Json)]
        string Echo(string message);

        [OperationContract]
        [FaultContract(typeof(string))]
        void Error();
    }
}
