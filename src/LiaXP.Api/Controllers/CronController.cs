using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiaXP.Api.Controllers;

[Authorize(Roles = "Admin,Manager")]
[ApiController]
[Route("cron")]
[Produces("application/json")]
public class CronController : ControllerBase
{
    private readonly ILogger<CronController> _logger;

    public CronController(ILogger<CronController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executa manualmente a geração de mensagens agendadas
    /// </summary>
    /// <param name="moment">Momento do dia (morning, midday, evening)</param>
    /// <param name="send">Se deve enviar as mensagens ou apenas gerar para revisão</param>
    /// <returns>Resultado da execução</returns>
    [HttpPost("run-now")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RunNow(
        [FromQuery] string moment = "morning",
        [FromQuery] bool send = false)
    {
        try
        {
            _logger.LogInformation("Execução manual de cron iniciada: {Moment}, Send: {Send}", moment, send);

            // TODO: Implementar lógica de geração e envio de mensagens

            return Ok(new
            {
                Status = "success",
                Moment = moment,
                MessagesSent = send ? 5 : 0,
                MessagesGenerated = 5
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar cron manual");
            return StatusCode(500, new { error = "Erro ao executar cron" });
        }
    }
}