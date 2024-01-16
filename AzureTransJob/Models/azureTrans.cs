using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureTransJob.Models
{
    internal class azureTrans
    {
    }

    public class AzureTransJobs
    {
        public int AJB_ID { get; set; }
        public Guid FILE_ID { get; set; }
        public Guid AZ_JOB_ID { get; set; }
        public string? TARGET_LANGCODE {get; set; }
        public DateTime JOB_STARTTIME { get; set; }
        public string? JOBSTATUS { get; set; }
        public string? JOBMESSAGE { get; set; }


        public Guid AJB_JOB_ID { get; set; }
        public DateTime AJB_JOB_STARTTIME { get; set; }
        public string? AJB_CALLBACKURL { get; set; }
        public string? AJB_DEST_FULLPATH { get; set; }
        public string? AJB_JOBSTATUS { get; set; }
        public string? AJB_JOBMESSAGE { get; set; }
    }

    public class JobResponse
    {
        public int StatusCode { get; set; }
        public string ?RequestMessage { get; set; }

    }

    public class CallbackResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public string FullPath { get; set; }
    }
}
