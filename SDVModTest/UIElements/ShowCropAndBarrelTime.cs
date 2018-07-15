﻿using Microsoft.Xna.Framework;
using UIInfoSuite.Extensions;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Resources;
using System.Reflection;
using System.Globalization;
using StardewValley.Objects;
using StardewModdingAPI;
using StardewValley.Locations;
using StardewValley.Buildings;

namespace UIInfoSuite.UIElements
{
    class ShowCropAndBarrelTime : IDisposable
    {
        private Dictionary<int, String> _indexOfCropNames = new Dictionary<int, string>();
        private StardewValley.Object _currentTile;
        private TerrainFeature _terrain;
        private Building _currentTileBuilding = null;
        private readonly IModHelper _helper;

        public ShowCropAndBarrelTime(IModHelper helper)
        {
            _helper = helper;
        }

        public void ToggleOption(bool showCropAndBarrelTimes)
        {
            GraphicsEvents.OnPreRenderHudEvent -= DrawHoverTooltip;
            GameEvents.FourthUpdateTick -= GetTileUnderCursor;

            if (showCropAndBarrelTimes)
            {
                GraphicsEvents.OnPreRenderHudEvent += DrawHoverTooltip;
                GameEvents.FourthUpdateTick += GetTileUnderCursor;
            }
        }

        private void GetTileUnderCursor(object sender, EventArgs e)
        {
            if (Game1.currentLocation is BuildableGameLocation buildableLocation)
            {
                _currentTileBuilding = buildableLocation.getBuildingAt(Game1.currentCursorTile);
            }
            else
            {
                _currentTileBuilding = null;
            }

            if (Game1.currentLocation != null)
            {
                if (Game1.currentLocation.Objects == null ||
                    !Game1.currentLocation.Objects.TryGetValue(Game1.currentCursorTile, out _currentTile))
                {
                    _currentTile = null;
                }

                if (Game1.currentLocation.terrainFeatures == null ||
                    !Game1.currentLocation.terrainFeatures.TryGetValue(Game1.currentCursorTile, out _terrain))
                {
                    //if (_currentTile is IndoorPot pot &&
                    //    pot.hoeDirt.Value != null)
                    //{
                    //    _terrain = pot.hoeDirt.Value;
                    //}
                    //else
                    //{
                        _terrain = null;
                    //}
                }
            }
            else
            {
                _currentTile = null;
                _terrain = null;
            }
        }

        public void Dispose()
        {
            ToggleOption(false);
        }

