using cAlgo.API;
using cAlgo.API.Internals;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace cAlgo.Robots
{
    [Robot(AccessRights = AccessRights.FullAccess, AddIndicators = true)]
    public class Tradingdaycontinue : Robot
    {
      [Parameter("Trade enable", DefaultValue = true, Group = "Trade")]
      public bool IsTradeEnable { get; set; }
      [Parameter("Day to stop", DefaultValue = 1, Group = "Trade")]
      public int DayToStop { get; set; }
      [Parameter("ChatId", Group = "Telegram Settings")]
      public string ChatId { get; set; }

      [Parameter("Token", Group = "Telegram Settings")]
      public string Token { get; set; }

      private Telegram telegram;
      private Border _accountInfosPanel;
      private TextBlock _accountInfosText;
      private int _dayCount;
      DateTime _currentDay;

    protected override void OnStart()
    {
      //System.Diagnostics.Debugger.Launch();
        _currentDay = Server.Time.Date;
        if (!IsBacktesting && !string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(ChatId))
            telegram = new Telegram(token: Token, chatId: long.Parse(ChatId));
        else
          telegram = new Telegram();

        SendBotInformation();
        SendHistoricalTrades(3);

        CreateAccountInfosPanel();
        UpdateAccountInfosPanel();
    }

    protected override void OnTick()
    {
      UpdateAccountInfosPanel();
    }

    protected override void OnBar()
    {
      if (Server.Time.Date.CompareTo(_currentDay) > 0)
      {
        _dayCount++; 
        if(_dayCount > DayToStop)
        {
          telegram.SendMessage(message: $"The bot has been running successfully for {_dayCount} days.", isBacktesting: IsBacktesting);
          Stop();
        }
      }

      var germanyTimeNow = GetGermanyTimeNow();
      if (germanyTimeNow.Hour == 10 && germanyTimeNow.Minute == 0)
      {
        if (IsTradeEnable && CountTradedOfCurrentDay() == 0)
        {
          Random random = new Random();
          int value = random.Next(0, 2);
          if (value == 0)
          {
            ExecuteMarketOrder(
              TradeType.Buy,
              SymbolName,
              Symbol.NormalizeVolumeInUnits(0.01, RoundingMode.Down),
              "BUY",
              20,
              20);
          }
          else if (value == 1)
          {
            ExecuteMarketOrder(
              TradeType.Sell,
              SymbolName,
              Symbol.NormalizeVolumeInUnits(0.01, RoundingMode.Down),
              "Sell",
              20,
              20);
          }

          if(!GetAllOpenedPositions().Any())
            SendNoTradeWarning();
        }
      }

      if (germanyTimeNow.Hour == 12 && germanyTimeNow.Minute == 0)
        SendNoTradeWarning();

      if (germanyTimeNow.Hour == 15 && germanyTimeNow.Minute == 0)
        SendNoTradeWarning();

      if (germanyTimeNow.Hour == 17 && germanyTimeNow.Minute == 0)
        SendNoTradeWarning();

      if (germanyTimeNow.Hour == 20 && germanyTimeNow.Minute == 0)
        SendNoTradeWarning();
    }

    private void SendNoTradeWarning()
    {
      if (CountTradedOfCurrentDay() == 0)
        telegram.SendMessage(message: "There’s still no trade today. Place one! ", isBacktesting: IsBacktesting);
    }

    protected override void OnStop()
    {
      telegram.SendMessage(message: "🔴 bot is stopped \n" + DateTime.Now, isBacktesting: IsBacktesting);
    }
    private void CreateAccountInfosPanel()
    {
      _accountInfosText = new TextBlock
      {
        ForegroundColor = Color.White,
        FontSize = 12,
        TextAlignment = TextAlignment.Left
      };

      _accountInfosPanel = new Border
      {
        BackgroundColor = Color.FromArgb(180, 20, 20, 20),
        BorderColor = Color.DodgerBlue,
        BorderThickness = 1,
        CornerRadius = 5,
        Padding = 10,
        Width = 250,
        Child = _accountInfosText,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
        Margin = 10
      };

      Chart.AddControl(_accountInfosPanel);
    }

    private void UpdateAccountInfosPanel()
    {
      var trades = History;

      int totalTrades = trades.Count;
      int wins = trades.Count(t => t.NetProfit > 0);
      int losses = trades.Count(t => t.NetProfit < 0);
      double netProfit = trades.Sum(t => t.NetProfit);
      double winRate = totalTrades > 0 ? (double)wins / totalTrades * 100 : 0;



      _accountInfosText.Text =
          $"{Account.BrokerName} - {Account.Number} ({SymbolName})\n" +
          $"Chat name: {telegram.chatName}\n\n" +
          $"Day to stop: {DayToStop}\n" +
          $"Completed day: {_dayCount}\n\n" +
          $"Total : {totalTrades}\n" +
          $"Winning : {wins}          " + $"Win Rate : {winRate:F2}%\n" +
          $"Losing : {losses}         " + $"Net Profit : {netProfit:F2}";
    }
    private void SendHistoricalTrades(int prevDay)
    {
      var info = new StringBuilder();
      info.AppendLine($@"💰 trade in last {prevDay} day");
      info.AppendLine("");

      var from = Server.Time.AddDays(-Math.Abs(prevDay));

      var recentTrades = History.Where(t => t.ClosingTime >= from).ToList();

      if (!recentTrades.Any())
      {
        info.AppendLine("no trade");
        telegram.SendMessage(message: info.ToString(), isBacktesting: IsBacktesting);
        return;
      }

      double sumPercent = 0;
      double sum = 0;
      DateTime dayCount = default;
      foreach (var trade in recentTrades)
      {
        if (!dayCount.Equals(trade.EntryTime.Date))
        {
          dayCount = trade.EntryTime.Date;
          info.AppendLine("-----------------------");
          info.AppendLine($"{dayCount}");
        }

        var percent = Math.Round(value: trade.NetProfit / trade.Balance * 100, digits: 1);
        sumPercent += percent;
        sum += trade.NetProfit;
        info.AppendLine($"{trade.SymbolName} {trade.Label} {percent}% {trade.NetProfit} {Account.Asset.Name.ToLower()}");
      }

      info.AppendLine("");
      info.AppendLine($"Sum: {sumPercent}% {sum}{Account.Asset.Name.ToLower()}");
      telegram.SendMessage(message: info.ToString(), isBacktesting: IsBacktesting);
    }
    private void SendBotInformation(string subjectPrefix = "Bot Info")
    {
      var info = new StringBuilder();

      info.AppendLine($@"🤖 Trading day continue Bot {GetInstanceId()} ");
      info.AppendLine($"📈 Symbol: {SymbolName}");
      info.AppendLine($"🏦 Account: {Account.Number} ({Account.Asset.Name}) - {Account.BrokerName}");
      info.AppendLine($"💰 Balance: {Account.Balance}");
      info.AppendLine($"🕒 {Server.Time.ToString(CultureInfo.InvariantCulture)}");

      telegram.SendMessage(message: info.ToString().Trim(), isBacktesting: IsBacktesting);
    }
    private string GetInstanceId()
    {
      var match = Regex.Match(input: InstanceId, pattern: @"[^-]+$");

      return match.Success ? match.Value : InstanceId;
    }

    private DateTime GetGermanyTimeNow() =>
      TimeZoneInfo.ConvertTimeFromUtc(dateTime: Server.Time,
                                    destinationTimeZone: TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
    private int CountTradedOfCurrentDay()
    {
      var from = Server.Time;

      var recentTrades = History.Where(t => DateTime.Compare(t.ClosingTime.Date, from.Date) == 0).ToList();
      return recentTrades.Count;
    }

    private List<Position> GetAllOpenedPositions()
    {
      return Positions.Where(pos => string.Equals(a: pos.Symbol.Name, b: SymbolName)).ToList();
    }
  }
}