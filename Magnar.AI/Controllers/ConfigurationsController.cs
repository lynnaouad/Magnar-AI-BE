using Magnar.AI.Application.Configuration;
using Microsoft.Extensions.Options;

namespace Magnar.AI.Controllers;

public class ConfigurationsController : BaseController
{
    private readonly ReCaptchaConfiguration reCaptchaOptions;
    private readonly UrlsConfiguration urlsConf;

    public ConfigurationsController(
        IMediator mediator,
        IOptions<ReCaptchaConfiguration> reCaptchaOptions,
        IOptions<UrlsConfiguration> urlsConf)
        : base(mediator)
    {
        this.reCaptchaOptions = reCaptchaOptions.Value;
        this.urlsConf = urlsConf.Value;
    }

    [HttpGet]
    [Route("client-configuration")]
    public IActionResult GetClientConfiguration()
    {
        return Ok(new ClientConfigurationDto() { ReCaptchaConfig = reCaptchaOptions, ApiUri = urlsConf.Authority });
    }
}
