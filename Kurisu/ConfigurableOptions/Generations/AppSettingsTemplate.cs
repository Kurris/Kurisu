using System.Collections.Generic;
using System.Dynamic;

namespace Kurisu.ConfigurableOptions.Generations
{
    public class AppSettingsTemplate
    {
        public AppSettingsItem AppSettings { get; set; } = new(new Dictionary<string, object>());
    }

    public class AppSettingsItem : DynamicObject
    {
        public Dictionary<string, object> Properties { get; }

        public AppSettingsItem(Dictionary<string, object> items)
        {
            Properties = items;
        }


        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return Properties.Keys;
        }


        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (Properties.ContainsKey(binder.Name))
            {
                result = Properties[binder.Name];
                return true;
            }

            result = null;
            return false;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (Properties.ContainsKey(binder.Name))
            {
                Properties[binder.Name] = value;
                return true;
            }

            return false;
        }
    }
}