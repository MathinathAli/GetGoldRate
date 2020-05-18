using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace GetGoldRate
{
    public static class Function1
    {
        [FunctionName("FnGetGoldPrice")]
        public static async void Run([TimerTrigger("* 30 14 * * *")]TimerInfo myTimer, ILogger log)
        {
            string sendgrid_api = "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";
            SqlConnection con = new SqlConnection("Server=tcp:db-server-track.database.windows.net,1433;Initial Catalog=db-rate-track;Persist Security Info=False;User ID=username;Password=Password@123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"); 
            try
            {
                log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");                
                string site = "https://www.goodreturns.in/gold-rates/chennai.html";
                int today_rate=0;
                
                var htmlWeb = new HtmlWeb();                
                var documentNode = htmlWeb.Load(site).DocumentNode;
                var findclasses = documentNode.SelectNodes("//strong[contains(@id,'el')]");
                var text = string.Join(Environment.NewLine, findclasses.Select(x => x.InnerText));
                
                string[] splittedvalues;
                splittedvalues = text.Split(';');
                foreach (string str in splittedvalues)
                {
                    if (str.Contains(","))
                    {
                       today_rate=Convert.ToInt32( str.Trim().Replace(",", ""));
                    }
                }

                SqlCommand cmd = new SqlCommand("sp_upsert_gold", con);
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@price", today_rate);
                con.Open();
                cmd.ExecuteNonQuery();                
                log.LogInformation($"Function succeeded");
            }

            catch(Exception ex)
            {
                string message = ex.Message;
                log.LogInformation($"Function failed due to  {message}");
                await sendEmailUsingSendGrid(sendgrid_api,message);
            }
            finally
            {
                con.Close();
            }
        }

        public static async Task sendEmailUsingSendGrid(string apiKey, string message)
        {
            var client = new SendGridClient(apiKey);
            ////send an email message using the SendGrid Web API with a console application.  
            var msgs = new SendGridMessage()
            {
                From = new EmailAddress("mathinath.ali@gds.ey.com", "Ali"),
                Subject = message,
                TemplateId = "fb09a5fb-8bc3-4183-b648-dc6d48axxxxx",
                ////If you have html ready and dont want to use Template's   
                //PlainTextContent = "Hello, Email!",  
                //HtmlContent = "<strong>Hello, Email!</strong>",  
            };
            //if you have multiple reciepents to send mail  
            msgs.AddTo(new EmailAddress("mathinath.ali@gds.ey.com", "Ali"));            
            msgs.SetFooterSetting(true, "<strong>Regards,</strong><b> Ali", "Ali");              
            var responses = await client.SendEmailAsync(msgs);
        }
    }
}
