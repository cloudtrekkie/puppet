﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Puppet.Common.Devices;
using Puppet.Common.Events;
using Puppet.Common.Models;

namespace Puppet.Common.Services
{
    /// <summary>
    /// Base class for Home Automation Platforms.
    /// </summary>
    public abstract class HomeAutomationPlatform
    {
        const string DEVICE_FILENAME = "devicemap.json";
        dynamic _deviceMap { get; }

        public ConcurrentDictionary<string, object> StateBag { get; set; }
        public abstract void DoAction(IDevice device, string action, string[] args = null);
        public abstract Task SendNotification(string notificationText);
        
        public abstract Task StartAutomationEventWatcher();

        public event EventHandler<AutomationEventEventArgs> AutomationEvent;

        public HomeAutomationPlatform(IConfiguration configuration)
        {
            this._deviceMap = JObject.Parse(
                File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), DEVICE_FILENAME)));
        }

        public abstract Task<SunriseAndSunset> GetSunriseAndSunset();

        protected virtual void OnAutomationEvent(AutomationEventEventArgs e)
        {
            AutomationEvent?.Invoke(this, e);
        }

        public string LookupDeviceId(string mappedDeviceName)
        {
            return ParseAndLookupMappedDeviceName(this._deviceMap, mappedDeviceName);
        }
        string ParseAndLookupMappedDeviceName(dynamic obj, string mappedDeviceName)
        {
            string[] tokens = mappedDeviceName.Split('.');
            if (tokens.Length > 1)
                return ParseAndLookupMappedDeviceName(obj[tokens[0]],
                    mappedDeviceName.Substring(mappedDeviceName.IndexOf(tokens[1])));
            else
                return obj[mappedDeviceName];
        }
        public async Task<T> GetDeviceByMappedName<T>(string mappedDeviceName)
        {
            return await GetDeviceById<T>(LookupDeviceId(mappedDeviceName));
        }

        public async Task<T> GetDeviceById<T>(string deviceId)
        {
            return await Task.FromResult((T)Activator.CreateInstance(typeof(T), new Object[] { this, deviceId }));
        }

        public abstract Task<Dictionary<string, string>> GetDeviceState(IDevice device);

        public abstract Task<T> GetDeviceByLabel<T>(string label);
    }
}
