﻿using System.ComponentModel.Composition;
using WinTak.Framework.Preferences;
using WinTak.Framework.Preferences.Attributes;

namespace ipswintakplugin.Preferences
{
    [Export(typeof(IToolPreference))]
    internal class IPSPreferences : Preference, IToolPreference, IPreference
    {
        private const string soundNotification = "playSound";
        private bool notificationSoundBool;

        [PropertyOrder(1)]
        [SettingsKey("ChangeNotification")]

        public bool PlayNotificationSound
        {
            get
            {
                return this.notificationSoundBool;
            }
            set
            {
                base.SetProperty(ref this.notificationSoundBool, value, "");
            }
        }
    }
}
