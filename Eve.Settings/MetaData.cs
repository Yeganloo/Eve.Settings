using System;
using System.Collections.Generic;
using System.Text;

namespace Eve.Settings
{
    public class MetaData
    {
        public bool Updated = false;

        public Action<object> OnChange;

        public Type SettingsType;
    }
}
