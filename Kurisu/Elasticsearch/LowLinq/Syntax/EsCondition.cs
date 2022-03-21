using System;
using System.Collections.Generic;

namespace Kurisu.Elasticsearch.LowLinq.Syntax
{
    internal static class EsCondition
    {
        public static EsItem GetItem(string field, object value, EsConditionType conditionType)
        {
            field = field.ToLower();

            switch (conditionType)
            {
                case EsConditionType.Term:
                    return new EsItem(new Dictionary<string, object>
                    {
                        ["term"] = new EsItem(new Dictionary<string, object> {
                        { field, new
                            {
                               value
                            }
                        }})
                    });
                case EsConditionType.Match:
                    return new EsItem(new Dictionary<string, object>
                    {
                        ["match"] = new EsItem(new Dictionary<string, object> { { field, value } })
                    });
                default:
                    throw new NotImplementedException();
            }
        }
    }

    internal enum EsConditionType
    {
        /// <summary>
        /// Equals
        /// </summary>
        Term = 0,

        /// <summary>
        /// Contains
        /// </summary>
        Match = 1,
        Should = 2,
        Must = 3,
    }
}
