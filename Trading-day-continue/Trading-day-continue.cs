using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using System;
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
      public bool IsEnable { get; set; }
      [Parameter("ChatId", Group = "Telegram Settings")]
      public string ChatId { get; set; }

      [Parameter("Token", Group = "Telegram Settings")]
      public string Token { get; set; }

      private Telegram telegram;
      private Border _accountInfosPanel;
      private TextBlock _accountInfosText;

    protected override void OnStart()
      {
        if (!IsBacktesting && !string.IsNullOrEmpty(Token) && !string.IsNullOrEmpty(ChatId))
          telegram = new Telegram(token: Token, chatId: long.Parse(ChatId));
        else
          telegram = new Telegram();

        SendBotInformation();
        SendHistoricalTrades(3);


    }

    protected override void OnTick()
      {
          // Handle price updates here
      }

      protected override void OnStop()
      {
          // Handle cBot stop here
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


      double balance = Account.Balance;

      double risk05 = Math.Round(balance * 0.005, 0);
      double risk1 = Math.Round(balance * 0.01, 0);
      double risk2 = Math.Round(balance * 0.02, 0);
      double risk04 = Math.Round(balance * 0.004, 0);
      double risk08 = Math.Round(balance * 0.008, 0);
      double risk16 = Math.Round(balance * 0.016, 0);

      var equity = Account.Equity;
      var targetBalance = 0.0;
      var targetReachedText = "";


      _accountInfosText.Text =
          $"{Account.BrokerName} - {Account.Number} ({SymbolName})\n" +
          $"Chat name: {telegram.chatName}\n\n" +
          $"Target: {targetBalance}\n" +
          $"Equity: {equity} {targetReachedText}\n\n" +
          $"Balance: {Account.Balance}\n" +
          $"0.5% Risk : {risk05}      " + $"0.4% Risk : {risk04}\n" +
          $"1.0% Risk : {risk1}       " + $"0.8% Risk : {risk08}\n" +
          $"2.0% Risk : {risk2}       " + $"1.6% Risk : {risk16}\n\n" +
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

      info.AppendLine($@"🤖 Pivot Point Bot {GetInstanceId()} ");
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
    private void OnBuyClicked(ButtonClickEventArgs obj)
    {
      ExecuteMarketOrder(
          TradeType.Buy,
          SymbolName,
          1.1,
          "BUY_BUTTON");
    }

    private void OnSellClicked(ButtonClickEventArgs obj)
    {

      ExecuteMarketOrder(
          TradeType.Sell,
          SymbolName,
          1.1,
          "SELL_BUTTON");
    }
  }
}