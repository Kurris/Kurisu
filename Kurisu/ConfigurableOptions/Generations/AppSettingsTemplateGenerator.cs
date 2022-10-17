using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kurisu.ConfigurableOptions.Attributes;
using Newtonsoft.Json;

namespace Kurisu.ConfigurableOptions.Generations
{
    /// <summary>
    /// 配置文件生成类
    /// </summary>
    public class AppSettingsTemplateGenerator
    {
        /// <summary>
        /// 获取json结构
        /// </summary>
        /// <returns></returns>
        public static string GetJson()
        {
            var typesNeedToMapping = App.ActiveTypes.Where(x => x.IsDefined(typeof(ConfigurationAttribute)));
            return !typesNeedToMapping.Any()
                ? string.Empty
                : GenerateAppSettingsTemplate(typesNeedToMapping);
        }

        /// <summary>
        /// 生成框架配置文件模版
        /// </summary>
        /// <param name="typesNeedToMapping"></param>
        /// <returns></returns>
        private static string GenerateAppSettingsTemplate(IEnumerable<Type> typesNeedToMapping)
        {
            var template = new AppSettingsTemplate();
            template.AppSettings.Properties.Add("Logging", new
            {
                LogLevel = new Dictionary<string, string>
                {
                    ["Default"] = "Information",
                    ["Microsoft"] = "Warning",
                    ["Microsoft.Hosting.Lifetime"] = "Information"
                }
            });

            foreach (var type in typesNeedToMapping)
            {
                var configurationAttribute = type.GetCustomAttribute<ConfigurationAttribute>();
                var path = string.IsNullOrEmpty(configurationAttribute.Path) ? type.Name : configurationAttribute.Path;

                template.AppSettings = SetAppSettings(template.AppSettings, path, type);
            }

            template.AppSettings.Properties.Add("Serilog", new
            {
                MinimumLevel = new Dictionary<string, object>
                {
                    ["Default"] = "Warning",
                    ["Override"] = new Dictionary<string, string>()
                    {
                        ["System"] = "Warning",
                        ["Microsoft"] = "Warning",
                        ["Microsoft.Hosting.Lifetime"] = "Information",
                        ["Microsoft.EntityFrameworkCore.Database.Command"] = "Information"
                    }
                },
                WriteTo = new Dictionary<string, object>()
                {
                    ["Name"] = "Console",
                    ["Args"] = new
                    {
                        outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
                    }
                }
            });

            var json = JsonConvert.SerializeObject(template);
            return json;
        }

        private static AppSettingsItem SetAppSettings(AppSettingsItem item, string path, Type type)
        {
            var currentSection = item;
            var sections = path.Split(":").ToList();

            while (sections.Count != 1)
            {
                var section = sections.First();
                sections.RemoveAt(0);

                var nextSection = new AppSettingsItem(new Dictionary<string, object>());
                currentSection.Properties.Add(section, nextSection);
                currentSection = nextSection;
            }

            var @object = Activator.CreateInstance(type);
            var config = type.GetProperties().ToDictionary(x => x.Name, x => x.GetValue(@object));
            currentSection.Properties.Add(path, config);

            return currentSection;
        }
    }
}