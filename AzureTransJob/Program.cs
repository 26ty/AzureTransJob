using Azure.Storage.Blobs;
using Azure;
using AzureTransJob.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NLog.Web;
using System.Data;
using System.Text;
using uty60;
using System.Net;
using Azure.Storage.Blobs.Models;
using System.Net.NetworkInformation;
using AzureTransDoc;
using System.Text.Json;
using Azure.Core;
using Microsoft.AspNetCore.JsonPatch.Operations;
using NLog.Fluent;
using Oracle.ManagedDataAccess.Client;
using System.Net.Sockets;
using Microsoft.AspNetCore.Routing;

namespace AzureTransJob
{
    internal class Program
    {
        private static readonly string endpoint = "https://innodrive.cognitiveservices.azure.com/translator/text/batch/v1.1";
        // private static readonly string key = "5c2847d594a94bab8487a4de1d7bfcc9";
        private static readonly string key = "dc2e5c5a2f6941e5952035ce146d6210";
        static readonly string sourceURL = "https://innodrivedev.blob.core.windows.net/source?sp=rl&st=2023-12-20T07:25:01Z&se=2099-12-31T15:25:01Z&spr=https&sv=2022-11-02&sr=c&sig=%2BXSZ7G0rqsExCSWT04S3hV%2F5efzquP%2BZwqnh8eINx9g%3D";
        static readonly string targetURL = "https://innodrivedev.blob.core.windows.net/target?sp=rwl&st=2023-12-20T07:27:52Z&se=2099-12-31T15:27:52Z&spr=https&sv=2022-11-02&sr=c&sig=jn%2BfPCyofMEfG6SSLuC99lHv0W4%2Fk5FOCidgoyOxTjc%3D";
        static readonly string connectionString = "DefaultEndpointsProtocol=https;AccountName=innodrivedev;AccountKey=AUrQTTArXgnC3p/TnC0or0W+iKiwB27KazZ474iQmarp1xn85lPBCrCY1DSzDIbUnAP8KOZaDIr4+AStyu1ssQ==;EndpointSuffix=core.windows.net";
        static readonly string sourceContainer = "source";
        static readonly string destinationContainer = "target";
        //private static readonly string baseUrl = "http://localhost:5238/api";
        //private static readonly string baseUrl = "http://pinnodrvapitn.cminl.oa/api";
        private static readonly string baseUrl = "http://10.55.21.226:5110/api";
        public static IConfiguration config;