        private void DrawHoverTooltip(object sender, EventArgs e)
        {
            if (_currentTileBuilding != null)
            {
                if (_currentTileBuilding is Mill millBuilding)
                {
                    if (millBuilding.input != null)
                    {
                        if (!millBuilding.input.isEmpty())
                        {
                            int wheatCount = 0;
                            int beetCount = 0;

                            foreach (var item in millBuilding.input.items)
                            {
                                if (item != null &&
                                    !String.IsNullOrEmpty(item.Name))
                                {
                                    switch (item.Name)
                                    {
                                        case "Wheat": wheatCount = item.Stack; break;
                                        case "Beet": beetCount = item.Stack; break;
                                    }
                                }
                            }

                            StringBuilder builder = new StringBuilder();

                            if (wheatCount > 0)
                                builder.Append(wheatCount + " wheat");

                            if (beetCount > 0)
                            {
                                if (wheatCount > 0)
                                    builder.Append(Environment.NewLine);
                                builder.Append(beetCount + " beets");
                            }

                            if (builder.Length > 0)
                            {
                                IClickableMenu.drawHoverText(
                                   Game1.spriteBatch,
                                   builder.ToString(),
                                   Game1.smallFont);
                            }
                        }
                    }
                }
            }
            else if (_currentTile != null &&
                (!_currentTile.bigCraftable ||
                _currentTile.minutesUntilReady > 0))
            {
                if (_currentTile.bigCraftable &&
                    _currentTile.minutesUntilReady > 0 &&
                    _currentTile.Name != "Heater")
                {
                    StringBuilder hoverText = new StringBuilder();
                    hoverText.AppendLine(_currentTile.heldObject.DisplayName);

                    if (_currentTile is Cask)
                    {
                        Cask currentCask = _currentTile as Cask;

                        hoverText.Append((int)(currentCask.daysToMature / currentCask.agingRate))
                            .Append(" " + _helper.SafeGetString(
                            LanguageKeys.DaysToMature));
                    }
                    else
                    {
                        int hours = _currentTile.minutesUntilReady / 60;
                        int minutes = _currentTile.minutesUntilReady % 60;
                        if (hours > 0)
                            hoverText.Append(hours).Append(" ")
                                .Append(_helper.SafeGetString(
                                    LanguageKeys.Hours))
                                .Append(", ");
                        hoverText.Append(minutes).Append(" ")
                            .Append(_helper.SafeGetString(
                                LanguageKeys.Minutes));
                    }
                    IClickableMenu.drawHoverText(
                        Game1.spriteBatch,
                        hoverText.ToString(),
                        Game1.smallFont);
                }
            }
            else if (_terrain != null)
            {
                if (_terrain is HoeDirt)
                {
                    HoeDirt hoeDirt = _terrain as HoeDirt;
                    if (hoeDirt.crop != null &&
                        !hoeDirt.crop.dead)
                    {
                        int num = 0;

                        if (hoeDirt.crop.fullyGrown &&
                            hoeDirt.crop.dayOfCurrentPhase > 0)
                        {
                            num = hoeDirt.crop.dayOfCurrentPhase;
                        }
                        else
                        {
                            for (int i = 0; i < hoeDirt.crop.phaseDays.Count - 1; ++i)
                            {
                                if (hoeDirt.crop.currentPhase == i)
                                    num -= hoeDirt.crop.dayOfCurrentPhase;

                                if (hoeDirt.crop.currentPhase <= i)
                                    num += hoeDirt.crop.phaseDays[i];
                            }
                        }

                        if (hoeDirt.crop.indexOfHarvest > 0)
                        {
                            String hoverText = _indexOfCropNames.SafeGet(hoeDirt.crop.indexOfHarvest);
                            if (String.IsNullOrEmpty(hoverText))
                            {
                                hoverText = new StardewValley.Object(new Debris(hoeDirt.crop.indexOfHarvest, Vector2.Zero, Vector2.Zero).chunkType, 1).DisplayName;
                                _indexOfCropNames.Add(hoeDirt.crop.indexOfHarvest, hoverText);
                            }

                            StringBuilder finalHoverText = new StringBuilder();
                            finalHoverText.Append(hoverText).Append(": ");
                            if (num > 0)
                            {
                                finalHoverText.Append(num).Append(" ")
                                    .Append(_helper.SafeGetString(
                                        LanguageKeys.Days));
                            }
                            else
                            {
                                finalHoverText.Append(_helper.SafeGetString(
                                    LanguageKeys.ReadyToHarvest));
                            }
                            IClickableMenu.drawHoverText(
                                Game1.spriteBatch,
                                finalHoverText.ToString(),
                                Game1.smallFont);
                        }
                    }
                }
                else if (_terrain is FruitTree)
                {
                    FruitTree tree = _terrain as FruitTree;

                    var text = new StardewValley.Object(new Debris(tree.indexOfFruit, Vector2.Zero, Vector2.Zero).chunkType, 1).DisplayName;
                    if (tree.daysUntilMature > 0)
                    {
                        text += Environment.NewLine + tree.daysUntilMature + " " +
                                _helper.SafeGetString(
                                    LanguageKeys.DaysToMature);

                    }
                    IClickableMenu.drawHoverText(
                            Game1.spriteBatch,
                            text,
                            Game1.smallFont);
                }
            }
        }
    }
}
