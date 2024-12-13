using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared;
using ExileCore2.Shared.Cache;
using ExileCore2.Shared.Enums;
using ExileCore2.Shared.Helpers;
using Vector2 = System.Numerics.Vector2;

namespace MiscInformation
{
    public class MiscInformation : BaseSettingsPlugin<MiscInformationSettings>
    {
        private string areaName = "";

        private Dictionary<int, float> ArenaEffectiveLevels = new Dictionary<int, float>
        {
            {71, 70.94f},
            {72, 71.82f},
            {73, 72.64f},
            {74, 73.4f},
            {75, 74.1f},
            {76, 74.74f},
            {77, 75.32f},
            {78, 75.84f},
            {79, 76.3f},
            {80, 76.7f},
            {81, 77.04f},
            {82, 77.32f},
            {83, 77.54f},
            {84, 77.7f}
        };

        private TimeCache<bool> CalcXp;
        private bool CanRender;
        private Vector2 drawTextVector2;
        private string fps = "";
        private string latency = "";
        private RectangleF leftPanelStartDrawRect = RectangleF.Empty;
        private TimeCache<bool> LevelPenalty;
        private double levelXpPenalty, partyXpPenalty;
        private float percentGot;
        private double partyTime = 4000;
        private string ping = "";
        private DateTime startTime, lastTime;
        private long startXp, getXp, xpLeftQ;
        private float startY;
        private double time;
        private string Time = "";
        private string timeLeft = "";
        private TimeSpan timeSpan;
        private string xpGetLeft = "";
        private string xpRate = "";
        private string xpReceivingText = "";

        public float GetEffectiveLevel(int monsterLevel)
        {
            return Convert.ToSingle(-0.03 * Math.Pow(monsterLevel, 2) + 5.17 * monsterLevel - 144.9);
        }

        public override void OnLoad()
        {
            Order = -50;
        }

        public override bool Initialise()
        {
            GameController.LeftPanel.WantUse(() => Settings.Enable);
            CalcXp = new TimeCache<bool>(() =>
            {
                partyTime += time;
                time = 0;
                CalculateXp();
                var areaCurrentArea = GameController.Area.CurrentArea;

                if (areaCurrentArea == null)
                    return false;

                timeSpan = DateTime.UtcNow - areaCurrentArea.TimeEntered;

                Time = AreaInstance.GetTimeString(timeSpan);
                xpReceivingText = $"{xpRate}  *{levelXpPenalty * partyXpPenalty:p0}";

                xpGetLeft =
                    $"Got: {ConvertHelper.ToShorten(getXp, "0.00")} ({percentGot:P3})  Left: {ConvertHelper.ToShorten(xpLeftQ, "0.00")}";

                if (partyTime > 4900)
                {
                    var levelPenaltyValue = LevelPenalty.Value;
                }

                return true;
            }, 1000);

            LevelPenalty = new TimeCache<bool>(() =>
            {
                partyXpPenalty = PartyXpPenalty();
                levelXpPenalty = LevelXpPenalty();
                return true;
            }, 5000);

            GameController.EntityListWrapper.PlayerUpdate += OnEntityListWrapperOnPlayerUpdate;
            OnEntityListWrapperOnPlayerUpdate(this, GameController.Player);

            return true;
        }

        private void OnEntityListWrapperOnPlayerUpdate(object sender, Entity entity)
        {
            percentGot = 0;
            xpRate = "0.00 xp/h";
            timeLeft = "-h -m -s  to level up";
            getXp = 0;
            xpLeftQ = 0;

            startTime = lastTime = DateTime.UtcNow;
            startXp = entity.GetComponent<Player>().XP;
            levelXpPenalty = LevelXpPenalty();
        }

        public override void AreaChange(AreaInstance area)
        {
            LevelPenalty.ForceUpdate();
        }

        public override void Tick()
        {
            TickLogic();
        }

        private void TickLogic()
        {
            time += GameController.DeltaTime;
            var gameUi = GameController.Game.IngameState.IngameUi;

            if (GameController.Area.CurrentArea == null || gameUi.InventoryPanel.IsVisible || gameUi.SyndicatePanel.IsVisibleLocal)
            {
                CanRender = false;
                return;
            }

            var UIHover = GameController.Game.IngameState.UIHover;

            if (UIHover.Tooltip != null && UIHover.Tooltip.IsVisibleLocal &&
                UIHover.Tooltip.GetClientRectCache.Intersects(leftPanelStartDrawRect))
            {
                CanRender = false;
                return;
            }

            CanRender = true;

            var calcXpValue = CalcXp.Value;
            //var ingameStateCurFps = GameController?.Game?.IngameState?.CurFps ?? 1.0f;
            var areaSuffix = (GameController.Area.CurrentArea.RealLevel >= 68)
                ? $" - T{GameController.Area.CurrentArea.RealLevel - 67}"
                : "";

            fps = $"fps:(N/A)"; // ({ingameStateCurFps})";
            areaName = $"{GameController.Area.CurrentArea.DisplayName}{areaSuffix}";
            latency = $"({GameController.Game.IngameState.ServerData.Latency})";
            ping = $"ping:({GameController.Game.IngameState.ServerData.Latency})";
        }

