using DevExpress.DashboardAspNetCore;
using DevExpress.DashboardWeb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;

namespace Magnar.AI.Controllers;

[Authorize]
public class DefaultDashboardController : DashboardController
{
    public DefaultDashboardController(DashboardConfigurator configurator, IDataProtectionProvider? dataProtectionProvider)
        : base(configurator, dataProtectionProvider)
    {
    }
}