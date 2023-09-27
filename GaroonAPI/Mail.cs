using GaroonAPI.GaroonService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GaroonAPI
{
    internal class Mail
    {
        public event Action<string> OnMessageChanged;
        public event Action<string> OnMessageAdd;
        private string username;
        private string password;

        public async void GetMailInfo(string Username, string Password)
        {
            try
            {
                username = Username;
                password = Password;
                // メール用インスタンス
                MailBinding mailAPI = new MailBinding();

                bool IsFirst = true;

                SetElement(mailAPI, "MailGetMailVersions");

                // メール確認までできた
                OnMessageChanged?.Invoke("読み込み中");

                // 過去24時間のメールを取得したい
                DateTime now = DateTime.Now;
                var mailGetMailVersionsRequestType = new MailGetMailVersionsRequestType();
                mailGetMailVersionsRequestType.start = new DateTime(now.Year, now.Month, now.Day, 15, 0, 0).AddDays(-4);
                mailGetMailVersionsRequestType.end = new DateTime(now.Year, now.Month, now.Day, 15, 0, 0).AddDays(-3);
                mailGetMailVersionsRequestType.endSpecified = false;
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
                foreach (var a in responseMail)
                {
                    if (IsFirst)
                    {
                        OnMessageChanged?.Invoke(a.subject);
                        OnMessageAdd?.Invoke(a.date.ToString());
                        OnMessageAdd?.Invoke("");
                        IsFirst = false;
                    }
                    else
                    {
                        OnMessageAdd?.Invoke(a.subject);
                        OnMessageAdd?.Invoke(a.date.ToString());
                        OnMessageAdd?.Invoke("");
                    }
                }
            }
            catch (Exception ex)
            {
                OnMessageChanged?.Invoke(ex.Message);
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
    }
}
