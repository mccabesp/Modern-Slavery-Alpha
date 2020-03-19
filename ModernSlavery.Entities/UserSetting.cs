﻿using System;
using ModernSlavery.Extensions;
using ModernSlavery.Entities.Enums;

namespace ModernSlavery.Entities
{
    public class UserSetting
    {

        public UserSetting(UserSettingKeys key, string value)
        {
            Key = key;
            Value = value;
        }

        public long UserId { get; set; }
        public UserSettingKeys Key { get; set; }
        public string Value { get; set; }
        public DateTime Modified { get; set; } = VirtualDateTime.Now;

        public virtual User User { get; set; }

    }
}