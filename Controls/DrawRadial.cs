﻿using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Controls.Intern;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manlaan.Mounts.Controls
{
    internal class RadialMount
    {
        public double AngleBegin { get; set; }
        public double AngleEnd { get; set; }
        public Mount Mount { get; set; }
        public Texture2D Texture { get; set; }
        public int ImageX { get; set; }
        public int ImageY { get; set; }
        public bool Selected { get; set; }
        public bool Default { get; internal set; }
    }

    internal class DrawRadial : Control
    {
        private static readonly Logger Logger = Logger.GetLogger<DrawRadial>();
        private readonly Helper _helper;
        private List<RadialMount> RadialMounts = new List<RadialMount>();
        private RadialMount SelectedMount => RadialMounts.SingleOrDefault(m => m.Selected);

        public override int ZIndex { get => base.ZIndex; set => base.ZIndex = value; }
        public bool IsActionCamToggledOnMount { get; private set; }

        int radius = 0;
        int mountIconSize = 0;
        int _maxRadialDiameter = 0;

        private Point SpawnPoint = default;

        public DrawRadial(Helper helper)
        {
            Visible = false;
            Padding = Thickness.Zero;
            _helper = helper;

            Shown += async (sender, e) => await HandleShown(sender, e);
            Hidden += async (sender, e) => await HandleHidden(sender, e);
        }

        protected override CaptureType CapturesInput()
        {
            return CaptureType.Mouse;
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds) {
            RadialMounts.Clear();
            var mounts = Module._availableOrderedMounts;

            Mount mountToPutInCenter = _helper.GetCenterMount();
            if (mountToPutInCenter != null && mountToPutInCenter.IsAvailable)
            {
                if (Module._settingMountRadialRemoveCenterMount.Value)
                {
                    mounts.Remove(mountToPutInCenter);
                }
                var texture = _helper.GetImgFile(mountToPutInCenter.ImageFileName);
                int loc = radius;
                RadialMounts.Add(new RadialMount { Texture = texture, Mount = mountToPutInCenter, ImageX = loc, ImageY = loc, Default = true });
            }


            double currentAngle = 0;
            var partAngleStep = Math.PI * 2 / mounts.Count();
            foreach (var mount in mounts)
            {
                var angleMid = currentAngle + partAngleStep / 2;
                var angleEnd = currentAngle + partAngleStep;
                var texture = _helper.GetImgFile(mount.ImageFileName);

                int x = (int)Math.Round(radius + radius * Math.Cos(angleMid));
                int y = (int)Math.Round(radius + radius * Math.Sin(angleMid));
                RadialMounts.Add(new RadialMount
                {
                    Texture = texture,
                    Mount = mount,
                    ImageX = x,
                    ImageY = y,
                    AngleBegin = currentAngle,
                    AngleEnd = angleEnd
                });
                currentAngle = angleEnd;
            }

            var mousePos = Input.Mouse.Position;
            var diff = mousePos - SpawnPoint;
            var angle = Math.Atan2(diff.Y, diff.X);
            if (angle < 0)
            {
                angle += Math.PI * 2;
            }

            var length = new Vector2(diff.Y, diff.X).Length();
            
            foreach (var radialMount in RadialMounts)
            {
                if (length < mountIconSize * Math.Sqrt(2) / 2)
                {
                    radialMount.Selected = radialMount.Default;
                } 
                else 
                {
                    radialMount.Selected = radialMount.AngleBegin <= angle && radialMount.AngleEnd > angle;
                }

                spriteBatch.DrawOnCtrl(this, radialMount.Texture, new Rectangle(radialMount.ImageX, radialMount.ImageY, mountIconSize, mountIconSize), null, Color.White * (radialMount.Selected ? 1f : Module._settingMountRadialIconOpacity.Value));
            }
        }

        public async Task TriggerSelectedMountAsync()
        {
            await (SelectedMount?.Mount.DoHotKey() ?? Task.CompletedTask);
        }


        private async Task HandleShown(object sender, EventArgs e)
        {
            Logger.Debug("HandleShown entered");
            var isCursorVisible = GameService.Input.Mouse.CursorIsVisible;
            if (!isCursorVisible)
            {
                IsActionCamToggledOnMount = true;
                await _helper.TriggerKeybind(Module._settingMountRadialToggleActionCameraKeyBinding);
                Logger.Debug("HandleShown turned off action cam");
            }

            _maxRadialDiameter = Math.Min(GameService.Graphics.SpriteScreen.Width, GameService.Graphics.SpriteScreen.Height);
            mountIconSize = (int)(_maxRadialDiameter / 4 * Module._settingMountRadialIconSizeModifier.Value);
            radius = (int)((_maxRadialDiameter / 2 - mountIconSize / 2) * Module._settingMountRadialRadiusModifier.Value);
            Size = new Point(_maxRadialDiameter, _maxRadialDiameter);

            if (Module._settingMountRadialSpawnAtMouse.Value && isCursorVisible)
            {
                SpawnPoint = Input.Mouse.Position;
            }
            else
            {
                Mouse.SetPosition(GameService.Graphics.WindowWidth / 2, GameService.Graphics.WindowHeight / 2, true);
                SpawnPoint = new Point(GameService.Graphics.SpriteScreen.Width / 2, GameService.Graphics.SpriteScreen.Height / 2);
            }

            Location = new Point(SpawnPoint.X - radius - mountIconSize / 2, SpawnPoint.Y - radius - mountIconSize / 2);
        }

        private async Task HandleHidden(object sender, EventArgs e)
        {
            Logger.Debug("HandleHidden entered");
            if (IsActionCamToggledOnMount)
            {
                await _helper.TriggerKeybind(Module._settingMountRadialToggleActionCameraKeyBinding);
                IsActionCamToggledOnMount = false;
                Logger.Debug("HandleHidden turned back on action cam");
            }
            await TriggerSelectedMountAsync();
        }
    }
}