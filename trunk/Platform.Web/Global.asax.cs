﻿using System;
using Abp.Castle.Logging.Log4Net;
using Abp.PlugIns;
using Abp.Web;
using Castle.Facilities.Logging;

namespace Platform.Web
{
    public class MvcApplication : AbpWebApplication<PlatformWebModule>
    {
        protected override void Application_Start(object sender, EventArgs e)
        {
            AbpBootstrapper.IocManager.IocContainer.AddFacility<LoggingFacility>(
                f => f.UseAbpLog4Net().WithConfig("log4net.config")
            );

            AbpBootstrapper.PlugInSources.AddFolder(Server.MapPath("/Plugins/PluginTemplate/release"));
            AbpBootstrapper.PlugInSources.AddFolder(Server.MapPath("/Plugins/Examination/release"));

            base.Application_Start(sender, e);
        }
    }
}
