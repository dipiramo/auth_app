using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Web;

namespace Microsoft.Mdp.Identity.Demo.Web
{
    public class AppSettings
    {
        public class Environment
        {
            /// <summary>
            /// Returns the name of the environment from App.Settings 
            /// </summary>
            static public string Name
            {
                get
                {
                    if (ConfigurationManager.AppSettings["Environment:Name"] != null)
                        return ConfigurationManager.AppSettings["Environment:Name"].ToString();

                    return string.Empty;
                }
            }

            /// <summary>
            /// Returns the data center location from App.Settings
            /// </summary>
            static public string DataCenter
            {
                get
                {
                    if (ConfigurationManager.AppSettings["Environment:DataCenter"] != null)
                        return ConfigurationManager.AppSettings["Environment:DataCenter"].ToString();

                    return string.Empty;
                }
            }

            /// <summary>
            /// Returens the full description of the environment
            /// </summary>
            static public string Description
            {
                get
                {
                    if (Name.Contains("*"))
                        return "";
                    else
                        return $"({Name}, {DataCenter}, {Assembly.GetExecutingAssembly().GetName().Version})";
                }
            }

            /// <summary>
            /// Returns the HTML/CSS color of the environment
            /// </summary>
            public static string EnvironmentColor
            {
                get
                {
                    switch (AppSettings.Environment.Name.ToLower())
                    {
                        case "dev":
                            return "purple";
                        case "test":
                            return "Teal";
                        case "qa":
                            return "Maroon";
                        case "preprod":
                            return "DarkOliveGreen";
                        default:
                            return "black";
                    }
                }
            }

        }
    }
}