        private void CalculateXp()
        {
            var level = GameController.Player.GetComponent<Player>()?.Level ?? 100;

            if (level >= 100)
            {
                // player can't level up, just show fillers
                xpRate = "0.00 xp/h";
                timeLeft = "--h--m--s";
                return;
            }

            long currentXp = GameController.Player.GetComponent<Player>().XP;
            getXp = currentXp - startXp;
            var rate = (currentXp - startXp) / (DateTime.UtcNow - startTime).TotalHours;
            xpRate = $"{ConvertHelper.ToShorten(rate, "0.00")} xp/h";

            if (level >= 0 && level + 1 < Constants.PlayerXpLevels.Length && rate > 1)
            {
                var xpLeft = Constants.PlayerXpLevels[level + 1] - currentXp;
                xpLeftQ = xpLeft;
                var time = TimeSpan.FromHours(xpLeft / rate);
                timeLeft = $"{time.Hours:0}h {time.Minutes:00}m {time.Seconds:00}s to level up";

                if (getXp == 0)
                    percentGot = 0;
                else
                {
                    percentGot = getXp / ((float) Constants.PlayerXpLevels[level + 1] - (float) Constants.PlayerXpLevels[level]);
                    if (percentGot < -100) percentGot = 0;
                }
            }
        }

        private double LevelXpPenalty()
        {
            var arenaLevel = GameController.Area.CurrentArea.RealLevel;
            var characterLevel = GameController.Player.GetComponent<Player>()?.Level ?? 100;


            if (arenaLevel > 70 && !ArenaEffectiveLevels.ContainsKey(arenaLevel))
            {
                // calculate the effective level and add it to dictionary
                ArenaEffectiveLevels.Add(arenaLevel, GetEffectiveLevel(arenaLevel));
            }
            var effectiveArenaLevel = arenaLevel < 71 ? arenaLevel : ArenaEffectiveLevels[arenaLevel];
            var safeZone = Math.Floor(Convert.ToDouble(characterLevel) / 16) + 3;
            var effectiveDifference = Math.Max(Math.Abs(characterLevel - effectiveArenaLevel) - safeZone, 0);
            double xpMultiplier;

            xpMultiplier = Math.Pow((characterLevel + 5) / (characterLevel + 5 + Math.Pow(effectiveDifference, 2.5)), 1.5);

            if (characterLevel >= 95) //For player levels equal to or higher than 95:
                xpMultiplier *= 1d / (1 + 0.1 * (characterLevel - 94));

            xpMultiplier = Math.Max(xpMultiplier, 0.01);

            return xpMultiplier;
        }

        private double PartyXpPenalty()
        {
            var entities = GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Player];

            if (entities.Count == 0)
                return 1;

            var levels = entities.Select(y => y.GetComponent<Player>()?.Level ?? 100).ToList();
            var characterLevel = GameController.Player.GetComponent<Player>()?.Level ?? 100;
            var partyXpPenalty = Math.Pow(characterLevel + 10, 2.71) / levels.Sum(level => Math.Pow(level + 10, 2.71));
            return partyXpPenalty * levels.Count;
        }

        public override void Render()
        {
            if (!CanRender)
                return;
            var origStartPoint = GameController.LeftPanel.StartDrawPoint;

            var rightHalfDrawPoint = origStartPoint.Translate(
                Settings.DrawXOffset.Value - GameController.IngameState.IngameUi.MapSideUI.Width);
            leftPanelStartDrawRect = new RectangleF(rightHalfDrawPoint.X, rightHalfDrawPoint.Y, 1, 1);

            var leftSideItems = new[]
            {
                //00:00
                (Time, Settings.TimerTextColor),
                //fps:(100)
                (fps, Settings.FpsTextColor),
                //ping:(100)
                (ping, Settings.LatencyTextColor)
            };

            var rightSideItems = new[]
            {
                //NameArea
                (areaName, GameController.Area.CurrentArea.AreaColorName),
                //-h-m-s to level
                (timeLeft, Settings.TimeLeftColor.Value),
                //0,00 xph*%
                (xpReceivingText, Settings.XphTextColor.Value),
                //GotLeft
                (xpGetLeft, Settings.XphGetLeft.Value)
            };
            var rightTextBounds = leftSideItems.Select(x => Graphics.MeasureText(x.Item1)).ToList()
                switch { var s => new Vector2(s.DefaultIfEmpty(Vector2.Zero).Max(x => x.X), s.Sum(x => x.Y)) };
            var leftTextBounds = rightSideItems.Select(x => Graphics.MeasureText(x.Item1)).ToList()
                switch { var s => new Vector2(s.DefaultIfEmpty(Vector2.Zero).Max(x => x.X), s.Sum(x => x.Y)) };

            var sumX = rightTextBounds.X + leftTextBounds.X + 5;
            var maxY = Math.Max(rightTextBounds.Y, leftTextBounds.Y);
            var leftHalfDrawPoint = rightHalfDrawPoint with { X = rightHalfDrawPoint.X - sumX };
            startY = leftHalfDrawPoint.Y;
            var bounds = new RectangleF(leftHalfDrawPoint.X, startY - 2, sumX, maxY);

            Graphics.DrawBox(bounds, Settings.BackgroundColor);

            foreach (var (text, color) in leftSideItems)
            {
                drawTextVector2 = Graphics.DrawText(text, leftHalfDrawPoint, color);
                leftHalfDrawPoint.Y += drawTextVector2.Y;
            }

            foreach (var (text, color) in rightSideItems)
            {
                drawTextVector2 = Graphics.DrawText(text, rightHalfDrawPoint, color, FontAlign.Right);
                rightHalfDrawPoint.Y += drawTextVector2.Y;
            }

            GameController.LeftPanel.StartDrawPoint = new Vector2(origStartPoint.X, origStartPoint.Y + maxY + 10);
        }
    }
}
