using GaroonAPI.GaroonService;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GaroonAPI
{
    internal class Mail
    {
        /* 各MailBindingやらMessageBindingのReference.csの中に以下を追加する必要あるよ
         * public ActionElement action;
         * public SecurityElement security;
         * public TimestampElement timeStamp;
         */

        /* APIをたたく前にReference.csの各メソッドに以下を追加する必要あるよ
         * [System.Web.Services.Protocols.SoapHeaderAttribute("action", Direction = SoapHeaderDirection.InOut)]
         * [System.Web.Services.Protocols.SoapHeaderAttribute("security", Direction = SoapHeaderDirection.InOut)]
         * [System.Web.Services.Protocols.SoapHeaderAttribute("timeStamp", Direction = SoapHeaderDirection.InOut)]
         */


        public event Action<string> OnMessageChanged;
        public event Action<string> OnMessageAdd;
        public event Action GetMailInfoCompleted;
        private string username;
        private string password;

        public async void GetMailInfo(string Username, string Password, string Date)
        {
            try
            {
                username = Username;
                password = Password;
                // メール用インスタンス
                MailBinding mailAPI = new MailBinding();

                IsFirst = true;

                SetElement(mailAPI, "MailGetMailVersions");

                // メール確認までできた
                OnMessageChanged?.Invoke("読み込み中");

                // 過去24時間のメールを取得したい
                DateTime now = DateTime.UtcNow;
                int adddays;
                if(int.TryParse(Date,out adddays))
                {
                    adddays = -adddays;
                }
                else
                {
                    adddays = -1;
                }
                var mailGetMailVersionsRequestType = new MailGetMailVersionsRequestType();
                mailGetMailVersionsRequestType.start = now.AddDays(adddays);
                //mailGetMailVersionsRequestType.start = new DateTime(now.Year, now.Month, now.Day, 15, 0, 0).AddDays(-4);
                //mailGetMailVersionsRequestType.end = new DateTime(now.Year, now.Month, now.Day, 15, 0, 0).AddDays(-3);
                ItemVersionResultType[] responseItemVersion = await mailAPI.MailGetMailVersionsAsyncWrapper(mailGetMailVersionsRequestType);

                // 各メールのIDを配列に変換
                List<string> idList = new List<string>();
                foreach (var a in responseItemVersion)
                {
                    idList.Add(a.id);
                }
                string[] idArray = idList.ToArray();

                // SOAPヘッダーセット
                SetElement(mailAPI, "MailGetMailsById");

                // メールの詳細情報取得
                MailType[] responseMail = await mailAPI.MailGetMailsByIDAsyncWrapper(idArray);
                List<string> kentaroList = new List<string>();
                foreach (var a in responseMail)
                {
                    if (a.from != null && a.from.address == "technicalcentermain1@techk1.com")
                    {
                        if (a.subject != null && a.subject.Contains("舟山中天重工"))
                        {
                            kentaroList = DownloadKentaro(mailAPI, a, kentaroList);
                        }
                        else
                        {
                            if (a.to != null)
                            {
                                foreach (var b in a.to)
                                {
                                    if (b.address != null)
                                    {
                                        switch (b.address)
                                        {
                                            case "info@logi-seo.co.jp":
                                                kentaroList = DownloadKentaro(mailAPI, a, kentaroList);
                                                break;

                                            case "hasebe@logi-seo.co.jp":
                                                kentaroList = DownloadKentaro(mailAPI, a, kentaroList);
                                                break;
                                        }
                                    }
                                }
                            }
                        }

                    }

                }
                if (kentaroList != null && kentaroList.Count > 0)
                {
                    SetElement(mailAPI, "MailRemoveMails");
                    MailRemoveMailsRequestType req = new MailRemoveMailsRequestType();
                    req.mail_id = kentaroList.ToArray();
                    foreach (var e in kentaroList)
                    {
                        OnMessageAdd?.Invoke(e);
                    }
                    await mailAPI.MailRemoveMailsAsyncWrapper(req);
                    OnMessageAdd?.Invoke("完了");
                    GetMailInfoCompleted?.Invoke();
                }
                else
                {
                    OnMessageAdd?.Invoke("健太郎 0");
                    GetMailInfoCompleted?.Invoke();
                }
            }
            catch (Exception ex)
            {
                OnMessageAdd?.Invoke(ex.Message);
                GetMailInfoCompleted?.Invoke();
            }
        }

        /// <summary>
        /// MailAPIのSOAPヘッダー設定
        /// </summary>
        /// <param name="mailAPI"></param>
        /// <param name="actionName"></param>
        private void SetElement(MailBinding mailAPI, string actionName)
        {
            ActionElement actionElement = new ActionElement();
            SecurityElement securityElement = new SecurityElement();
            TimestampElement timestampElement = new TimestampElement();
            UsernameTokenElement userNameTokenElement = new UsernameTokenElement();

            // アクションエレメント
            actionElement.actionValue = actionName;

            // セキュリティエレメント
            userNameTokenElement.Username = username; //送信者のﾕｰｻﾞｰｱｶｳﾝﾄ;
            userNameTokenElement.Password = password; //送信者のﾕｰｻﾞｰﾊﾟｽﾜｰﾄ;
            securityElement.usernameToken = userNameTokenElement;

            // タイムスタンプエレメント
            timestampElement.Created = DateTime.Now;
            timestampElement.Expires = timestampElement.Created.AddDays(8);

            // SOAPヘッダー
            mailAPI.action = actionElement;
            mailAPI.security = securityElement;
            mailAPI.timeStamp = timestampElement;
        }

        /// <summary>
        /// 健太郎からのスケジュールファイルダウンロード
        /// </summary>
        /// <param name="mailAPI"></param>
        /// <param name="response"></param>
        /// <param name="kentaroList"></param>
        /// <returns></returns>
        private List<string> DownloadKentaro(MailBinding mailAPI, MailType response, List<string>kentaroList)
        { 
            try
            {
                if (IsFirst)
                {
                    OnMessageChanged?.Invoke("From 健太郎");
                    OnMessageAdd?.Invoke(response.subject);
                    OnMessageAdd?.Invoke(response.date.AddHours(9).ToString());
                    if (!(response.file == null) && response.file.Length > 0)
                    {
                        foreach (var file in response.file)
                        {
                            // 添付ファイルのみ削除しているか判定
                            if (file.size > 0)
                            {
                                // SOAPヘッダーセット
                                SetElement(mailAPI, "MailFileDownload");
                                MailFileDownloadRequestType req = new MailFileDownloadRequestType();
                                req.mail_id = response.key;
                                req.file_id = file.id;
                                MailFileDownloadResponseType res = mailAPI.MailFileDownload(req);

                                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "NissenVesselSchedules");
                                if (!Directory.Exists(folderPath))
                                {
                                    Directory.CreateDirectory(folderPath);
                                }


                                string filePath = Path.Combine(folderPath, response.date.AddHours(9).ToString("yyyyMMdd") + "_" + file.name);

                                File.WriteAllBytes(filePath, res.file.content);
                                OnMessageAdd?.Invoke("File Downloaded");
                            }
                            else
                            {
                                OnMessageAdd?.Invoke("File Deleted");
                            }
                        }
                        kentaroList.Add(response.key);
                    }
                    else
                    {
                        OnMessageAdd?.Invoke("404 File NotFound");
                    }
                    IsFirst = false;
                    OnMessageAdd?.Invoke("");
                }
                else 
                {
                    OnMessageAdd?.Invoke("From 健太郎");
                    OnMessageAdd?.Invoke(response.subject);
                    OnMessageAdd?.Invoke(response.date.AddHours(9).ToString());
                    if (!(response.file == null) && response.file.Length > 0)
                    {
                        foreach (var file in response.file)
                        {
                            // 添付ファイルのみ削除しているか判定
                            if (file.size > 0)
                            {
                                // SOAPヘッダーセット
                                SetElement(mailAPI, "MailFileDownload");
                                MailFileDownloadRequestType req = new MailFileDownloadRequestType();
                                req.mail_id = response.key;
                                req.file_id = file.id;
                                MailFileDownloadResponseType res = mailAPI.MailFileDownload(req);

                                string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "NissenVesselSchedules");
                                if (!Directory.Exists(folderPath))
                                {
                                    Directory.CreateDirectory(folderPath);
                                }


                                string filePath = Path.Combine(folderPath, response.date.AddHours(9).ToString("yyyyMMdd") + "_" + file.name);

                                File.WriteAllBytes(filePath, res.file.content);
                                OnMessageAdd?.Invoke("File Downloaded");
                            }
                            else
                            {
                                OnMessageAdd?.Invoke("File Deleted");
                            }
                        }
                        kentaroList.Add(response.key);
                    }
                    else
                    {
                        OnMessageAdd?.Invoke("404 File NotFound");
                    }
                    OnMessageAdd?.Invoke("");
                }

                return kentaroList;
            }
            catch(Exception ex)
            {
                OnMessageAdd?.Invoke(ex.Message);
                kentaroList.Clear();
                return kentaroList;
            }
        }

        private bool IsFirst
        {
            get; set;
        }

    }
}