        static async Task Main(string[] args)
        {
            config = new ConfigurationBuilder()
                   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                   .Build();
            var log = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            log.Info("init");

            // 呼叫取得狀態為Pending的job
            /*List<AzureTransJobs> azureTransJobs = await CheckJobStatusAsync(config);
            while (azureTransJobs.Count > 0)
            {
                foreach (AzureTransJobs item in azureTransJobs)
                {
                    Console.WriteLine("取得Pending狀態的Job");
                    Console.WriteLine($"AJB_ID: {item.AJB_ID}");
                    Console.WriteLine($"FILE_ID: {item.FILE_ID}");
                    Console.WriteLine($"AZ_JOB_ID: {item.AZ_JOB_ID}");//--------->jobId
                    Console.WriteLine($"TARGET_LANGCODE: {item.TARGET_LANGCODE}");
                    Console.WriteLine($"AJB_JOB_STARTTIME: {item.JOB_STARTTIME}");
                    Console.WriteLine($"JOBSTATUS: {item.JOBSTATUS}");
                    Console.WriteLine($"JOBMESSAGE: {item.JOBMESSAGE}");
                    Console.WriteLine("==================================================================================");

                    //查詢pending中的job目前狀態
                    await GetJobStatus(endpoint, key, item.AZ_JOB_ID.ToString() , item.AJB_ID.ToString());
                }

                // 等待一段時間再重新取得Pending狀態的Job
                //await Task.Delay(TimeSpan.FromSeconds(10));
                //azureTransJobs = await CheckJobStatusAsync(config);
            }*/

            /* 列出狀態為Pending的所有job */
            List<AzureTransJobs> azureTransJobs = await CheckJobStatusAsync(config);
            foreach (AzureTransJobs item in azureTransJobs)
            {
                Console.WriteLine("取得Pending狀態的Job");
                Console.WriteLine($"AJB_ID: {item.AJB_ID}");
                Console.WriteLine($"FILE_ID: {item.FILE_ID}");
                Console.WriteLine($"AZ_JOB_ID: {item.AZ_JOB_ID}");//--------->jobId
                Console.WriteLine($"TARGET_LANGCODE: {item.TARGET_LANGCODE}");
                Console.WriteLine($"AJB_JOB_STARTTIME: {item.JOB_STARTTIME}");
                Console.WriteLine($"JOBSTATUS: {item.JOBSTATUS}");
                Console.WriteLine($"JOBMESSAGE: {item.JOBMESSAGE}");

                //查詢pending中的job目前狀態
                await GetJobStatus(endpoint, key, item.AZ_JOB_ID.ToString(), item.AJB_ID.ToString());
            }

            /* 將jobId寫入DB 、更改job狀態*/
            //AzureTransJobs azureTrans = new AzureTransJobs();
            //azureTrans.JOBSTATUS = "Success";

            //main api
            //await AzureTransDoc(@"D:\新人訓檔案\MIS\Mars\Azure translation doc\日_翻譯測試文件5.txt", "en", @"http://localhost:5238/api/ClsAzure/Callback");
        
            //ClsAzureTransDoc ATD = new ClsAzureTransDoc();
            //await ATD.AzureTransDoc(@"D:\新人訓檔案\MIS\Mars\Azure translation doc\翻譯測試文件1.txt", "en", @"http://localhost:5238/api/ClsAzure/Callback");
        }

        

        //翻譯狀態，會傳回文件翻譯要求的狀態。 此狀態包括整體要求狀態，以及要求中要翻譯的文件狀態。
        static string status = "";
        static int total = 0;
        static int failed = 0;
        static int success = 0;
        static int inProgress = 0;
        static int notYetStarted = 0;
        static int cancelled = 0;
        static int totalCharacterCharged = 0;

