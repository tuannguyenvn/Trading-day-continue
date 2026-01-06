using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace cAlgo.Robots
{
    public sealed class Telegram
    {
        private TelegramBotClient telegramBotClient;
        private ChatId chatId;
        private string token;
        private static readonly HttpClient httpClient = new HttpClient();
        private long lastUpdateId;
        public string chatName;

    internal Telegram() 
        { }

       internal Telegram(string token, long chatId)
       {
           ServicePointManager.Expect100Continue = true;
           ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
           // PivotPoint cBot Manager 
           this.chatId = new ChatId(chatId);
           this.token = token;
           telegramBotClient = new TelegramBotClient(token);
           chatName = GetChatName();
       }

    internal async void SendMessage(string message, bool isBacktesting)
       {
           if (this.telegramBotClient == null || isBacktesting)
               return;
           message += "\napp.ctrader.com";
           var url = $"https://api.telegram.org/bot{token}/sendMessage?chat_id={chatId}&text={message}";
           try
           {
               await httpClient.GetAsync(url);
           }
           catch (HttpRequestException ex)
           { }
       }

       internal async Task SendTelegramPhoto(string filePath, string id)
       {
      if (this.telegramBotClient == null) return;
           try
           {
               using var client = new HttpClient();
               using var form = new MultipartFormDataContent();
               client.Timeout = TimeSpan.FromSeconds(10);

               var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(filePath));
               fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");

               form.Add(content: fileContent, name: "photo", fileName: Path.GetFileName(filePath));
               form.Add(content: new StringContent(chatId.ToString()), name: "chat_id");
               form.Add(content: new StringContent(id), name: "caption");

               var response = await client.PostAsync(requestUri: $"https://api.telegram.org/bot{token}/sendPhoto", content: form);

               //if (!response.IsSuccessStatusCode)
               //  Print("❌ Error sending image: " + await response.Content.ReadAsStringAsync());
           }
           catch (TaskCanceledException)
           { }
           catch (Exception)
           { }
       }

       internal string MessengeListen() 
       {
           var url = $"https://api.telegram.org/bot{token}/getUpdates";
           if (lastUpdateId >= 0)
               url += $"?offset={lastUpdateId + 1}";

           HttpResponseMessage response;

           try
           {
               response = httpClient.GetAsync(url).GetAwaiter().GetResult();
           }
           catch (HttpRequestException httpEx)
           {
               return string.Empty; 
           }
           catch (TaskCanceledException tcEx)
           {
               return string.Empty; 
           }
           catch (Exception ex)
           {
               return string.Empty;
           }

           if (!response.IsSuccessStatusCode)
           {
               return string.Empty;
           }

           var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
           TelegramResponse telegramResponse;

           try
           {
               telegramResponse = JsonConvert.DeserializeObject<TelegramResponse>(json);
           }
           catch (Newtonsoft.Json.JsonException jsonEx)
           {
               return string.Empty;
           }

           if (telegramResponse == null || telegramResponse.Result == null || !telegramResponse.Ok)
               return string.Empty;

           foreach (var resultResponse in telegramResponse.Result)
           {
               if (resultResponse.Update_Id <= lastUpdateId)
                  continue;

               var msg = resultResponse.Message;
               if (msg == null || string.IsNullOrEmpty(msg.Text))
               {
                  lastUpdateId = 0;
                  continue;
               }

               lastUpdateId = resultResponse.Update_Id;
               //var chatId = msg.Chat.Id.ToString();
               var text = msg.Text.Trim();
               return text.ToLower();
           }

           return string.Empty;
       }

    internal string GetChatName()
    {
      var url = $"https://api.telegram.org/bot{token}/getMe";

      HttpResponseMessage response;

      response = httpClient.GetAsync(url).GetAwaiter().GetResult();

      var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

      using JsonDocument doc = JsonDocument.Parse(json);

      return doc.RootElement
              .GetProperty("result")
              .GetProperty("first_name")
              .GetString(); ;
    }

    private sealed class TelegramResponse
       {
           public bool Ok { get; set; }
           public List<ResultItem> Result { get; set; }
       }

       private class ResultItem
       {
           public long Update_Id { get; set; }
           public Message Message { get; set; }
       }

       private class Message
       {
           public long Message_Id { get; set; }
           public From From { get; set; }
           public Chat Chat { get; set; }
           public long Date { get; set; }
           public string Text { get; set; }
       }

       private class From
       {
           public long Id { get; set; }
           public bool Is_Bot { get; set; }
           public string First_Name { get; set; }
           public string Last_Name { get; set; }
           public string Language_Code { get; set; }
       }

       internal enum RequestTexts
        {
            ping,
            his1, his2,
            pic,
            setrisk1, setrisk2
        }

        // Position Tracker
        // Token: 7255549686:AAF6t8oppSpWh3VkbElqUAFvDZkWRpa9PDE
        // ChatId: 5009776683

        // Pivot Tracker
        // Token: 8030985155:AAEPWaN6jwzN77VW_Vm1-FYHItWGjo0IwWE
        // ChatId: 5009776683

        // Use this to get chat id
        //https://api.telegram.org/bot$TOKEN/getUpdates
    }
}
