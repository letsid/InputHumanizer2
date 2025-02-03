﻿using ExileCore2.Shared;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static InputHumanizer.InputHumanizer;

namespace InputHumanizer.Input
{
    internal class InputController : IInputController
    {
        internal InputController(InputHumanizer plugin, InputHumanizerSettings settings, InputLockManager manager)
        {
            Plugin = plugin;
            Settings = settings;
            Manager = manager;
        }

        ~InputController()
        {
            Manager.ReleaseController();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Manager.ReleaseController();
        }

        private InputHumanizer Plugin { get; }
        private InputHumanizerSettings Settings { get; }
        private InputLockManager Manager { get; }
        private Dictionary<Keys, DateTime> ButtonDelays = new Dictionary<Keys, DateTime>();

        public async SyncTask<bool> KeyDown(Keys key, CancellationToken cancellationToken = default)
        {
            await Task.Delay(GenerateDelay(), cancellationToken);

            Plugin.DebugLog("KeyDown: " + key);
            ExileCore2.Input.KeyDown(key);

            ButtonDelays[key] = DateTime.Now.AddMilliseconds(GenerateDelay());
            return true;
        }

        public async SyncTask<bool> KeyUp(Keys key, bool releaseImmediately = false, CancellationToken cancellationToken = default)
        {
            if (!releaseImmediately)
            {
                ButtonDelays.TryGetValue(key, out DateTime releaseTime);
                DateTime now = DateTime.Now;

                if (now < releaseTime)
                {
                    TimeSpan remainingDelay = releaseTime.Subtract(now);
                    Plugin.DebugLog("KeyUp remaining delay key:" + key + " delay: " + remainingDelay);
                    await Task.Delay(remainingDelay, cancellationToken);
                }
            }

            Plugin.DebugLog("KeyUp: " + key);
            ExileCore2.Input.KeyUp(key);
            ButtonDelays.Remove(key);
            return true;
        }

        public async SyncTask<bool> Click(CancellationToken cancellationToken = default)
        {
            return await Click(MouseButtons.Left, null, cancellationToken);
        }

        public async SyncTask<bool> Click(MouseButtons button, CancellationToken cancellationToken = default)
        {
            return await Click(button, null, cancellationToken);
        }

        public async SyncTask<bool> Click(MouseButtons button, Vector2 coordinate, CancellationToken cancellationToken = default)
        {
            return await Click(button, (Vector2?)coordinate, cancellationToken);
        }

        private async SyncTask<bool> Click(MouseButtons button, Vector2? coordinate, CancellationToken cancellationToken = default)
        {
            if (coordinate != null)
            {
                if (!await MoveMouse(coordinate.Value, cancellationToken))
                    return false;
            }

            Plugin.DebugLog("Click Delay");
            await Task.Delay(GenerateDelay(), cancellationToken);

            Plugin.DebugLog("Click " + button);
            ExileCore2.Input.Click(button);

            Plugin.DebugLog("Click Delay 2");
            await Task.Delay(GenerateDelay(), cancellationToken);
            return true;
        }

        public async SyncTask<bool> VerticalScroll(bool forward, int numClicks, CancellationToken cancellationToken = default)
        {
            return await VerticalScroll(forward, numClicks, null, cancellationToken);
        }

        public async SyncTask<bool> VerticalScroll(bool forward, int numClicks, Vector2? coordinate, CancellationToken cancellationToken = default)
        {
            if (coordinate != null)
            {
                if (!await MoveMouse(coordinate.Value, cancellationToken))
                    return false;
            }

            Plugin.DebugLog("Vertical Scroll Delay");
            await Task.Delay(GenerateDelay(), cancellationToken);

            Plugin.DebugLog("Vertical Scroll");
            ExileCore2.Input.VerticalScroll(forward, numClicks);

            Plugin.DebugLog("Vertical Scroll Delay 2");
            await Task.Delay(GenerateDelay(), cancellationToken);
            return true;
        }

        public async SyncTask<bool> MoveMouse(Vector2 coordinate, CancellationToken cancellationToken = default)
        {
            if (Settings.UseWindMouse)
            {
                return await MoveMouseWindMouseImpl(
                    coordinate, 
                    Settings.GravityStrength, 
                    Settings.WindStrength, 
                    Settings.WindMouseMinimumDelay, 
                    Settings.WindMouseMaximumDelay, 
                    Settings.StepSize, 
                    Settings.TargetArea, 
                    cancellationToken);
            }
            else
            {
                return await MoveMouse(
                    coordinate, 
                    Settings.MaximumInterpolationDistance, 
                    Settings.MinimumInterpolationDelay, 
                    Settings.MaximumInterpolationDelay, 
                    cancellationToken);
            }
        }

        public async SyncTask<bool> MoveMouse(Vector2 coordinate, int maxInterpolationDistance, int minInterpolationDelay, int maxInterpolationDelay, CancellationToken cancellationToken = default)
        {
            Plugin.DebugLog("Mouse Move start");
            return await Mouse.MoveMouse(Plugin, coordinate, maxInterpolationDistance, minInterpolationDelay, maxInterpolationDelay, cancellationToken);
        }

        public async SyncTask<bool> MoveMouseWindMouseImpl(Vector2 coordinate, double gravityStrength, double windStrength, int minInterpolationDelay, int maxInterpolationDelay, double stepSize, double targetArea, CancellationToken cancellationToken = default)
        {
            Plugin.DebugLog("Mouse Move start");
            var currentPos = ExileCore2.Input.ForceMousePosition;
            return await Mouse.WindMouseImpl(
                Plugin,
                currentPos.X,
                currentPos.Y,
                coordinate.X,
                coordinate.Y,
                gravityStrength,
                windStrength,
                minInterpolationDelay,
                maxInterpolationDelay,
                stepSize,
                targetArea,
                cancellationToken);
        }

        public int GenerateDelay()
        {
            return Delay.GetDelay(Settings.MinimumDelay, Settings.MaximumDelay, Settings.DelayMean, Settings.DelayStandardDeviation);
        }
    }
}