        /// <summary>
        /// 取得翻譯工作狀態，監控job status
        /// </summary>
        /// <param name="endpoint">翻譯工具端點</param>
        /// <param name="key"></param>
        /// <param name="jobId"></param>
        /// <param name="callBackAPIUrl"></param>
        /// <param name="FromFullPath"></param>
        /// <returns></returns>
        public static async Task GetJobStatus(string endpoint, string key, string jobId , string ajbId)
        {
            string route = $"/batches/{jobId}";
            // 宣告http物件
            HttpClient client = new HttpClient();
            using HttpRequestMessage request = new HttpRequestMessage();
            {
                request.Method = HttpMethod.Get;
                request.RequestUri = new Uri(endpoint + route);
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);

                HttpResponseMessage response = await client.SendAsync(request);
                string result = response.Content.ReadAsStringAsync().Result;
                Console.WriteLine("==================================GetJobStatus Result================================");
                Console.WriteLine($"Job  result: {result}");
                Console.WriteLine();

                // 解析 JSON 字串成對應的物件
                dynamic jsonResult = JsonConvert.DeserializeObject(result);

                // 排除錯誤情況
                if (jsonResult.error == null)
                {
                    // 取得 jobId
                    jobId = jsonResult.id;
                    // 取得 summary 欄位
                    dynamic summary = jsonResult.summary;
                    // 取得 status 欄位
                    status = jsonResult.status;

                    // 取得 summary 中的特定欄位值
                    total = summary.total;
                    failed = summary.failed;
                    success = summary.success;
                    inProgress = summary.inProgress;
                    notYetStarted = summary.notYetStarted;
                    cancelled = summary.cancelled;
                    totalCharacterCharged = summary.totalCharacterCharged;

                    Console.WriteLine("====================================取得翻譯工作狀態==================================");
                    Console.WriteLine(
                    $"jobId: {jobId}\n" +
                    $"status: {status}\n" +
                    $"Total: {total}\n" +
                    $"Failed: {failed}\n" +
                    $"Success: {success}\n" +
                    $"InProgress: {inProgress}\n" +
                    $"NotYetStarted: {notYetStarted}\n" +
                    $"Cancelled: {cancelled}\n" +
                    $"TotalCharacterCharged: {totalCharacterCharged}\n");
                }
                
                // 建立要傳遞的資料物件
                AzureTransJobs jobData = new AzureTransJobs();

                /* 若翻譯完成，狀態為success，呼叫AzureTransCompletedCallback(AJB_ID) 歸檔，若未完成則跳下⼀筆。CallBack會做到更新DB狀態 */
                if (total == success && success != 0)
                {
                    Console.WriteLine("");
                    Console.WriteLine("==============翻譯完畢，呼叫AzureTransCompletedCallback(AJB_ID) 歸檔================");
                    /* 用AJB_ID呼叫 callbackurl */
                    await CallAzureTransCompletedCallback(ajbId);
                    Console.WriteLine("==================================結束一次監控工作===================================");
                    /* 進行下載 (先去除)*/
                    //await BlobListAndDownload(FromFullPath, callBackAPIUrl,jobId);
                }
                else if (total == failed && failed != 0)
                {
                    Console.WriteLine("====================================翻譯錯誤!=========================================");
                    await CallAzureTransCompletedCallback(ajbId);
                }
                else if (status == "ValidationFailed")
                {
                    Console.WriteLine("====================================翻譯錯誤!=========================================");
                    await CallAzureTransCompletedCallback(ajbId);
                }
                else if (jsonResult.error != null)
                {
                    Console.WriteLine("====================================翻譯錯誤!=========================================");
                    await CallAzureTransCompletedCallback(ajbId);
                }
                else
                {
                    Console.WriteLine("===============================未翻譯完成，可監控Job狀態===============================");
                    //Console.WriteLine();
                    //await Task.Delay(TimeSpan.FromSeconds(20)); // 等待20秒
                }

                // 重新定義工作完成/錯誤

                /*if (status != "Succeeded")
                {
                    jobData.AJB_JOBSTATUS = status;

                    // 將job寫入DB
                    await UpdateJobStatus(jobId, jobData);
                }
                else
                {
                    jobData.AJB_JOBSTATUS = status;

                    // 更新DB status
                    await UpdateJobStatus(jobId, jobData);
                }*/

                //Console.WriteLine($"jobData: {jobData}");



            }
        }

        /// <summary>
        /// 從DB取得狀態為Pending的job
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        static async Task<List<AzureTransJobs>> CheckJobStatusAsync(IConfiguration config)
        {
            using (ClsDBLib db = ClsData.getDBLib(false, config))
            {
                // 取得狀態為Pending的job
                string sql = $@"
                select A.AJB_ID,A.FILE_ID,A.AZ_JOB_ID,A.TARGET_LANGCODE,A.JOB_STARTTIME,A.JOBSTATUS,A.JOBMESSAGE
                from DOC_AZURETRANSJOB A
                WHERE A.JOBSTATUS='Pending'";
                DataTable dt = await db.getDataTable(sql);

                List<AzureTransJobs> jobList = new List<AzureTransJobs>();

                foreach (DataRow dr in dt.Rows)
                {
                    AzureTransJobs job = new AzureTransJobs
                    {
                        AJB_ID = (int)dr["AJB_ID"], // 要拿來呼叫calbackurl
                        FILE_ID = new Guid(dr["FILE_ID"].ToString()),
                        AZ_JOB_ID = new Guid(dr["AZ_JOB_ID"].ToString()),
                        TARGET_LANGCODE = dr["TARGET_LANGCODE"].ToString(),
                        JOB_STARTTIME = DateTime.Parse(dr["JOB_STARTTIME"].ToString()),
                        JOBSTATUS = dr["JOBSTATUS"].ToString(),
                        JOBMESSAGE = dr["JOBMESSAGE"].ToString()
                    };

                    jobList.Add(job);
                    //string jobID = dr["AJB_JOB_ID"].ToString_Mars();
                    //string jobCallBackUrl = dr["AJB_CALLBACKURL"].ToString_Mars();
                    //Console.WriteLine($"jobID: {jobID}");
                    //Console.WriteLine($"jobCallBackUrl: {jobCallBackUrl}");
                    //Console.WriteLine();

                }
                return jobList;
            }
        }


