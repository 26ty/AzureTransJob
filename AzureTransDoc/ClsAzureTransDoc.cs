using Azure;
using Azure.Storage.Blobs;
using AzureTransDoc.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using AzureTransDoc.ClsDB;
using NLog.Web;

namespace AzureTransDoc
{
    public class ClsAzureTransDoc
    {
        private static readonly string endpoint = "https://innodrive.cognitiveservices.azure.com//translator/text/batch/v1.1";
        private static readonly string key = "dc2e5c5a2f6941e5952035ce146d6210";
        static readonly string sourceURL = "https://tranalator.blob.core.windows.net/source?sp=rl&st=2023-12-27T07:51:13Z&se=2024-01-02T15:51:13Z&skoid=012aad91-a6c9-4123-8f80-56d7f863efbc&sktid=5af0aac6-3ea7-4c0b-a4f8-9475b62d9c5f&skt=2023-12-27T07:51:13Z&ske=2024-01-02T15:51:13Z&sks=b&skv=2022-11-02&spr=https&sv=2022-11-02&sr=c&sig=SPiRVxD5bJ6MhUhDLTgq3us6vkZwGegZP5XnLmnudkA%3D";
        static readonly string targetURL = "https://tranalator.blob.core.windows.net/destination?sp=rwl&st=2023-12-27T07:51:58Z&se=2024-01-02T15:51:58Z&skoid=012aad91-a6c9-4123-8f80-56d7f863efbc&sktid=5af0aac6-3ea7-4c0b-a4f8-9475b62d9c5f&skt=2023-12-27T07:51:58Z&ske=2024-01-02T15:51:58Z&sks=b&skv=2022-11-02&spr=https&sv=2022-11-02&sr=c&sig=hKypDOpazSJjF50kfRCp4oixUD8%2Bflh%2FhUo7vav2fcA%3D";
        static readonly string connectionString = "DefaultEndpointsProtocol=https;AccountName=tranalator;AccountKey=vhNhiyaKcLyYwYzXQNte+YN6MGcOd1RJZvdyAdjFr0r6FA+dkwvboavHAni/WosQ1hErOsJDxxOS+AStfjhq7Q==;EndpointSuffix=core.windows.net";
        static readonly string sourceContainer = "source";
        static readonly string destinationContainer = "destination";
        private static readonly string baseUrl = "http://localhost:5238/api";
        public static IConfiguration config;

        /// <summary>
        /// 上傳指定的 File，並呼叫 Azure 執⾏翻譯，將翻譯工作寫⼊ DB Table 
        /// </summary>
        /// <param name="FromFullPath">翻譯文件路徑</param>
        /// <param name="ToLangCode">翻譯語系代號</param>
        /// <param name="callBackAPIUrl"></param>
        /// <returns></returns>
        public async Task AzureTransDoc(string FromFullPath, string ToLangCode, string callBackAPIUrl)
        {
            Console.WriteLine($"callBackAPIUrl: {callBackAPIUrl}");
            Console.WriteLine($"FromFullPath: {FromFullPath}");
            // 宣告來源容器物件
            var blobContainerClient = new BlobContainerClient(connectionString, sourceContainer);
            /* 呼叫上傳Blob區塊 */
            await UploadFromFileAsync(blobContainerClient, FromFullPath, ToLangCode, callBackAPIUrl);
        }

        // 

        /// <summary>
        /// 接收指定檔案路徑上傳blob區塊
        /// </summary>
        /// <param name="containerClient">來源容器物件</param>
        /// <param name="FromFullPath"></param>
        /// <param name="ToLangCode"></param>
        /// <param name="callBackAPIUrl"></param>
        /// <returns></returns>
        public static async Task UploadFromFileAsync(BlobContainerClient containerClient, string FromFullPath, string ToLangCode, string callBackAPIUrl)
        {
            Console.WriteLine($"完整檔案路徑: {FromFullPath}");
            string fileName = Path.GetFileName(FromFullPath);
            Console.WriteLine($"欲上傳的檔案名稱: {fileName}");

            // 將檔案名稱宣告於來源容器物件
            BlobClient blobClient = containerClient.GetBlobClient(fileName);

            // 取得檔案純路徑
            string onlyPath = Path.GetDirectoryName(FromFullPath);

            Console.WriteLine($"檔案純路徑: {onlyPath}");
            Console.WriteLine();

            try
            {
                // 將檔案上傳至來源容器
                await blobClient.UploadAsync(FromFullPath, true);
                // 確認上傳檔案成功處
                if (await blobClient.ExistsAsync())
                {
                    Console.WriteLine("檔案上傳成功!");
                    Console.WriteLine("==================================================================================");
                    Console.WriteLine();
                    /* 執行翻譯文件工作 */
                    await DocTranslation(sourceURL, targetURL, endpoint, key, FromFullPath, ToLangCode, callBackAPIUrl);
                }
                else
                {
                    //錯誤處理
                    Console.WriteLine("檔案上傳失敗!");
                    Console.WriteLine("==================================================================================");
                    Console.WriteLine();
                }
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine("blobClient", e.Message);
                Console.WriteLine();
                throw;
            }

        }

