using System;
using cAlgo.API;
using cAlgo.API.Collections;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;

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

      protected override void OnStart()
          {
              // To learn more about cTrader Algo visit our Help Center:
              // https://help.ctrader.com/ctrader-algo/

          }

          protected override void OnTick()
          {
              // Handle price updates here
          }

          protected override void OnStop()
          {
              // Handle cBot stop here
          }
    }
}