        /// <summary>
        /// 取得ApiTicket
        /// </summary>
        /// <returns></returns>
        public static async Task<string> GetApiTicket()
        {
            string route = "http://tinnodrvapattachtn.cminl.oa/api/Interface/GetAPTicket";
            string postData = "{\"Account\":\"API_AZURE\",\"Password\":\"API_AZURE\"}";
            var responseTicket="";

            HttpClient client = new HttpClient();
            try
            {
                using HttpRequestMessage request = new HttpRequestMessage();
                {
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(route);

                    // 將資料物件序列化成 JSON 字串
                    //var jsonData = JsonConvert.SerializeObject(new { AJB_ID = ajbId });
                    request.Content = new StringContent(postData, Encoding.UTF8, "application/json");

                    HttpResponseMessage result = await client.SendAsync(request);
                    
                    Console.WriteLine($"GetApiTicket  result: {result}");
                    if (result.IsSuccessStatusCode)
                    {
                        responseTicket = await result.Content.ReadAsStringAsync();
                        // 移除引號
                        responseTicket = responseTicket.Trim('"');
                        Console.WriteLine($"GetApiTicket result: {responseTicket}");
                    }
                    else
                    {
                        Console.WriteLine($"GetApiTicket request failed with status code: {result.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions
                Console.WriteLine("Error: " + ex.Message);
            }

            return responseTicket;
        }

        /// <summary>
        /// 呼叫翻譯完成的 CallbackURL http://pinnodrvapitn.cminl.oa/api/AzureTranslate/AzureTransCompletedCallback，需要ticket?
        /// </summary>
        /// <param name="ajbId"></param>
        /// <returns></returns>
        static async Task<HttpResponseMessage> CallAzureTransCompletedCallback(string ajbId)
        {
            //先取得APITicket
            var APITicket = await GetApiTicket();
            Console.WriteLine("APITicket: " + APITicket);

            string route = "/AzureTranslate/AzureTransCompletedCallback";
            HttpClient client = new HttpClient();
            try
            {
                using HttpRequestMessage request = new HttpRequestMessage();
                {
                    request.Method = HttpMethod.Post;
                    request.RequestUri = new Uri(baseUrl + route);

                    // 將資料物件序列化成 JSON 字串
                    var jsonData = JsonConvert.SerializeObject(new { AJB_ID = ajbId });
                    // 指定body
                    request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                    // 指定header ticket 
                    request.Headers.Add("ticket", APITicket);

                    HttpResponseMessage result = await client.SendAsync(request);
                    Console.WriteLine($"CallAzureTransCompletedCallback  result: {result}");
                    return result;
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"發生錯誤: {ex.Message}");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
            
        }

        


        /*-----------------------------------------------------------------------------列出/下載檔案(之後要刪除)--------------------------------------------------------------*/
        /// <summary>
        /// 更新job status
        /// </summary>
        /// <param name="ajb_job_id"></param>
        /// <param name="azureTransJob"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        static async Task<HttpResponseMessage> UpdateJobStatus(string az_job_id, [FromBody] AzureTransJobs azureTransJob, IConfiguration config)
        {
            using (ClsDBLib db = ClsData.getDBLib(false, config))
            {
                //先查詢id是否存在
                string sql = $@"
                    SELECT A.AZ_JOB_ID
                    FROM DOC_AZURETRANSJOB A
                    WHERE AZ_JOB_ID = '{az_job_id}'
                    ";
                DataTable dt = await db.getDataTable(sql);

                if (dt.Rows.Count == 0)
                {
                    Console.WriteLine($"Resource with AZ_JOB_ID {az_job_id} not found.");
                    // return NotFound(new { Message = $"Resource with AJB_JOB_ID {ajb_job_id} not found." });
                }

                //如果id存在
                sql = $@"
                    UPDATE DOC_AZURETRANSJOB
                    SET JOBSTATUS = '{azureTransJob.JOBSTATUS}'
                    WHERE AZ_JOB_ID = '{az_job_id}'
                    ";
                //job.id = Guid.Parse(id);
                db.ExecNonQuery(sql);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }

        // 列出容器中區塊 Blob 
        public static async Task<List<string>> ListBlobsFlatListing(BlobContainerClient blobContainerClient, int? segmentSize)
        {
            List<string> blobNames = new List<string>();
            // 特定的Blob區塊
            //var blobClien = new BlobClient(connectionString, "container00001", "翻譯測試文件3.txt");
            try
            {
                // Call the listing operation and return pages of the specified size.
                var resultSegment = blobContainerClient.GetBlobsAsync()
                    .AsPages(default, segmentSize);

                // Enumerate the blobs returned for each page.
                await foreach (Page<BlobItem> blobPage in resultSegment)
                {
                    foreach (BlobItem blobItem in blobPage.Values)
                    {
                        //Console.WriteLine("List Blob name: {0}", blobItem.Name);
                        //await DownloadBlobToFileAsync(blobClien, @"C:\Users\Niansin.chen\Documents\a1\", blobItem.Name);
                        blobNames.Add(blobItem.Name);
                    }

                    Console.WriteLine();
                }
                return blobNames;
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        // 下載 Blob
        public static async Task DownloadBlobToFileAsync(BlobClient blobClient, BlobClient sourceBlobClient, string FromFullPath, string fileName, string callBackAPIUrl, string jobId)
        {
            //副檔名
            //string fileExtension = Path.GetExtension(fileName);
            //檔名
            //string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            //新檔名
            //string newFileName = $"{fileNameWithoutExtension}_{language}{fileExtension}";
            //string newFileName = Path.GetFileName(FromFullPath);

            //var finalFilePath = Path.Combine(localFilePath, fileName);
            string onlyPath = Path.GetDirectoryName(FromFullPath);
            //var finalFilePath = Path.Combine(onlyPath, newFileName);
            Console.WriteLine("欲下載的檔案路徑(翻譯後的檔案完整路徑 fullpath) : " + FromFullPath);
            Console.WriteLine();
            try
            {
                await blobClient.DownloadToAsync(FromFullPath).ConfigureAwait(false);

                if (await blobClient.ExistsAsync())
                {
                    Console.WriteLine("已下載!呼叫CallBackUrl");
                    Console.WriteLine();
                    //callBackAPIUrl
                    var callBackAPIUrlBody = new AzureTransJobs
                    {
                        AJB_JOB_ID = new Guid(jobId.ToString()),
                    };
                    //CallbackResponse callBackAPIUrlResult = await CallBackAPIUrlRequest(callBackAPIUrl, callBackAPIUrlBody);
                    //CallbackResponse callBackAPIUrlResult = await Callback(jobId, config);
                    Console.WriteLine("==================================呼叫CallBackAPIUrl================================");
                    //Console.WriteLine($"callBackAPIUrlResult - Status: {callBackAPIUrlResult.Status}");
                    //Console.WriteLine($"callBackAPIUrlResult - FullPath: {callBackAPIUrlResult.FullPath}");
                    //Console.WriteLine($"callBackAPIUrlResult - Message: {callBackAPIUrlResult.Message}");
                    //已下載，就刪除來源、目的容器的blob
                    await DeleteBlobSnapshotsAsync(blobClient);
                    await DeleteBlobSnapshotsAsync(sourceBlobClient);
                }
                else
                {
                    Console.WriteLine("下載出現錯誤!");
                }
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine($"Error: {e.Message}");
                //Console.WriteLine("Access to the path is denied. Please check file permissions or choose a different destination path.");
                // 其他處理措施，例如顯示錯誤訊息、記錄錯誤、選擇其他路徑等
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine();
                throw;
            }

        }
        // 列出後進行下載
        public static async Task BlobListAndDownload(string FromFullPath, string callBackAPIUrl, string jobId)
        {
            // 宣告目標容器物件
            var blobContainerClient = new BlobContainerClient(connectionString, destinationContainer);
            // 列出目標容器中所有Blob區塊檔案
            List<string> blobNames = await ListBlobsFlatListing(blobContainerClient, 10);

            if (blobNames != null && blobNames.Count > 0)
            {
                foreach (string name in blobNames)
                {
                    Console.WriteLine($"目前在容器中的區塊檔案名稱:  {name}");
                    Console.WriteLine();
                    // 目的容器的Blob區塊
                    var blobClien = new BlobClient(connectionString, destinationContainer, name);
                    // 來源容器的Blob區塊
                    var sourceBlobClient = new BlobClient(connectionString, sourceContainer, name);
                    //下載容器內所有檔案到指定路徑
                    await DownloadBlobToFileAsync(blobClien, sourceBlobClient, FromFullPath, name, callBackAPIUrl, jobId);
                }
            }
            else
            {
                Console.WriteLine("無文件可供下載!");
            }
        }

        // 刪除blob
        public static async Task DeleteBlobSnapshotsAsync(BlobClient blob)
        {
            // Delete a blob and all of its snapshots
            await blob.DeleteAsync(snapshotsOption: DeleteSnapshotsOption.IncludeSnapshots);

            // Delete only the blob's snapshots
            //await blob.DeleteAsync(snapshotsOption: DeleteSnapshotsOption.OnlySnapshots);
        }

        /*  (未使用)callBackUrl ---> 串接API (未使用)*/
        static async Task<JobResponse> CallBackUrlRequest([FromBody] CallbackResponse callbackResponse)
        {
            string route = "/ClsAzure/Callback";
            HttpClient client = new HttpClient();

            // 建立要傳遞的資料物件
            var callbackData = new
            {
                callbackResponse.Status,
                callbackResponse.Message,
                callbackResponse.FullPath,
            };

            using HttpRequestMessage request = new HttpRequestMessage();
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(baseUrl + route);

                // 將資料物件序列化成 JSON 字串
                var jsonData = JsonConvert.SerializeObject(callbackData);
                request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                HttpResponseMessage result = await client.SendAsync(request);
                // string result = response.Content.ReadAsStringAsync().Result;

                // 根據需要處理回應結果
                // Console.WriteLine($"Result: {result}");
                JobResponse response = new JobResponse
                {
                    StatusCode = (int)result.StatusCode,
                    RequestMessage = result.RequestMessage.ToString()
                };
                Console.WriteLine(response.StatusCode);
                Console.WriteLine(response.RequestMessage);
                return response;
            }
        }

        /*  (未使用)callBackUrl ---> 串接API (未使用)*/
        static async Task<CallbackResponse> CallBackAPIUrlRequest(string router, [FromBody] AzureTransJobs azureTransJobs)
        {
            HttpClient client = new HttpClient();
            // 建立要傳遞的資料物件
            var callbackBody = new
            {
                azureTransJobs.AJB_JOB_ID
            };

            using HttpRequestMessage request = new HttpRequestMessage();
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(router);

                // 將資料物件序列化成 JSON 字串
                var jsonData = JsonConvert.SerializeObject(callbackBody);
                request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.SendAsync(request);
                string result = response.Content.ReadAsStringAsync().Result;

                // 解析 JSON 字串成對應的物件
                CallbackResponse callbackResponse = JsonConvert.DeserializeObject<CallbackResponse>(result);
                return callbackResponse;
            }
        }
    }
}