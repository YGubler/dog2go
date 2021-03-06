﻿using System;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace dog2go.Views.Extensions
{
    public static class RequireJsHelpers
    {
        public static MvcHtmlString InitPageMainModule(this HtmlHelper helper, string pageModule)
        {
            var require = new StringBuilder();
            var scriptsPath = "~/Frontend/";
            var absolutePath = VirtualPathUtility.ToAbsolute(scriptsPath);
            require.AppendLine("<script>");
            require.AppendFormat("    require([\"{0}requirejs.config.js\"]," + Environment.NewLine, absolutePath);
            require.AppendLine("        function() {");
            require.AppendFormat("            require([\"{0}\", \"domReady!\"]);" + Environment.NewLine, pageModule);
            require.AppendLine("        }");
            require.AppendLine("    );");
            require.AppendLine("</script>");

            return new MvcHtmlString(require.ToString());
        }
    }
}