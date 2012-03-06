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
    [ServiceContract]
    public interface IFileService {
        [OperationContract]
        [FaultContract(typeof(DataDistributionServiceException))]
        [WebGet(ResponseFormat = WebMessageFormat.Json)]
        PathStatusDto GetPathStatus(string path);

        [OperationContract]
        [FaultContract(typeof(DataDistributionServiceException))]
        [WebGet(ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        byte[] Read(string path, long offset, int count);

        [OperationContract]
        [WebGet(UriTemplate = "/Echo/{message}",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.WrappedRequest)]
        [FaultContract(typeof(DataDistributionServiceException))]
        string Echo(string message);
    }
}
