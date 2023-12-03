﻿using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using InputHumanizer.Input;
using System;
using static InputHumanizer.InputHumanizer;

namespace InputHumanizer
{
    public class InputHumanizer : BaseSettingsPlugin<InputHumanizerSettings>
    {
        public override bool Initialise()
        {
            GameController.PluginBridge.SaveMethod("InputHumanizer.TryGetInputController", (string requestingPlugin) =>
            {
                IInputController controller;
                if (TryGetInputController(requestingPlugin, out controller))
                {
                    return controller;
                }
                return null;
            });


            GameController.PluginBridge.SaveMethod("InputHumanizer.GetInputController", (string requestingPlugin, TimeSpan waitTime) =>
            {
                return GetInputController(requestingPlugin, waitTime);
            });

            return true;
        }

        public async SyncTask<IInputController> GetInputController(string requestingPlugin, TimeSpan waitTime)
        {
            IInputController controller = await InputLockManager.Instance.GetInputControllerLock(requestingPlugin, this, Settings, waitTime);
            if (controller == null)
            {
                LogError($"InputHumanizer - Plugin {requestingPlugin} requested input controller but {InputLockManager.Instance.PluginWithSemaphore} is still holding it. Try your action again later.");
            }
            else
            {
                DebugLog("Plugin: " + requestingPlugin + " successfully got input controller lock.");
            }

            return controller;
        }

        public bool TryGetInputController(string requestingPlugin, out IInputController controller)
        {
            controller = InputLockManager.Instance.TryGetInputController(requestingPlugin, this, Settings);
            bool wasSuccess = controller != null;
            if (wasSuccess)
            {
                DebugLog("Plugin: " + requestingPlugin + " successfully got input controller lock.");
            }
            else
            {
                DebugLog("Plugin: " + requestingPlugin + " failed to get input lock.");
            }

            return wasSuccess;
        }

        public void DebugLog(String message)
        {
            if (Settings.Debug)
            {
                LogMsg(message);
            }
        }

        public class InputHumanizerSettings : ISettings
        {
            [Menu("Minimum Interpolation Delay", "Minimum Delay in Milliseconds")]
            public RangeNode<int> MinimumInterpolationDelay { get; set; } = new(0, 0, 1000);

            [Menu("Maximum Interpolation Delay", "Maximum Delay in Milliseconds")]
            public RangeNode<int> MaximumInterpolationDelay { get; set; } = new(1000, 0, 1000);

            [Menu("Maximum Interpolation Distance")]
            public RangeNode<int> MaximumInterpolationDistance { get; set; } = new(2560, 0, 2560);

            [Menu("Delay Mean", "Mean of the Gaussian Distribution in Milliseconds")]
            public RangeNode<int> DelayMean { get; set; } = new(100, 0, 1000);

            [Menu("Delay Standard Deviation", "Standard Deviation of the Gaussian Distribution in Milliseconds")]
            public RangeNode<int> DelayStandardDeviation { get; set; } = new(50, 0, 1000);

            [Menu("Minimum Delay", "Minimum Delay in Milliseconds")]
            public RangeNode<int> MinimumDelay { get; set; } = new(50, 0, 1000);

            [Menu("Maximum Delay", "Maximum Delay in Milliseconds")]
            public RangeNode<int> MaximumDelay { get; set; } = new(150, 0, 1000);

            public ToggleNode Enable { get; set; } = new ToggleNode(true);
            public ToggleNode Debug { get; set; } = new ToggleNode(false);
        }
    }
}