        // 宣告全域變數
        static string jobId = "";
        /// <summary>
        /// 翻譯容器內所有檔案
        /// </summary>
        /// <param name="sourceURL"></param>
        /// <param name="targetURL"></param>
        /// <param name="endpoint"></param>
        /// <param name="key"></param>
        /// <param name="FromFullPath"></param>
        /// <param name="ToLangCode"></param>
        /// <param name="callBackAPIUrl"></param>
        /// <returns></returns>
        public static async Task DocTranslation(string sourceURL, string targetURL, string endpoint, string key, string FromFullPath, string ToLangCode, string callBackAPIUrl)
        {
            Console.WriteLine("請求翻譯中...");
            Console.WriteLine("==================================================================================");
            Console.WriteLine();
            string route = "/batches";
            string json = "{\"inputs\": [{\"source\": {\"sourceUrl\": \"" + sourceURL + "\", \"storageSource\": \"AzureBlob\"}, \"targets\": [{\"targetUrl\": \"" + targetURL + "\", \"storageSource\": \"AzureBlob\", \"category\": \"general\", \"language\": \"" + ToLangCode + "\"}]}]}";

            //使用HttpClient類別建立http用戶端物件，用於向API發送請求
            using HttpClient client = new HttpClient();

            //使用HttpRequestMessage類別建立http請求物件，用於定義發送給API的請求
            using HttpRequestMessage request = new HttpRequestMessage();
            {
                //建立StringContent物件，將json字串(包含sourceURL、targetURL)轉為http請求的內容
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                //設定了 HTTP 請求的方法為 POST，表示要向 API 發送一個 POST 請求
                request.Method = HttpMethod.Post;

                //設定了 HTTP 請求的 URI，組合了 endpoint 和 route 的值
                request.RequestUri = new Uri(endpoint + route);

                //在請求標頭中添加了一個名為 "Ocp-Apim-Subscription-Key" 的標頭欄位，其值為 key
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);

                //設定了 HTTP 請求的內容為之前建立的 content 物件，即包含要發送的 JSON 格式參數。
                request.Content = content;

                //使用 HTTP 客戶端發送了異步的 HTTP 請求，並將 API 的回應儲存在 response 物件中。
                HttpResponseMessage response = await client.SendAsync(request);

                //從回應的內容中讀取回應的字串內容，並將其儲存在 result 變數中。
                string result = response.Content.ReadAsStringAsync().Result;

                //檢查回應的狀態代碼是否表示成功。
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("成功送出翻譯請求!");
                    Console.WriteLine("");
                    Console.WriteLine($"請求翻譯狀態碼 Status code: {response.StatusCode}");
                    Console.WriteLine($"請求翻譯回應 Response Headers:{response.Headers}");
                    Console.WriteLine("==================================================================================");
                    // 取得 Operation-Location 標頭的值
                    string operationLocation = response.Headers.GetValues("Operation-Location").FirstOrDefault();
                    if (!string.IsNullOrEmpty(operationLocation))
                    {
                        // 擷取最後的 id 部分
                        string id = operationLocation.Split('/').Last();
                        // 將jobid賦值給全域變數
                        jobId = id;

                        Console.WriteLine($"Job ID: {jobId}");

                        // 建立要傳遞的資料物件
                        string fileName = System.IO.Path.GetFileName(FromFullPath);
                        //副檔名
                        string fileExtension = Path.GetExtension(fileName);
                        //檔名
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                        //新檔名
                        string newFileName = $"{fileNameWithoutExtension}_{ToLangCode}{fileExtension}";

                        //var finalFilePath = Path.Combine(localFilePath, fileName);
                        string onlyPath = Path.GetDirectoryName(FromFullPath);
                        var finalFilePath = Path.Combine(onlyPath, newFileName);

                        /* 取得翻譯工作狀態 */
                        await GetJobStatus(endpoint, key, id, callBackAPIUrl, FromFullPath);

                        AzureTransJobs jobData = new AzureTransJobs();
                        if (status == "ValidationFailed" || failed != 0)
                        {
                            jobData.AJB_JOBSTATUS = "Failed";
                        }
                        else
                        {
                            jobData.AJB_JOBSTATUS = "Pending";
                        }

                        jobData.AJB_JOB_ID = Guid.Parse(jobId.ToString());
                        jobData.AJB_CALLBACKURL = callBackAPIUrl;
                        jobData.AJB_DEST_FULLPATH = finalFilePath;
                        jobData.AJB_JOBMESSAGE = "AzureTransjob message";
                        Console.WriteLine("==================================資料庫新增一筆Job================================");
                        /* 將job寫入DB ---> 還是Pending狀態 */
                        // await CreateJob(jobData);
                        await CreateJob(jobData, config);

                    }
                }
                else
                {
                    Console.WriteLine("翻譯請求錯誤...");
                    Console.Write("Error");
                    Console.WriteLine($"Error Status code: {response.StatusCode}");
                    Console.WriteLine();
                    Console.WriteLine($"Error Response Headers:");
                    Console.WriteLine(response.Headers);
                }
            }

        }

        //翻譯工作狀態
        static string status = "";
        static int total = 0;
        static int failed = 0;
        static int success = 0;
        static int inProgress = 0;
        static int notYetStarted = 0;
        static int cancelled = 0;
        static int totalCharacterCharged = 0;

        /// <summary>
        /// 取得翻譯狀態，會傳回文件翻譯要求的狀態。 此狀態包括整體要求狀態，以及要求中要翻譯的文件狀態。
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="key"></param>
        /// <param name="jobId"></param>
        /// <param name="callBackAPIUrl"></param>
        /// <param name="FromFullPath"></param>
        /// <returns></returns>
        public static async Task GetJobStatus(string endpoint, string key, string jobId, string callBackAPIUrl, string FromFullPath)
        {
            string route = $"/batches/{jobId}";

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

                // 取得 jobId
                jobId = jsonResult.id;
                // 取得 summary 欄位
                dynamic summary = jsonResult.summary;
                status = jsonResult.status;

                // 取得 summary 中的特定欄位值
                total = summary.total;
                failed = summary.failed;
                success = summary.success;
                inProgress = summary.inProgress;
                notYetStarted = summary.notYetStarted;
                cancelled = summary.cancelled;
                totalCharacterCharged = summary.totalCharacterCharged;


                Console.WriteLine("====================================翻譯工作狀態==================================");
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
                Console.WriteLine("==================================更新Job資料庫狀態================================");
                // 建立要傳遞的資料物件
                AzureTransJobs jobData = new AzureTransJobs();
                // 翻譯狀態為success 才更改Job DB狀態
                if (total == success && success != 0)
                {
                    jobData.AJB_JOBSTATUS = status;
                    // 更新DB status
                    //await TUpdateJobStatus(jobId, jobData, config);

                    Console.WriteLine("翻譯完畢，可進行下載");
                    Console.WriteLine("==================================================================================");

                    // 進行下載
                    //await BlobListAndDownload(FromFullPath, ToLangCode_G, callBackAPIUrl, jobId);
                }
                else if (total == failed && failed != 0)
                {
                    Console.WriteLine("翻譯錯誤!");
                    Console.WriteLine("==================================================================================");
                }
                else if (status == "ValidationFailed")
                {
                    Console.WriteLine("翻譯錯誤!");
                    Console.WriteLine("==================================================================================");
                }
                else
                {
                    Console.WriteLine("未翻譯完成，可監控Job狀態");
                    Console.WriteLine("==================================================================================");
                }

            }
        }

        /// <summary>
        /// 將jobId寫入DB ---> 直接連接SQL
        /// </summary>
        /// <param name="azureTransJob"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        static async Task<HttpResponseMessage> CreateJob([FromBody] AzureTransJobs azureTransJob, IConfiguration config)
        {
            config = new ConfigurationBuilder()
                   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                   .Build();
            var log = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            log.Info("init");
            using (ClsDBLib db = ClsData.getDBLib(false, config))
            {
                //int ajbId = Convert.ToInt32(data["AJB_ID"]);
                string ajbJobId = azureTransJob.AJB_JOB_ID.ToString();
                DateTime ajbJobStartTime = DateTime.Now;
                string ajbCallbackUrl = azureTransJob.AJB_CALLBACKURL.ToString();
                string ajbDestFullPath = azureTransJob.AJB_DEST_FULLPATH.ToString();
                string ajbJobStatus = azureTransJob.AJB_JOBSTATUS.ToString();
                string ajbJobMessage = azureTransJob.AJB_JOBMESSAGE.ToString();

                string sql = $@"
                    INSERT INTO DOC_AZURETRANSJOB (AJB_JOB_ID, AJB_JOB_STARTTIME, AJB_CALLBACKURL, AJB_DEST_FULLPATH, AJB_JOBSTATUS, AJB_JOBMESSAGE)
                    VALUES ('{ajbJobId}', SYSDATE, '{ajbCallbackUrl}', '{ajbDestFullPath}', '{ajbJobStatus}', '{ajbJobMessage}')";

                db.ExecNonQuery(sql);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        }
    }
}
